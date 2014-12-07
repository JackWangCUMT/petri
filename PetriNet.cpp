//
//  PetriNet.cpp
//  IA Pétri
//
//  Created by Rémi on 29/11/2014.
//

#include "PetriNet.h"
#include <cassert>

PetriNet::PetriNet(std::string const &name) : _actionsPool(InitialThreadsActions, name), _name(name) {}

PetriNet::~PetriNet() {
	this->stop();
}

void PetriNet::addAction(std::shared_ptr<Action> &action, bool active) {
	if(this->running()) {
		throw std::runtime_error("Cannot modify running state chart!");
	}

	_states.emplace_back(action, active);
}

void PetriNet::run() {
	if(this->running()) {
		throw std::runtime_error("Already running!");
	}

	for(auto &p : _states) {
		if(p.second) {
			_running = true;
			this->enableState(*p.first);
		}
	}
}

void PetriNet::stop() {
	if(this->running()) {
		_running = false;
		_activationCondition.notify_all();

		_actionsPool.join();
	}
}

void PetriNet::executeState(Action &a) {	
	Action *nextState = nullptr;

	for(auto &t : a.transitions()) {
		t->actionStarted();
	}

	// Runs the Callable
	ResultatAction res = a.action()();

	for(auto &t : a.transitions()) {
		t->actionEnded();
	}

	if(!a.transitions().empty()) {
		std::list<decltype(a.transitions().begin())> transitionsToTest;
		for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
			transitionsToTest.push_back(it);
		}

		auto lastTest = ClockType::time_point();

		while(_running && transitionsToTest.size()) {
			auto now = ClockType::now();
			auto minDelay = ClockType::duration::max() / 2;

			for(auto it = transitionsToTest.begin(); it != transitionsToTest.end();) {
				bool isFulfilled = false;

				if((now - lastTest) >= (**it)->delayBetweenEvaluation()) {
					// Testing the transition
					isFulfilled = (**it)->isFulfilled(res);
					minDelay = std::min(minDelay, (**it)->delayBetweenEvaluation());
				}
				else {
					minDelay = std::min(minDelay, (**it)->delayBetweenEvaluation() - (now - lastTest));
				}

				if(isFulfilled) {
					Action &a = (**it)->next();
					std::lock_guard<std::mutex> tokensLock(a.tokensMutex());
					if(++a.currentTokens() >= a.requiredTokens()) {
						a.currentTokens() -= a.requiredTokens();

						if(nextState == nullptr) {
							nextState = &a;
						}
						else {
							this->enableState(a);
						}
					}

					it = transitionsToTest.erase(it);
				}
			}

			if(nextState != nullptr) {
				break;
			}
			else {
				lastTest = now;

				while(ClockType::now() - lastTest <= minDelay) {
					std::this_thread::sleep_for(std::min(1000000ns, minDelay));
				}
			}
		}
	}

	if(nextState != nullptr) {
		this->swapStates(a, *nextState);
	}
	else {
		this->disableState(a);
	}
}

void PetriNet::swapStates(Action &oldAction, Action &newAction) {
	{
		std::lock_guard<std::mutex> lk(_activationMutex);
		_activeStates.insert(&newAction);

		auto it = _activeStates.find(&oldAction);
		assert(it != _activeStates.end());
		_activeStates.erase(it);
	}

	this->stateDisabled(oldAction);
	this->stateEnabled(newAction);

	_actionsPool.addTask(make_callable_ptr(std::bind(&PetriNet::executeState, std::ref(*this), std::placeholders::_1), std::ref(newAction)));
}

void PetriNet::enableState(Action &a) {
	{
		std::lock_guard<std::mutex> lk(_activationMutex);
		_activeStates.insert(&a);

		if(_actionsPool.threadCount() < _activeStates.size()) {
			_actionsPool.addThread();
		}
	}

	_actionsPool.addTask(make_callable_ptr(std::bind(&PetriNet::executeState, std::ref(*this), std::placeholders::_1), std::ref(a)));

	this->stateEnabled(a);
}

void PetriNet::disableState(Action &a) {
	std::lock_guard<std::mutex> lk(_activationMutex);

	auto it = _activeStates.find(&a);
	assert(it != _activeStates.end());

	_activeStates.erase(it);

	if(_activeStates.size() == 0) {
		_running = false;
	}

	this->stateDisabled(a);
}

