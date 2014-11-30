//
//  PetriDebug.cpp
//  IA Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "PetriDebug.h"
#include "DebugServer.h"

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
