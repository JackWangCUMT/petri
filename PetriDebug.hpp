//
//  PetriDebug.cpp
//  Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "DebugServer.h"

namespace Petri {

	template<typename _ActionResult>
	inline void PetriDebug<_ActionResult>::stateEnabled(Action<_ActionResult> &a) {
		if(_observer) {
			_observer->addActiveState(a);
		}
	}

	template<typename _ActionResult>
	inline void PetriDebug<_ActionResult>::stateDisabled(Action<_ActionResult> &a) {
		if(_observer) {
			_observer->removeActiveState(a);
		}
	}

	template<typename _ActionResult>
	inline void PetriDebug<_ActionResult>::addAction(std::shared_ptr<Action<_ActionResult>> &action, bool active) {
		_statesMap[action->ID()] = action.get();
		this->PetriNet<_ActionResult>::addAction(action, active);
	}

	template<typename _ActionResult>
	inline void PetriDebug<_ActionResult>::stop() {
		if(_observer) {
			_observer->notifyStop();
		}
	}

}
