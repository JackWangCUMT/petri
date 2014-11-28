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

class DebugSession;

class PetriDebug : public PetriNet {
public:
	PetriDebug() : PetriNet() {}

	virtual ~PetriDebug() = default;

	void setObserver(DebugSession *session) {
		_observer = session;
	}

protected:
	virtual void enableState(Action &a) override;
	virtual void disableState(Action &a) override;

	DebugSession *_observer = nullptr;
};


#endif
