//
//  DebugServer.cpp
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#include "DebugServer.h"
#include "Commun.h"

std::string const DebugServer::version = "0.1";
std::chrono::system_clock::time_point DebugServer::getAPIdate() {
	char const *format = "%a %b %d %H:%M:%S %Y";
	std::tm tm;

	strptime(__TIMESTAMP__, format, &tm);
	return std::chrono::system_clock::from_time_t(std::mktime(&tm));
}

DebugSession::DebugSession(PetriDynamicLibCommon &petri) : _socket(SockProtocol::TCP), _client(SockProtocol::TCP), _petriNetFactory(petri) {}

DebugSession::~DebugSession() {
	if(_receptionThread.joinable())
		_receptionThread.join();
	if(_heartBeat.joinable())
		_heartBeat.join();
}

void DebugSession::start() {
	_running = true;
	_receptionThread = std::thread(&DebugSession::serverCommunication, this);
}

void DebugSession::stop() {
	_running = false;
	if(_receptionThread.joinable())
		_receptionThread.join();
	if(_heartBeat.joinable())
		_heartBeat.join();
}

bool DebugSession::running() const {
	return _running;
}

void DebugSession::addActiveState(Action &a) {
	std::lock_guard<std::mutex> lk(_stateChangeMutex);
	++_activeStates[&a];

	_stateChange = true;
	_stateChangeCondition.notify_all();
}

void DebugSession::removeActiveState(Action &a) {
	std::lock_guard<std::mutex> lk(_stateChangeMutex);
	auto it = _activeStates.find(&a);
	if(it == _activeStates.end() || it->second == 0)
		throw std::runtime_error("Trying to remove an inactive state!");
	--it->second;

	_stateChange = true;
	_stateChangeCondition.notify_all();
}

void DebugSession::serverCommunication() {
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
						throw std::runtime_error("The server (version "s + DebugServer::version + ") is incompatible with your client!"s);
					}
					else {
						Json::Value ehlo;
						ehlo["type"] = "ehlo";
						ehlo["version"] = DebugServer::version;
						this->sendObject(ehlo);
						_heartBeat = std::thread(&DebugSession::heartBeat, this);
					}
				}
				else if(type == "start") {
					if(!_petriNetFactory.loaded()) {
						try {
							_petriNetFactory.load();
						}
						catch(std::exception &e) {
							this->sendObject(this->error("The PetriNet API has been updated after the compilation of the dynamic library, please recompile to allow debugging!"));
							logError("The PetriNet API has been updated after the compilation of the dynamic library, please recompile to allow debugging!");
						}
					}
					if(_petriNetFactory.loaded()) {
						if(root["payload"]["hash"] != _petriNetFactory.hash()) {
							this->sendObject(this->error("You are trying to run a Petri net that is different from the one which is compiled!"));
							logError("You are trying to run a Petri net that is different from the one which is compiled!");
							_petriNetFactory.unload();
						}
						else {
							if(!_petri) {
								_petri = _petriNetFactory.createDebug();
								_petri->setObserver(this);
							}
							else if(_petri->running())
								throw std::runtime_error("Petri net is already running!");

							_petri->run();

							this->sendObject(this->json("ack", "start"));
						}
					}
				}
				else if(type == "exit") {
					this->clearPetri();

					this->sendObject(this->json("exit", "kbye"));

					break;
				}
				else if(type == "stop") {
					this->clearPetri();

					this->sendObject(this->json("ack", "stop"));
				}
				else if(type == "reload") {
					this->clearPetri();
					_petriNetFactory.reload();
					_petri = _petriNetFactory.createDebug();
					_petri->setObserver(this);
				}
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

		this->clearPetri();

		logDebug0("Disconnected!");
	}

	_running = false;
	_socket.shutDown();
	_stateChangeCondition.notify_all();

	if(_petri)
		_petri->stop();
}

void DebugSession::heartBeat() {
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
				state["id"] = Json::Value(Json::UInt64(p.first->ID()));
				state["count"] = Json::Value(Json::UInt64(p.second));
				states[states.size()] = state;
			}
		}

		this->sendObject(this->json("states", states));
		_stateChange = false;
	}
}

void DebugSession::clearPetri() {
	if(_petri != nullptr) {
		_petri = nullptr;
	}
	_activeStates.clear();
}

Json::Value DebugSession::receiveObject() {
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

void DebugSession::sendObject(Json::Value const &o) {
	Json::FastWriter writer;
	std::string s = writer.write(o);

	_socket.sendMsg(_client, s.c_str(), s.size());
}

Json::Value DebugSession::json(std::string const &type, Json::Value const &payload) {
	Json::Value err;
	err["type"] = type;
	err["payload"] = payload;

	return err;
}

Json::Value DebugSession::error(std::string const &error) {
	return this->json("error", error);
}
