//
//  PetriDebug.cpp
//  Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "DebugServer.h"

namespace Petri {

	inline void PetriDebug::stateEnabled(Action &a) {
		if(_observer) {
			_observer->addActiveState(a);
		}
	}

	inline void PetriDebug::stateDisabled(Action &a) {
		if(_observer) {
			_observer->removeActiveState(a);
		}
	}

	inline void PetriDebug::addAction(std::shared_ptr<Action> &action, bool active) {
		_statesMap[action->ID()] = action.get();
		this->PetriNet::addAction(action, active);
	}

	inline void PetriDebug::stop() {
		if(_observer) {
			_observer->notifyStop();
			std::cout << "notifyStop" << std::endl;
		}
		this->PetriNet::stop();
	}

}
