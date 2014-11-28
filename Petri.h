//
//  StateChart.h
//  IA Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef IA_Pe_tri_StateChart_h
#define IA_Pe_tri_StateChart_h

#include "Callable.h"
#include "Condition.h"
#include <queue>
#include <list>
#include <unordered_map>
#include "ThreadPool.h"
#include <atomic>
#include <mutex>
#include <thread>
#include <deque>
#include "Log.h"
#include "Commun.h"

using namespace std::chrono_literals;

class Action;

#include "Transition.h"
#include "Action.h"

class PetriNet {
	enum {InitialThreadsActions = 0};
public:
	PetriNet() : _actionsPool(InitialThreadsActions) {}

	virtual ~PetriNet() {
		this->stop();
		if(_statesManager.joinable())
			_statesManager.join();
	}

	void addAction(std::shared_ptr<Action> &action, bool active = false) {
		if(this->running()) {
			throw std::runtime_error("Cannot modify running state chart!");
		}

		_states.push_back(action);

		if(active) {
			std::lock_guard<std::mutex> lk(_activationMutex);
			// We allow the initially active states to be actually enabled
			_states.back().get()->currentTokens() = _states.back().get()->requiredTokens();
			++_activeStates;
			_toBeActivated.insert(_states.back().get());
		}
	}

	bool running() const {
		return _running;
	}

	virtual void run() {
		if(_running) {
			throw std::runtime_error("Already running!");
		}

		if(_toBeActivated.empty()) {
			throw std::runtime_error("No active state!");
		}

		_running = true;
		_statesManager = std::thread(&PetriNet::manageStates, this);
	}

	virtual void stop() {
		if(this->running()) {
			_running = false;

			_activationCondition.notify_all();

			// stop() may be called by _statesManager, so we do not try to join from our own thread.
			if(std::this_thread::get_id() != _statesManager.get_id())
				_statesManager.join();
			_actionsPool.join();
		}
	}

protected:
	using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

	virtual void executeState(Action &a) {
		// Lock later, during the reaction to a fulfilled transition
		std::unique_lock<std::mutex> activationLock(_activationMutex, std::defer_lock);
		ResultatAction res = a.action()();

		std::vector<std::pair<decltype(a.transitions().begin()), bool>> conditionsResult;
		conditionsResult.reserve(a.transitions().size());

		for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
			(*it)->willTest();
		}

		auto lastTest = ClockType::time_point();

		bool deactivate = false;
		do {
			if(!_running || a.transitions().empty())
				break;

			auto now = ClockType::now();
			auto minDelay = ClockType::duration::max() / 2;
			for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
				bool isFulfilled = false;
				if((now - lastTest) >= (*it)->delayBetweenEvaluation()) {
					isFulfilled = (*it)->isFulfilled(res);
					minDelay = std::min(minDelay, (*it)->delayBetweenEvaluation());
				}
				else {
					minDelay = std::min(minDelay, (*it)->delayBetweenEvaluation() - (now - lastTest));
				}

				conditionsResult.push_back(std::make_pair(it, isFulfilled));
			}
			lastTest = now;
			
			activationLock.lock();
			for(auto &p : conditionsResult) {
				if(p.second) {
					Action &a = (*p.first)->next();
					++a.currentTokens();

					if(_toBeActivated.insert(&a).second)
						++_activeStates;

					deactivate = true;
				}
			}
			activationLock.unlock();

			while(ClockType::now() - lastTest <= minDelay) {
				std::this_thread::sleep_for(std::min(1000000ns, minDelay));
			}

		} while(!deactivate && _running);

		for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
			(*it)->didTest();
		}

		activationLock.lock();
		_toBeDisabled.push(&a);
		activationLock.unlock();

		_activationCondition.notify_all();
	}

	virtual void manageStates() {
		while(_running) {
			std::unique_lock<std::mutex> lk(_activationMutex);
			_activationCondition.wait(lk, [this]() { return !_toBeActivated.empty() || !_toBeDisabled.empty() || !_running; });

			if(!_running)
				return;

			while(!_toBeDisabled.empty()) {
				this->disableState(*_toBeDisabled.front());
				--_activeStates;
			}

			for(auto it = _toBeActivated.begin(); it != _toBeActivated.end(); ) {
				Action &a = **it;

				if(a.currentTokens() >= a.requiredTokens()) {
					if(_activeStates >= _actionsPool.threadCount()) {
						logInfo("Pool too small, resizing needed (new size: ", _actionsPool.threadCount() + 1, ") !");
						_actionsPool.addThread();
					}

					a.currentTokens() -= a.requiredTokens();

					this->enableState(a);

					it = _toBeActivated.erase(it);
				}
				else {
					++it;
				}
			}
			lk.unlock();

			if(_activeStates == 0) {
				if(!_toBeActivated.empty()) {
					logError("Warning!\nThe statechart has states waiting for tokens to be activated, but will never get them as there are no active states to give them.\nThe pending states are now discarded.");
				}
				this->stop();
			}

			if(!_toBeActivated.empty()) {
				std::this_thread::sleep_for(std::chrono::milliseconds(1));
			}
		}
	}

	virtual void enableState(Action &a) {
		_actionsPool.addTask(make_callable_ptr(&PetriNet::executeState, *this, std::ref(a)));
	}

	virtual void disableState(Action &a) {
		_toBeDisabled.pop();
	}

	std::thread _statesManager;
	std::condition_variable _activationCondition;
	std::set<Action *> _toBeActivated;
	std::queue<Action *> _toBeDisabled;
	std::mutex _activationMutex;

	std::list<std::shared_ptr<Action>> _states;
	std::list<Transition> _transitions;
	
	ThreadPool<void> _actionsPool;

	std::atomic_ulong _activeStates;
	std::atomic_bool _running = {false};
};


#endif
