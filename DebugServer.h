//
//  DebugServer.h
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#ifndef IA_Pe_tri_DebugServer_h
#define IA_Pe_tri_DebugServer_h

#include <string>
#include "PetriNet.h"
#include "jsoncpp/include/json.h"
#include <set>
#include "Socket.h"

using namespace std::string_literals;

namespace DebugServer {
	extern std::string getVersion();
	extern std::chrono::system_clock::time_point getAPIdate();
	extern std::chrono::system_clock::time_point getDateFromTimestamp(char const *timestamp);
}

template<typename _ActionResult>
class PetriDynamicLibCommon;

template<typename _ActionResult>
class PetriDebug;

template<typename _ActionResult>
class DebugSession {
public:
	DebugSession(PetriDynamicLibCommon<_ActionResult> &petri);
	
	~DebugSession();

	DebugSession(DebugSession const &) = delete;
	DebugSession &operator=(DebugSession const &) = delete;

	DebugSession(DebugSession &&) = default;
	DebugSession &operator=(DebugSession &&) = default;

	void start();
	void stop();
	bool running() const;

	void addActiveState(Action<_ActionResult> &a);
	void removeActiveState(Action<_ActionResult> &a);

protected:
	void serverCommunication();
	void heartBeat();

	void clearPetri();

	void setPause(bool pause);

	void updateBreakpoints(Json::Value const &breakpoints);

	Json::Value receiveObject();
	void sendObject(Json::Value const &o);

	Json::Value json(std::string const &type, Json::Value const &payload);
	Json::Value error(std::string const &error);

	std::map<Action<_ActionResult> *, std::size_t> _activeStates;
	bool _stateChange = false;
	std::condition_variable _stateChangeCondition;
	std::mutex _stateChangeMutex;

	std::thread _receptionThread;
	std::thread _heartBeat;
	Petri::Socket _socket;
	Petri::Socket _client;
	std::atomic_bool _running = {false};

	PetriDynamicLibCommon<_ActionResult> &_petriNetFactory;
	std::unique_ptr<PetriDebug<_ActionResult>> _petri;
	std::mutex _sendMutex;
	std::mutex _breakpointsMutex;
	std::set<Action<_ActionResult> *> _breakpoints;
};

#include "DebugServer.hpp"

#endif
