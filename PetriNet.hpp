//
//  PetriNet.cpp
//  Pétri
//
//  Created by Rémi on 29/11/2014.
//

#include <cassert>

namespace Petri {

	template<typename _ActionResult>
	inline PetriNet<_ActionResult>::PetriNet(std::string const &name) : _actionsPool(InitialThreadsActions, name), _name(name) {}

	template<typename _ActionResult>
	inline PetriNet<_ActionResult>::~PetriNet() {
		this->stop();
	}

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::addAction(std::shared_ptr<Action<_ActionResult>> &action, bool active) {
		if(this->running()) {
			throw std::runtime_error("Cannot modify running state chart!");
		}

		_states.emplace_back(action, active);
	}

	template<typename _ActionResult>
	void PetriNet<_ActionResult>::addVariable(std::uint_fast32_t id) {
		_variables.emplace(std::make_pair(id, std::make_unique<Atomic>()));
	}

	template<typename _ActionResult>
	Atomic &PetriNet<_ActionResult>::getVariable(std::uint_fast32_t id) {
		auto it = _variables.find(id);
		return *it->second;
	}

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::run() {
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

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::stop() {
		if(this->running()) {
			_running = false;
			_activationCondition.notify_all();
		}
		_actionsPool.cancel();
	}

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::join() {
		// Quick and dirty…
		while(this->running()) {
			std::this_thread::sleep_for(std::chrono::nanoseconds(1'000'000));
		}
	}

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::executeState(Action<_ActionResult> &a) {
		Action<_ActionResult> *nextState = nullptr;

		for(auto &t : a.transitions()) {
			t->actionStarted();
		}

		// Runs the Callable
		auto res = a.action()();

		for(auto &t : a.transitions()) {
			t->actionEnded();
		}

		if(!a.transitions().empty()) {
			std::list<Transition<_ActionResult>> transitionsToTest;
			for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
				transitionsToTest.emplace_back(**it);
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
						Action<_ActionResult> &a = (it)->next();
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

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::swapStates(Action<_ActionResult> &oldAction, Action<_ActionResult> &newAction) {
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

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::enableState(Action<_ActionResult> &a) {
		{
			std::lock_guard<std::mutex> lk(_activationMutex);
			_activeStates.insert(&a);

			if(_actionsPool.threadCount() < _activeStates.size()) {
				_actionsPool.addThread();
			}
		}

		this->stateEnabled(a);
		_actionsPool.addTask(make_callable_ptr(std::bind(&PetriNet::executeState, std::ref(*this), std::placeholders::_1), std::ref(a)));
	}

	template<typename _ActionResult>
	inline void PetriNet<_ActionResult>::disableState(Action<_ActionResult> &a) {
		std::lock_guard<std::mutex> lk(_activationMutex);
		
		auto it = _activeStates.find(&a);
		assert(it != _activeStates.end());
		
		_activeStates.erase(it);
		
		this->stateDisabled(a);
		if(_activeStates.size() == 0) {
			_running = false;
		}
	}
	
}

