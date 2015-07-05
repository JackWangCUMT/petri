/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

//
//  DebugServer.cpp
//  Pétri
//
//  Created by Rémi on 23/11/2014.
//

#include "DebugServer.h"
#include <cstring>
#include <string>
#include "PetriDynamicLib.h"
#include "Action.h"
#include "ThreadPool.h"

namespace Petri {

	std::string const &DebugServer::getVersion() {
		static auto const version = "0.2"s;
		return version;
	}

	std::chrono::system_clock::time_point DebugServer::getAPIdate() {
		return DebugServer::getDateFromTimestamp(__TIMESTAMP__);
	}

	std::chrono::system_clock::time_point DebugServer::getDateFromTimestamp(char const *timestamp) {
		char const *format = "%a %b %d %H:%M:%S %Y";
		std::tm tm;
		std::memset(&tm, 0, sizeof(tm));

		strptime(timestamp, format, &tm);
		return std::chrono::system_clock::from_time_t(std::mktime(&tm));
	}

	DebugServer::DebugServer(PetriDynamicLib &petri) : _socket(), _client(), _petriNetFactory(petri) {}

	DebugServer::~DebugServer() {
		if(_receptionThread.joinable())
			_receptionThread.join();
		if(_heartBeat.joinable())
			_heartBeat.join();
	}

	void DebugServer::start() {
		_running = true;
		_receptionThread = std::thread(&DebugServer::serverCommunication, this);
	}

	void DebugServer::stop() {
		_running = false;

		if(_receptionThread.joinable())
			_receptionThread.join();
		if(_heartBeat.joinable())
			_heartBeat.join();
	}

	bool DebugServer::running() const {
		return _running;
	}

