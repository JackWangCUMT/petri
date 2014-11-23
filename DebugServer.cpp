//
//  DebugServer.cpp
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#include "DebugServer.h"
#include <unordered_map>
#include <string>
#include "Petri.h"
#include "Socket.h"
#include "jsoncpp/include/json.h"

namespace {
	struct Session {
		Session(PetriDynamicLibCommon &petri) : _socket(SOCK_TCP), _petriNetFactory(petri) {

		}

		void serverCommunication() {
			try {
				while(_running && _socket.getState() == SOCK_CONNECTED) {
					auto vect = _socket.receiveNewMsg();
					std::string msg(vect.begin(), vect.end());

					Json::Value root;
					Json::Reader reader;
					if(!reader.parse(&msg.data()[0], &msg.data()[msg.length()], root)) {
						logError("Invalid debug message received from server: ", msg);
						throw std::runtime_error("Invalid debug message received!");
					}

					std::string type = root["type"].asString();

					if(type == "start") {
						if(_petri)
							throw std::runtime_error("Petri net is already running!");
						_petri = std::unique_ptr<PetriDebug>(static_cast<PetriDebug *>(_petriNetFactory.createDebug().release()));
						_petri->run();
					}
					else if(type == "stop") {
						if(!_petri)
							throw std::runtime_error("Petri net is not running!");
						_petri->run();
					}
					else if(type == "reload") {
						if(!_petri)
							throw std::runtime_error("Petri net is not running!");
						_petri->stop();
						_petriNetFactory.reload();
						_petri = std::unique_ptr<PetriDebug>(static_cast<PetriDebug *>(_petriNetFactory.createDebug().release()));
					}
					logDebug0("New debug message received: ", type);
				}
			}
			catch(std::exception &e) {
				logError("Caught exception, exiting debugger!");
			}

			_socket.shutDown();
			_running = false;
			if(_petri)
				_petri->stop();
		}

		void start(std::string const &hostname, std::uint16_t port) {
			_hostname = hostname;
			_port = port;
			
			if(!_socket.connect(_hostname.c_str(), _port))
				throw std::runtime_error("Unable to connect to the debugging server!");

			_thread = std::thread(&Session::serverCommunication, this);
		}

		void stop() {
			_running = false;
			_thread.join();
		}
		
		std::string _hostname;
		std::uint16_t _port;
		std::thread _thread;
		Socket _socket;
		std::atomic_bool _running = {false};
		PetriDynamicLibCommon &_petriNetFactory;
		std::unique_ptr<PetriDebug> _petri;
	};

	std::unordered_map<std::string, std::unique_ptr<Session>> _sessions;
}

namespace DebugServer {
	void registerPetriNet(std::string const &name, PetriDynamicLibCommon &petriNet) {
		_sessions.emplace(std::make_pair(name, std::make_unique<Session>(petriNet)));
	}

	void unregisterPetriNet(std::string const &name) {
		if(_sessions.count(name) == 0)
			throw std::runtime_error("The requested Petri Net hasn't been registered");

		if(_sessions[name]->_running)
			_sessions[name]->stop();

		_sessions.erase(name);
	}

	void init() {

	}

	void exit() {
		for(auto &p : _sessions) {
			p.second->stop();
		}
	}

	void initSession(std::string const &name, std::string const &hostname, std::uint16_t port) {
		if(_sessions.count(name) == 0)
			throw std::runtime_error("The requested Petri Net hasn't been registered");

		_sessions[name]->start(hostname, port);
	}

	void exitSession(std::string const &name) {
		if(_sessions.count(name) == 0)
			throw std::runtime_error("The requested Petri Net hasn't been registered");

		_sessions[name]->stop();
	}
}