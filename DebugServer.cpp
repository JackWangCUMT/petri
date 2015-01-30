//
//  DebugServer.cpp
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#include "DebugServer.h"
#include "Commun.h"
#include "PetriDynamicLibCommon.h"
#include <cstring>
#include "PetriUtils.h"

std::string DebugServer::getVersion() {
	return "0.2";
}

std::chrono::system_clock::time_point DebugServer::getAPIdate() {
	logMagenta(__TIMESTAMP__);
	return DebugServer::getDateFromTimestamp(__TIMESTAMP__);
}

std::chrono::system_clock::time_point DebugServer::getDateFromTimestamp(char const *timestamp) {
	char const *format = "%a %b %d %H:%M:%S %Y";
	std::tm tm;
	std::memset(&tm, 0, sizeof(tm));

	strptime(timestamp, format, &tm);
	return std::chrono::system_clock::from_time_t(std::mktime(&tm));
}

DebugSession::DebugSession(PetriDynamicLibCommon &petri) : _socket(SockProtocol::TCP), _client(SockProtocol::TCP), _petriNetFactory(petri), _evaluator(petri.prefix()) {}

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
	{
		std::lock_guard<std::mutex> lk(_breakpointsMutex);
		if(_breakpoints.count(&a) > 0) {
			this->setPause(true);
			this->sendObject(this->json("ack", "pause"));
		}
	}
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

	logInfo("Debug session for Petri net ", _petriNetFactory.name(), " started.");

	while(_running) {
		logInfo("Waiting for the debugger to attach…");
		_socket.accept(_client);
		logDebug0("Debugger connected!");

		try {
			while(_running && _client.getState() == SOCK_ACCEPTED) {
				auto const root = this->receiveObject();
				auto const &type = root["type"];

				if(type == "hello") {
					if(root["payload"]["version"] != DebugServer::getVersion()) {
						this->sendObject(this->error("The server (version "s + DebugServer::getVersion() + ") is incompatible with your client!"s));
						throw std::runtime_error("The server (version "s + DebugServer::getVersion() + ") is incompatible with your client!"s);
					}
					else {
						Json::Value ehlo;
						ehlo["type"] = "ehlo";
						ehlo["version"] = DebugServer::getVersion();
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

							this->sendObject(this->json("ack", "start"));

							auto const root = this->receiveObject();
							this->updateBreakpoints(root["payload"]);

							_petri->run();
						}
					}
				}
				else if(type == "exit") {
					this->clearPetri();
					this->sendObject(this->json("exit", "kbye"));

					break;
				}
				else if(type == "exitSession") {
					this->clearPetri();
					this->sendObject(this->json("exitSession", "kbye"));
					_running = false;
					
					break;
				}
				else if(type == "stop") {
					this->clearPetri();
					this->sendObject(this->json("ack", "stop"));
				}
				else if(type == "pause") {
					this->setPause(true);
					this->sendObject(this->json("ack", "pause"));
				}
				else if(type == "resume") {
					this->setPause(false);
					this->sendObject(this->json("ack", "resume"));
				}
				else if(type == "reload") {
					this->clearPetri();
					_petriNetFactory.reload();
					_petriNetFactory.hash();
					_petri = _petriNetFactory.createDebug();
					_petri->setObserver(this);
				}
				else if(type == "breakpoints") {
					this->updateBreakpoints(root["payload"]);
				}
				else if(type == "evaluate") {
					std::string result;
					try {
						_evaluator.reload();
						result = _evaluator.evaluate();
					}
					catch(std::exception &e) {
						result = "could not evaluate the symbol, reason: "s + e.what();
					}
					this->sendObject(this->json("evaluation", result));
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

void DebugSession::updateBreakpoints(Json::Value const &breakpoints) {
	if(breakpoints.type() != Json::arrayValue) {
		throw std::runtime_error("Invalid breakpoint specifying format!");
	}

	std::lock_guard<std::mutex> lk(_breakpointsMutex);
	_breakpoints.clear();
	for(Json::ArrayIndex i = Json::ArrayIndex(0); i != breakpoints.size(); ++i) {
		auto id = breakpoints[i].asUInt64();
		_breakpoints.insert(_petri->stateWithID(id));
	}
}

void DebugSession::heartBeat() {
	setThreadName("DebugSession "s + _petriNetFactory.name() + " heart beat"s);
	auto lastSendDate = std::chrono::system_clock::now();
	auto const minDelayBetweenSend = 100ms;

	while(_running && _client.getState() == SOCK_ACCEPTED) {
		std::unique_lock<std::mutex> lk(_stateChangeMutex);
		_stateChangeCondition.wait(lk, [this]() {
			return _stateChange || !_running || _client.getState() != SOCK_ACCEPTED;
		});

		if(!_running || _client.getState() != SOCK_ACCEPTED)
			break;

		auto delaySinceLastSend = std::chrono::system_clock::now() - lastSendDate;
		if(delaySinceLastSend < minDelayBetweenSend) {
			std::this_thread::sleep_until(lastSendDate + minDelayBetweenSend);
		}

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

void DebugSession::setPause(bool pause) {
	if(!_petri || !_petri->running())
		throw std::runtime_error("Petri net is not running!");

	if(pause) {
		_petri->actionsPool().pause();
	}
	else {
		_petri->actionsPool().resume();
	}
}

Json::Value DebugSession::receiveObject() {
	std::vector<uint8_t> vect = _socket.receiveNewMsg(_client);

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
	std::lock_guard<std::mutex> lk(_sendMutex);
	
	Json::FastWriter writer;
	writer.omitEndingLineFeed();

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