	void DebugServer::addActiveState(Action &a) {
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

	void DebugServer::removeActiveState(Action &a) {
		std::lock_guard<std::mutex> lk(_stateChangeMutex);
		auto it = _activeStates.find(&a);
		if(it == _activeStates.end() || it->second == 0)
			throw std::runtime_error("Trying to remove an inactive state!");
		--it->second;

		_stateChange = true;
		_stateChangeCondition.notify_all();
	}

	void DebugServer::notifyStop() {
		this->sendObject(this->json("ack", "stop"));
	}

	void DebugServer::serverCommunication() {
		setThreadName(std::string("DebugServer ") + _petriNetFactory.name());

		int attempts = 0;
		while(true) {
			if(_socket.listen(_petriNetFactory.port()))
				break;

			std::this_thread::sleep_for(1s);
			std::cerr << "Could not bind socket to requested port (attempt " << ++attempts << ")" << std::endl;
			if(attempts > 20) {
				_running = false;
				std::cerr << "Too many attemps, aborting." << std::endl;
				break;
			}
		}

		std::cout << "Debug session for Petri net " << _petriNetFactory.name() << " started." << std::endl;

		while(_running) {
			std::cout << "Waiting for the debugger to attach…" << std::endl;
			_socket.accept(_client);
			std::cout << "Debugger connected!" << std::endl;

			try {
				while(_running && _client.getState() == Socket::SOCK_ACCEPTED) {
					auto const root = this->receiveObject();
					auto const &type = root["type"];

					if(type == "hello") {
						if(root["payload"]["version"] != DebugServer::getVersion()) {
							this->sendObject(this->error(std::string("The server (version ") + DebugServer::getVersion() + std::string(") is incompatible with your client!")));
							throw std::runtime_error(std::string("The server (version ") + DebugServer::getVersion() + std::string(") is incompatible with your client!"));
						}
						else {
							Json::Value ehlo;
							ehlo["type"] = "ehlo";
							ehlo["version"] = DebugServer::getVersion();
							this->sendObject(ehlo);
							_heartBeat = std::thread(&DebugServer::heartBeat, this);
						}
					}
					else if(type == "start") {
						if(!_petriNetFactory.loaded()) {
							try {
								_petriNetFactory.load();
							}
							catch(std::exception &e) {
								this->sendObject(this->error(std::string("An exception occurred upon dynamic lib loading (") + e.what() + ")!"));
								std::cerr << "An exception occurred upon dynamic lib loading (" << e.what() << ")!" << std::endl;
							}
						}
						if(_petriNetFactory.loaded()) {
							if(root["payload"]["hash"].asString() != _petriNetFactory.hash()) {
								std::cerr << "Requested hash: " <<  root["payload"]["hash"].asString() << std::endl;
								std::cerr << "Got hash: " << _petriNetFactory.hash() << std::endl;
								std::cout << root["payload"]["hash"].asString() << " " << _petriNetFactory.hash() << std::endl;
								this->sendObject(this->error("You are trying to run a Petri net that is different from the one which is compiled!"));
								std::cerr << "You are trying to run a Petri net that is different from the one which is compiled!" << std::endl;
								_petriNetFactory.unload();
							}
							else {
								if(!_petri) {
									_petri = _petriNetFactory.createDebug();
									_petri->setObserver(this);
								}
								else if(_petri->running()) {
									throw std::runtime_error("Petri net is already running!");
								}

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
						std::cout << "Reloaded Petri Net." << std::endl;
						std::cout << "Got hash: " << _petriNetFactory.hash() << std::endl;
						this->sendObject(this->json("ack", "reload"));
					}
					else if(type == "breakpoints") {
						this->updateBreakpoints(root["payload"]);
					}
					else if(type == "evaluate") {
						std::string result, lib;
						try {
							lib = root["payload"]["lib"].asString();
							DynamicLib dl(lib);
							dl.load();
							auto eval = dl.loadSymbol<char const *(void *)>(_petriNetFactory.prefix() + std::string("_evaluate"));
							result = eval(static_cast<void *>(_petri.get()));
						}
						catch(std::exception &e) {
							result = std::string("could not evaluate the symbol, reason: ") + e.what();
						}
						Json::Value payload;
						payload["eval"] = result;
						payload["lib"] = lib;

						this->sendObject(this->json("evaluation", payload));
					}
				}
			}
			catch(std::exception &e) {
				this->sendObject(this->json("exit", e.what()));
				std::cerr << "Caught exception, exiting debugger: " << e.what() << std::endl;
			}
			_client.shutdown();
			_stateChangeCondition.notify_all();
			if(_heartBeat.joinable())
				_heartBeat.join();

			this->clearPetri();

			std::cout << "Disconnected!" << std::endl;
		}

		_running = false;
		_socket.shutdown();
		_stateChangeCondition.notify_all();

		if(_petri)
			_petri->stop();
	}

	void DebugServer::updateBreakpoints(Json::Value const &breakpoints) {
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

	void DebugServer::heartBeat() {
		setThreadName(std::string("DebugServer ") + _petriNetFactory.name() + std::string(" heart beat"));
		auto lastSendDate = std::chrono::system_clock::now();
		auto const minDelayBetweenSend = 100ms;

		while(_running && _client.getState() == Petri::Socket::SOCK_ACCEPTED) {
			std::unique_lock<std::mutex> lk(_stateChangeMutex);
			_stateChangeCondition.wait(lk, [this]() {
				return _stateChange || !_running || _client.getState() != Petri::Socket::SOCK_ACCEPTED;
			});

			if(!_running || _client.getState() != Petri::Socket::SOCK_ACCEPTED)
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

	void DebugServer::clearPetri() {
		if(_petri != nullptr) {
			_petri = nullptr;
		}
		_activeStates.clear();
	}

	void DebugServer::setPause(bool pause) {
		if(!_petri || !_petri->running())
			throw std::runtime_error("Petri net is not running!");

		if(pause) {
			_petri->actionsPool().pause();
		}
		else {
			_petri->actionsPool().resume();
		}
	}

	Json::Value DebugServer::receiveObject() {
		std::vector<uint8_t> vect = _socket.receiveNewMsg(_client);

		std::string msg(vect.begin(), vect.end());

		Json::Value root;
		Json::Reader reader;
		if(!reader.parse(&msg.data()[0], &msg.data()[msg.length()], root)) {
			std::cerr << "Invalid debug message received from server: " << msg << std::endl;
			throw std::runtime_error("Invalid debug message received!");
		}

		return root;
	}

	void DebugServer::sendObject(Json::Value const &o) {
		std::lock_guard<std::mutex> lk(_sendMutex);

		Json::FastWriter writer;
		writer.omitEndingLineFeed();

		std::string s = writer.write(o);

		_socket.sendMsg(_client, s.c_str(), s.size());
	}
	
	Json::Value DebugServer::json(std::string const &type, Json::Value const &payload) {
		Json::Value err;
		err["type"] = type;
		err["payload"] = payload;
		
		return err;
	}
	
	Json::Value DebugServer::error(std::string const &error) {
		return this->json("error", error);
	}
	
}
