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
#include "Common.h"

#include "Transition.h"
#include "Action.h"

namespace Petri {

using namespace std::chrono_literals;

	template<typename _ActionResult>
	class PetriNet {
		enum {InitialThreadsActions = 1};
	public:
		/**
		 * Creates the PetriNet, assigning it a name which serves debug purposes (see ThreadPool constructor)
		 */
		PetriNet(std::string const &name);

		virtual ~PetriNet();

		/**
		 * Adds an Action to the PetriNet. The net must not be running yet.
		 * @param action The action to add
		 * @param active Controls whether the action is active as soon as the net is started or not
		 */
		virtual void addAction(std::shared_ptr<Action<_ActionResult>> &action, bool active = false);

		/**
		 * Checks whether the net is running.
		 * @return true means that the net has been started, and we can not add any more action to it now.
		 */
		bool running() const {
			return _running;
		}

		/**
		 * Starts the Petri net. It must not be already running. If no states are initially active, this is a no-op.
		 */
		virtual void run();

		/**
		 * Stops the Petri net. It blocks the calling thread until all running states are finished,
		 * but do not allows new states to be enabled. If the net is not running, this is a no-op.
		 */
		virtual void stop();

		/**
		 * Blocks the calling thread until the Petri net has completed its whole execution.
		 */
		virtual void join();

	protected:
		using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

		// This method is executed concurrently on the thread pool.
		virtual void executeState(Action<_ActionResult> &a);

		virtual void stateEnabled(Action<_ActionResult> &a) {}
		virtual void stateDisabled(Action<_ActionResult> &a) {}

		void enableState(Action<_ActionResult> &a);
		void disableState(Action<_ActionResult> &a);
		void swapStates(Action<_ActionResult> &oldAction, Action<_ActionResult> &newAction);

		std::condition_variable _activationCondition;
		std::multiset<Action<_ActionResult> *> _activeStates;
		std::mutex _activationMutex;

		std::atomic_bool _running = {false};
		ThreadPool<void> _actionsPool;

		std::string const _name;
		std::list<std::pair<std::shared_ptr<Action<_ActionResult>>, bool>> _states;
		std::list<Transition<_ActionResult>> _transitions;
	};

}

#include "PetriNet.hpp"


#endif
