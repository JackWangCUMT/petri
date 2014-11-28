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
#include "Petri.h"
#include "Socket.h"
#include "jsoncpp/include/json.h"
#include "Commun.h"

using namespace std::string_literals;

namespace DebugServer {
	extern std::string version;
}

class DebugSession {
public:
	DebugSession(PetriDynamicLibCommon &petri) : _socket(SockProtocol::TCP), _client(SockProtocol::TCP), _petriNetFactory(petri) {}
	
	~DebugSession() {
		if(_receptionThread.joinable())
			_receptionThread.join();
		if(_heartBeat.joinable())
			_heartBeat.join();
	}

	DebugSession(DebugSession const &) = delete;
	DebugSession &operator=(DebugSession const &) = delete;

	DebugSession(DebugSession &&) = default;
	DebugSession &operator=(DebugSession &&) = default;

	void start() {
		_running = true;
		_receptionThread = std::thread(&DebugSession::serverCommunication, this);
	}

	void stop() {
		_running = false;
		if(_receptionThread.joinable())
			_receptionThread.join();
		if(_heartBeat.joinable())
			_heartBeat.join();
	}

	bool running() const {
		return _running;
	}

	void addActiveState(Action &a) {
		std::lock_guard<std::mutex> lk(_stateChangeMutex);
		++_activeStates[&a];

		_stateChange = true;
		_stateChangeCondition.notify_all();
	}

	void removeActiveState(Action &a) {
		std::lock_guard<std::mutex> lk(_stateChangeMutex);
		auto it = _activeStates.find(&a);
		if(it == _activeStates.end() || it->second == 0)
			throw std::runtime_error("Trying to remove an inactive state!");
		--it->second;

		_stateChange = true;
		_stateChangeCondition.notify_all();
	}

protected:
	void serverCommunication() {
		setThreadName("DebugSession "s + _petriNetFactory.name());

		logInfo("Debug session for Petri net ", _petriNetFactory.name(), " started");

		int attempts = 0;
		while(true) {
			if(_socket.listen(_petriNetFactory.port()))
				break;

			sleep(1_s);
			logError("Could not bind socket to requested port (attempt ", ++attempts, ")");
			if(attempts > 20) {
				_running = false;
				break;
			}
		}

		while(_running) {
			logInfo("Listening…");
			_socket.accept(_client);
			logDebug0("Connected!");

			try {
				while(_running && _client.getState() == SOCK_ACCEPTED) {
					auto const root = this->receiveObject();
					auto const &type = root["type"];

					if(type == "hello") {
						if(root["payload"]["version"] != DebugServer::version) {
							this->sendObject(this->error("The server (version "s + DebugServer::version + ") is incompatible with your client!"s));
						}
						else {
							if(root["payload"]["hash"] != _petriNetFactory.hash()) {
								this->sendObject(this->error("You are trying to run a Petri net that is differrent from the one which is compiled!"));
								throw std::runtime_error("You are trying to run a Petri net that is differrent from the one which is compiled!");
							}
							else {
								Json::Value ehlo;
								ehlo["type"] = "ehlo";
								ehlo["version"] = DebugServer::version;
								this->sendObject(ehlo);
								
								_heartBeat = std::thread(&DebugSession::heartBeat, this);
							}
						}
					}
					else if(type == "start") {
						if(!_petri) {
							_petri = _petriNetFactory.createDebug();
							_petri->setObserver(this);
						}
						else if(_petri->running())
							throw std::runtime_error("Petri net is already running!");

						_petri->run();

						this->sendObject(this->json("ack", "start"));
					}
					else if(type == "exit") {
						if(_petri) {
							_petri->stop();
							_petri = nullptr;
						}

						this->sendObject(this->json("exit", "kbye"));

						break;
					}
					else if(type == "stop") {
						if(_petri) {
							_petri->stop();
							_petri = nullptr;
						}

						this->sendObject(this->json("ack", "stop"));
					}
					else if(type == "reload") {
						if(_petri) {
							_petri->stop();
						}
						_petriNetFactory.reload();
						_petri = _petriNetFactory.createDebug();
						_petri->setObserver(this);
					}

					//logDebug0("New debug message received: ", type);
				}
			}
			catch(std::exception &e) {
				this->sendObject(this->json("exit", e.what()));
				logError("Caught exception, exiting debugger: ", e.what());
			}
			_client.shutDown();
			_stateChangeCondition.notify_all();
			if(_heartBeat.joinable())
				_heartBeat.join();

			logDebug0("Disconnected!");
		}

		_running = false;
		_socket.shutDown();
		_stateChangeCondition.notify_all();

		if(_petri)
			_petri->stop();
	}
	
	void heartBeat() {
		setThreadName("DebugSession "s + _petriNetFactory.name() + " heart beat"s);
		while(_running && _client.getState() == SOCK_ACCEPTED) {
			std::unique_lock<std::mutex> lk(_stateChangeMutex);
			_stateChangeCondition.wait(lk, [this]() {
				return _stateChange || !_running || _client.getState() != SOCK_ACCEPTED;
			});

			if(!_running || _client.getState() != SOCK_ACCEPTED)
				break;

			Json::Value states(Json::arrayValue);

			for(auto &p : _activeStates) {
				if(p.second > 0) {
					Json::Value state;
					state["id"] = Json::Value(p.first->ID());
					state["count"] = Json::Value(Json::UInt64(p.second));
					states[states.size()] = state;
				}
			}

			this->sendObject(this->json("states", states));
			_stateChange = false;
		}
	}

	Json::Value receiveObject() {
		std::vector<std::uint8_t> vect = _socket.receiveNewMsg(_client);

		std::string msg(vect.begin(), vect.end());

		Json::Value root;
		Json::Reader reader;
		if(!reader.parse(&msg.data()[0], &msg.data()[msg.length()], root)) {
			logError("Invalid debug message received from server: ", msg);
			throw std::runtime_error("Invalid debug message received!");
		}

		return root;
	}

	void sendObject(Json::Value const &o) {
		Json::FastWriter writer;
		std::string s = writer.write(o);

		_socket.sendMsg(_client, s.c_str(), s.size());
	}

	Json::Value json(std::string const &type, Json::Value const &payload) {
		Json::Value err;
		err["type"] = type;
		err["payload"] = payload;

		return err;
	}

	Json::Value error(std::string const &error) {
		return this->json("error", error);
	}

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
