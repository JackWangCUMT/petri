//
//  PetriDebug.cpp
//  IA Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "PetriDebug.h"
#include "DebugServer.h"

void PetriDebug::enableState(Action &a) {
	this->PetriNet::enableState(a);
	if(_observer) {
		_observer->addActiveState(a);
	}
}

void PetriDebug::disableState(Action &a) {
	this->PetriNet::disableState(a);
	if(_observer) {
		_observer->removeActiveState(a);
	}
}
