//
//  PetriImp.h
//  IA Pétri
//
//  Created by Rémi on 04/05/2015.
//

#ifndef IA_Pe_tri_PetriImp_h
#define IA_Pe_tri_PetriImp_h

#include <cassert>
#include "Atomic.h"
#include "Action.h"
#include "Callable.h"
#include <queue>
#include <list>
#include <unordered_map>
#include "ThreadPool.h"
#include <atomic>
#include <mutex>
#include <thread>
#include <deque>
#include "Common.h"
#include <map>

#include "Transition.h"

namespace Petri {
	enum {InitialThreadsActions = 1};
	using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

	struct PetriNet::Internals {

		Internals(PetriNet &pn, std::string const &name) : _actionsPool(InitialThreadsActions, name), _name(name), _this(pn) {}
		virtual ~Internals() {
			
		}

		// This method is executed concurrently on the thread pool.
		virtual void executeState(Action &a);

		virtual void stateEnabled(Action &a) {}
		virtual void stateDisabled(Action &a) {}

		void enableState(Action &a);
		void disableState(Action &a);
		void swapStates(Action &oldAction, Action &newAction);

		std::condition_variable _activationCondition;
		std::multiset<Action *> _activeStates;
		std::mutex _activationMutex;

		std::atomic_bool _running = {false};
		ThreadPool<void> _actionsPool;

		std::string const _name;
		std::list<std::pair<Action, bool>> _states;
		std::list<Transition> _transitions;

		std::map<std::uint_fast32_t, std::unique_ptr<Atomic>> _variables;
		
		PetriNet &_this;
	};
}


#endif
