//
//  DebugServer.h
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#ifndef IA_Pe_tri_DebugServer_h
#define IA_Pe_tri_DebugServer_h

#include <string>
#include "Commun.h"
#include "PetriNet.h"
#include "jsoncpp/include/json.h"
#include "SymbolEvaluator.h"
#include <set>

using namespace std::string_literals;

namespace DebugServer {
	extern std::string getVersion();
	extern std::chrono::system_clock::time_point getAPIdate();
	extern std::chrono::system_clock::time_point getDateFromTimestamp(char const *timestamp);
}

class PetriDynamicLibCommon;
class PetriDebug;

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

	void setPause(bool pause);

	void updateBreakpoints(Json::Value const &breakpoints);

	Json::Value receiveObject();
	void sendObject(Json::Value const &o);

	Json::Value json(std::string const &type, Json::Value const &payload);
	Json::Value error(std::string const &error);

	std::map<Action *, std::size_t> _activeStates;
	bool _stateChange = false;
	std::condition_variable _stateChangeCondition;
	std::mutex _stateChangeMutex;

	std::thread _receptionThread;
	std::thread _heartBeat;
	Socket _socket;
	Socket _client;
	std::atomic_bool _running = {false};

	PetriDynamicLibCommon &_petriNetFactory;
	std::unique_ptr<PetriDebug> _petri;
	std::mutex _sendMutex;
	std::mutex _breakpointsMutex;
	std::set<Action *> _breakpoints;

	SymbolEvaluator _evaluator;
};

#endif
