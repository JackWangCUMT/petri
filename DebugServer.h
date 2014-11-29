//
//  DebugServer.h
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#ifndef IA_Pe_tri_DebugServer_h
#define IA_Pe_tri_DebugServer_h

#include "PetriDynamicLibCommon.h"
#include <string>
#include "PetriNet.h"
#include "Socket.h"
#include "jsoncpp/include/json.h"

using namespace std::string_literals;

namespace DebugServer {
	extern std::string const version;
}

class DebugSession {
public:
	DebugSession(PetriDynamicLibCommon &petri);
	
	~DebugSession();

	DebugSession(DebugSession const &) = delete;
	DebugSession &operator=(DebugSession const &) = delete;

	DebugSession(DebugSession &&) = default;
	DebugSession &operator=(DebugSession &&) = default;

	void start();
	void stop();
	bool running() const;

	void addActiveState(Action &a);
	void removeActiveState(Action &a);

protected:
	void serverCommunication();
	void heartBeat();

	void clearPetri();

	Json::Value receiveObject();
	void sendObject(Json::Value const &o);

	Json::Value json(std::string const &type, Json::Value const &payload);
	Json::Value error(std::string const &error);

	std::map<Action *, std::size_t> _activeStates;
	bool _stateChange = false;
	std::condition_variable _stateChangeCondition;
	std::mutex _stateChangeMutex;

	std::thread _receptionThread, _heartBeat;
	Socket _socket, _client;
	std::atomic_bool _running = {false};

	PetriDynamicLibCommon &_petriNetFactory;
	std::unique_ptr<PetriDebug> _petri;
};

#endif
