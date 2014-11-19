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
#include "PetriUtils.h"

class Action;
class Transition;

template<typename T>
struct CallableTimeout {
public:
	CallableTimeout(T id) : _id(id) { }

	T ID() const {
		return _id;
	}

	void setID(T id) {
		_id = id;
	}

	template<typename ReturnType>
	ReturnType operator()(CallableBase<ReturnType> &callable) {
		callable();
	}

private:
	T _id;
};

class Transition : public CallableTimeout<std::uint64_t> {
public:
	Transition(Action &previous, Action &next) : CallableTimeout(0), _previous(previous), _next(next) {}

	bool isFulfilled(ResultatAction resultatAction) const {
		_result = resultatAction;

		return _test->isFulfilled();
	}

	void willTest() {
		_test->willTest();
	}

	void didTest() {
		_test->didTest();
	}

	ConditionBase const &condition() const {
		return *_test;
	}

	void setCondition(ConditionBase const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test.copy_ptr());
	}

	void setCondition(std::shared_ptr<ConditionBase> const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test);
	}

	std::shared_ptr<ConditionBase> compareResult(ResultatAction const &r) const {
		return make_condition_ptr<Condition>(make_callable(&Transition::checkResult, std::cref(_result), r));
	}

	Action &previous() {
		return _previous;
	}

	Action &next() {
		return _next;
	}

	std::string const &name() const {
		return _name;
	}

	void setName(std::string const &name) {
		_name = name;
	}

	std::chrono::nanoseconds delayBetweenEvaluation() const {
		return _delayBetweenEvaluation;
	}

	void setDelayBetweenEvaluation(std::chrono::nanoseconds ms) {
		_delayBetweenEvaluation = ms;
	}

	std::atomic<ResultatAction> const &mutableResult() const {
		return _result;
	}

private:
	static bool checkResult(std::atomic<ResultatAction> const &r1, ResultatAction const &r2) {
		return r1 == r2;
	}

	std::shared_ptr<ConditionBase> _test;
	Action &_previous;
	Action &_next;
	std::string _name;
	mutable std::atomic<ResultatAction> _result;

	// Default delay between evaluation
	std::chrono::nanoseconds _delayBetweenEvaluation = 10ms;
};

class Action : public CallableTimeout<std::uint64_t> {
public:
	Action() : CallableTimeout(0), _action(nullptr), _requiredTokens(1) {}
	Action(CallableBase<ResultatAction> const &action) : CallableTimeout(0), _action(action.copy_ptr()), _requiredTokens(1) {}

	void addTransition(std::shared_ptr<Transition> &t) {
		_transitions.push_back(t);
	}

	CallableBase<ResultatAction> &action() {
		return *_action;
	}

	void setAction(CallableBase<ResultatAction> const &action) {
		_action = action.copy_ptr();
	}

	void setAction(std::shared_ptr<CallableBase<ResultatAction>> const &action) {
		_action = action;
	}

	std::size_t requiredTokens() const {
		return _requiredTokens;
	}

	void setRequiredTokens(std::size_t requiredTokens) {
		_requiredTokens = requiredTokens;
	}

	std::atomic_ulong &currentTokens() {
		return _currentTokens;
	}

	std::string const &name() const {
		return _name;
	}

	void setName(std::string const &name) {
		_name = name;
	}

	std::list<std::shared_ptr<Transition>> &transitions() {
		return _transitions;
	}

private:
	std::list<std::shared_ptr<Transition>> _transitions;
	std::shared_ptr<CallableBase<ResultatAction>> _action;
	std::string _name;
	std::size_t _requiredTokens;
	std::atomic_ulong _currentTokens;
};

class PetriNet {
	enum {InitialThreadsActions = 0};
public:
	PetriNet() : _actionsPool(InitialThreadsActions) {}

	~PetriNet() {
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
			//logDebug7("active states:", _activeStates);
			_toBeActivated.insert(_states.back().get());
		}
	}

	bool running() const {
		return _running;
	}

	void run() {
		if(_running) {
			throw std::runtime_error("Already running!");
		}

		if(_toBeActivated.empty()) {
			//throw std::runtime_error("No active state!");
			return;
		}

		_running = true;
		_statesManager = std::thread(&PetriNet::manageStates, this);
	}

	void stop() {
		if(this->running()) {
			_running = false;

			_activationCondition.notify_all();

			// stop() may be called by _statesManager, so we do not try to join from our own thread.
			if(std::this_thread::get_id() != _statesManager.get_id())
				_statesManager.join();
			_actionsPool.join();
		}
	}

private:
	using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

	void executeState(Action &a) {
		// Lock later, during the reaction to a fulfilled transition
		std::unique_lock<std::mutex> activationLock(_activationMutex, std::defer_lock);
		ResultatAction res = a.action()();

		/*static std::atomic_int nb(0);
		++nb;
		synchronizedOutput(std::to_string(nb));*/

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

					//logInfo("Pushing state for activation: " + a.name());
					//logInfo("tba: " + std::to_string(_toBeActivated.size()));
					deactivate = true;
				}
			}
			activationLock.unlock();

			while(ClockType::now() - lastTest <= minDelay) {
				std::this_thread::sleep_for(std::min(1000000ns, minDelay));
			}

		} while(!deactivate);

		for(auto it = a.transitions().begin(); it != a.transitions().end(); ++it) {
			(*it)->didTest();
		}

		activationLock.lock();
		_toBeDisabled.push(&a);
		activationLock.unlock();
		//logInfo("Pushing state for deactivation: " + a.name());
		//logInfo("tbd: " + std::to_string(_toBeDisabled.size()));
		_activationCondition.notify_all();
	}

	void manageStates() {
		while(_running) {
			std::unique_lock<std::mutex> lk(_activationMutex);
			_activationCondition.wait(lk, [this]() { return !_toBeActivated.empty() || !_toBeDisabled.empty() || !_running; });

			if(!_running)
				return;

			while(!_toBeDisabled.empty()) {
				//--_activeStates[_toBeDisabled.front()];
				//logInfo("Disabling state: ", _toBeDisabled.front()->name());
				_toBeDisabled.pop();
				--_activeStates;
				//logDebug5("active states:", _activeStates);
				//logInfo("tbd: " + std::to_string(_toBeDisabled.size()));
			}

			for(auto it = _toBeActivated.begin(); it != _toBeActivated.end(); ) {
				Action &a = **it;

				if(a.currentTokens() >= a.requiredTokens()) {
					if(_activeStates >= _actionsPool.threadCount()) {
						logInfo("Pool too small, resizing needed (new size: ", _actionsPool.threadCount() + 1, ") !");
						_actionsPool.addThread();
					}

					a.currentTokens() -= a.requiredTokens();
					//logInfo("Adding new state: ", a.name(), " ", _toBeActivated.size());
					_actionsPool.addTask(make_callable_ptr(&PetriNet::executeState, *this, std::ref(a)));
					it = _toBeActivated.erase(it);
				}
				else {
					++it;
				}
				//logInfo("tba: " + std::to_string(_toBeActivated.size()));
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

	//std::unordered_map<Action *, size_t> _activeStates;

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
