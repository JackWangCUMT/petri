//
//  StateChart.h
//  Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef Petri_StateChart_h
#define Petri_StateChart_h

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
#include "Action.h"

namespace Petri {

	class Atomic;
	class Action;

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
		virtual void addAction(std::shared_ptr<Action> &action, bool active = false);

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

		void addVariable(std::uint_fast32_t id);
		Atomic &getVariable(std::uint_fast32_t id);

	protected:
		using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

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
		std::list<std::pair<std::shared_ptr<Action>, bool>> _states;
		std::list<Transition> _transitions;

		std::map<std::uint_fast32_t, std::unique_ptr<Atomic>> _variables;
	};

}

#include "PetriNet.hpp"


#endif
