//
//  PetriDebug.cpp
//  Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "PetriDebug.h"
#include "DebugServer.h"
#include "Action.h"
#include "PetriNetImpl.h"

namespace Petri {

	struct PetriDebug::Internals : PetriNet::Internals {
		Internals(PetriDebug &pn, std::string const &name) : PetriNet::Internals(pn, name) {
			
		}
		void stateEnabled(Action &a) override;
		void stateDisabled(Action &a) override;

		DebugSession *_observer = nullptr;
		std::unordered_map<uint64_t, Action *> _statesMap;
	};

	void PetriDebug::Internals::stateEnabled(Action &a) {
		if(_observer) {
			_observer->addActiveState(a);
		}
	}

	void PetriDebug::Internals::stateDisabled(Action &a) {
		if(_observer) {
			_observer->removeActiveState(a);
		}
	}

	PetriDebug::PetriDebug(std::string const &name) : PetriNet(std::make_unique<PetriDebug::Internals>(*this, name)) {

	}

	PetriDebug::~PetriDebug() {
		
	};


	void PetriDebug::setObserver(DebugSession *session) {
		static_cast<Internals &>(*_internals)._observer = session;
	}
	void PetriDebug::addAction(std::shared_ptr<Action> &action, bool active) {
		static_cast<Internals &>(*_internals)._statesMap[action->ID()] = action.get();
		this->PetriNet::addAction(action, active);
	}

	void PetriDebug::stop() {
		if(static_cast<Internals &>(*_internals)._observer) {
			static_cast<Internals &>(*_internals)._observer->notifyStop();
		}
		this->PetriNet::stop();
	}

	Action *PetriDebug::stateWithID(uint64_t id) const {
		auto it = static_cast<Internals &>(*_internals)._statesMap.find(id);
		if(it != static_cast<Internals &>(*_internals)._statesMap.end())
			return it->second;
		else
			return nullptr;
	}

	ThreadPool<void> &PetriDebug::actionsPool() {
		return _internals->_actionsPool;
	}

}
