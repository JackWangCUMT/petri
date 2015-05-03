//
//  PetriDebug.cpp
//  Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "DebugServer.h"

namespace Petri {

	void PetriDebug::stateEnabled(Action &a) {
		if(_observer) {
			_observer->addActiveState(a);
		}
	}

	void PetriDebug::stateDisabled(Action &a) {
		if(_observer) {
			_observer->removeActiveState(a);
		}
	}

	void PetriDebug::addAction(std::shared_ptr<Action> &action, bool active) {
		_statesMap[action->ID()] = action.get();
		this->PetriNet::addAction(action, active);
	}

	void PetriDebug::stop() {
		if(_observer) {
			_observer->notifyStop();
			std::cout << "notifyStop" << std::endl;
		}
		this->PetriNet::stop();
	}

	Action *PetriDebug::stateWithID(uint64_t id) const {
		auto it = _statesMap.find(id);
		if(it != _statesMap.end())
			return it->second;
		else
			return nullptr;
	}

}
