//
//  PetriDebug.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_PetriDebug_h
#define IA_Pe_tri_PetriDebug_h

#include "Petri.h"
#include "Socket.h"
#include "jsoncpp/include/json.h"

class PetriDebug : public PetriNet {
public:
	PetriDebug() : PetriNet() {}

	virtual ~PetriDebug() = default;
};


#endif
