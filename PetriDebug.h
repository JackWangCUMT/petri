//
//  PetriDebug.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_PetriDebug_h
#define IA_Pe_tri_PetriDebug_h

#include "PetriNet.h"
#include "Socket.h"
#include "jsoncpp/include/json.h"

class DebugSession;

class PetriDebug : public PetriNet {
public:
	PetriDebug(std::string const &name) : PetriNet(name) {}

	virtual ~PetriDebug() = default;

	/**
	 * Adds an observer to the PetriDebug object. The observer will be notified by some of the Petri net events, such as when a state is activated or disabled.
	 * @param session The observer which will be notified of the events
	 */
	void setObserver(DebugSession *session) {
		_observer = session;
	}

protected:
	virtual void enableState(Action &a) override;
	virtual void disableState(Action &a) override;

	DebugSession *_observer = nullptr;
};


#endif
