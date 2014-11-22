//
//  Action.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_Action_h
#define IA_Pe_tri_Action_h

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
#include "Transition.h"

using namespace std::chrono_literals;

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

#endif
