//
//  PetriDebug.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_PetriDebug_h
#define Petri_PetriDebug_h

#include "PetriNet.h"
#include <unordered_map>

namespace Petri {

	class DebugSession;

	class PetriDebug : public PetriNet {
	public:
		PetriDebug(std::string const &name) : PetriNet(name) {}

		virtual ~PetriDebug() = default;

		/**
		 * Adds an Action to the PetriNet. The net must not be running yet.
		 * @param action The action to add
		 * @param active Controls whether the action is active as soon as the net is started or not
		 */
		virtual void addAction(std::shared_ptr<Action> &action, bool active = false) override;

		/**
		 * Sets the observer of the PetriDebug object. The observer will be notified by some of the Petri net events, such as when a state is activated or disabled.
		 * @param session The observer which will be notified of the events
		 */
		void setObserver(DebugSession *session) {
			_observer = session;
		}

		/**
		 * Retrieves the underlying ThreadPool object.
		 * @return The underlying ThreadPool
		 */
		ThreadPool<void> &actionsPool() {
			return this->PetriNet::_actionsPool;
		}

		/**
		 * Finds the state associated to the specified ID, or nullptr if not found.
		 * @param The ID to match with a state.
		 * @return The state matching ID
		 */
		Action *stateWithID(uint64_t id) const {
			auto it = _statesMap.find(id);
			if(it != _statesMap.end())
				return it->second;
			else
				return nullptr;
		}

		void stop() override;

	protected:
		virtual void stateEnabled(Action &a) override;
		virtual void stateDisabled(Action &a) override;

		DebugSession *_observer = nullptr;
		std::unordered_map<uint64_t, Action *> _statesMap;
	};

}

#include "PetriDebug.hpp"

#endif
