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

#include "Transition.h"
#include "Action.h"

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
	 * @param active Controls wether the action is active as soon as the net is started or not
	 */
	void addAction(std::shared_ptr<Action> &action, bool active = false);

	/**
	 * Checks wether the net is running.
	 * @return true means that the net has been started, and we can not add any more action to it now.
	 */
	bool running() const {
		return _running;
	}

	/**
	 * Starts the Petri net. It must not be already running. If no states are active, this is a no-op.
	 */
	virtual void run();

	/**
	 * Stops the Petri net. It blocks the calling thread until all running states are finished,
	 * but do not allows new states to be enabled. If the net is not running, this is a no-op.
	 */
	virtual void stop();

protected:
	using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

	// This method is executed concurrently on the thread pool.
	virtual void executeState(Action &a);

	// This method runs on a separate thread when the Petri net is in running state.
	virtual void manageStates();

	virtual void enableState(Action &a);
	virtual void disableState(Action &a);

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
	std::string _name;
};


#endif
