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
	DebugSession(PetriDynamicLibCommon &petri) : _socket(SockProtocol::TCP), _client(SockProtocol::TCP), _petriNetFactory(petri) {
		_petriNetFactory.load();
	}
	~DebugSession() {
		if(_thread.joinable())
			_thread.join();
	}

	DebugSession(DebugSession const &) = delete;
	DebugSession &operator=(DebugSession const &) = delete;

	DebugSession(DebugSession &&) = default;
	DebugSession &operator=(DebugSession &&) = default;

	void serverCommunication() {
		setThreadName(("DebugSession "s + _petriNetFactory.name()).c_str());

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
							Json::Value ehlo;
							ehlo["type"] = "ehlo";
							ehlo["version"] = DebugServer::version;
							this->sendObject(ehlo);
						}
					}
					else if(type == "start") {
						if(!_petri)
							_petri = _petriNetFactory.createDebug();
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
					}

					//logDebug0("New debug message received: ", type);
				}
			}
			catch(std::exception &e) {
				this->sendObject(this->json("exit", e.what()));
				logError("Caught exception, exiting debugger: ", e.what());
			}
			_client.shutDown();
		}

		_socket.shutDown();

		_running = false;
		if(_petri)
			_petri->stop();
	}

	void start() {
		_running = true;
		_thread = std::thread(&DebugSession::serverCommunication, this);
	}

	void stop() {
		_running = false;
		_thread.join();
	}

	bool running() const {
		return _running;
	}

protected:
	Json::Value receiveObject() {
		auto vect = _socket.receiveNewMsg(_client);
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

	std::thread _thread;
	Socket _socket, _client;
	std::atomic_bool _running = {false};
	PetriDynamicLibCommon &_petriNetFactory;
	std::unique_ptr<PetriDebug> _petri;
};

#endif
