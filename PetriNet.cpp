//
//  PetriNet.cpp
//  Pétri
//
//  Created by Rémi on 29/11/2014.
//

#include "PetriNet.h"
#include "PetriNetImpl.h"

namespace Petri {

	PetriNet::PetriNet(std::string const &name) : PetriNet(std::make_unique<Internals>(*this, name)) {}
	PetriNet::PetriNet(std::unique_ptr<Internals> internals) : _internals(std::move(internals)) {}

	PetriNet::~PetriNet() {
		this->stop();
	}

	Action &PetriNet::addAction(Action action, bool active) {
		if(this->running()) {
			throw std::runtime_error("Cannot modify running state chart!");
		}

		_internals->_states.emplace_back(std::move(action), active);

		return _internals->_states.back().first;
	}

	bool PetriNet::running() const {
		return _internals->_running;
	}

	void PetriNet::addVariable(std::uint_fast32_t id) {
		_internals->_variables.emplace(std::make_pair(id, std::make_unique<Atomic>()));
	}

	Atomic &PetriNet::getVariable(std::uint_fast32_t id) {
		auto it = _internals->_variables.find(id);
		return *it->second;
	}

	void PetriNet::run() {
		if(this->running()) {
			throw std::runtime_error("Already running!");
		}

		for(auto &p : _internals->_states) {
			if(p.second) {
				_internals->_running = true;
				_internals->enableState(p.first);
			}
		}
	}

	void PetriNet::stop() {
		if(this->running()) {
			_internals->_running = false;
			_internals->_activationCondition.notify_all();
		}
		_internals->_actionsPool.cancel();
	}

	void PetriNet::join() {
		// Quick and dirty…
		while(this->running()) {
			std::this_thread::sleep_for(std::chrono::nanoseconds(1'000'000));
		}
	}

	void PetriNet::Internals::executeState(Action &a) {
		Action *nextState = nullptr;

		// Runs the Callable
		auto res = a.action()();

		if(!a.transitions().empty()) {
			std::list<Transition> transitionsToTest;
			for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
				transitionsToTest.emplace_back(*it);
			}

			auto lastTest = ClockType::time_point();

			while(_running && transitionsToTest.size()) {
				auto now = ClockType::now();
				auto minDelay = ClockType::duration::max() / 2;

				for(auto it = transitionsToTest.begin(); it != transitionsToTest.end();) {
					bool isFulfilled = false;

					if((now - lastTest) >= (it)->delayBetweenEvaluation()) {
						// Testing the transition
						isFulfilled = (it)->isFulfilled(res);
						minDelay = std::min(minDelay, (it)->delayBetweenEvaluation());
					}
					else {
						minDelay = std::min(minDelay, (it)->delayBetweenEvaluation() - (now - lastTest));
					}

					if(isFulfilled) {
						Action &a = (it)->next();
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
					else {
						++it;
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

	void PetriNet::Internals::swapStates(Action &oldAction, Action &newAction) {
		{
			std::lock_guard<std::mutex> lk(_activationMutex);
			_activeStates.insert(&newAction);

			auto it = _activeStates.find(&oldAction);
			assert(it != _activeStates.end());
			_activeStates.erase(it);
		}

		this->stateDisabled(oldAction);
		this->stateEnabled(newAction);

		_actionsPool.addTask(make_callable_ptr(std::bind(&Internals::executeState, std::ref(*this), std::placeholders::_1), std::ref(newAction)));
	}

	void PetriNet::Internals::enableState(Action &a) {
		{
			std::lock_guard<std::mutex> lk(_activationMutex);
			_activeStates.insert(&a);

			if(_actionsPool.threadCount() < _activeStates.size()) {
				_actionsPool.addThread();
			}
		}

		this->stateEnabled(a);
		_actionsPool.addTask(make_callable_ptr(std::bind(&Internals::executeState, std::ref(*this), std::placeholders::_1), std::ref(a)));
	}

	void PetriNet::Internals::disableState(Action &a) {
		std::lock_guard<std::mutex> lk(_activationMutex);
		
		auto it = _activeStates.find(&a);
		assert(it != _activeStates.end());
		
		_activeStates.erase(it);
		
		this->stateDisabled(a);
		if(_activeStates.size() == 0 && _running) {
			_this.stop();
		}
	}
	
}

