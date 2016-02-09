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

#include "../../C/detail/Types.hpp"
#include "../Action.h"
#include "../DebugServer.h"
#include "../PetriDynamicLib.h"
#include "Socket.h"
#include "ThreadPool.h"
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdocumentation"
#endif
#include "jsoncpp/include/json/json.h"
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#include <atomic>
#include <condition_variable>
#include <cstring>
#include <mutex>
#include <set>
#include <string>
#include <thread>

using namespace std::string_literals;

namespace Petri {

    struct DebugServer::Internals {
        Internals(DebugServer &that, PetriDynamicLib &lib)
                : _that(that)
                , _socket()
                , _client()
                , _petriNetFactory(lib) {}

        DebugServer &_that;

        void addActiveState(Action &a);
        void removeActiveState(Action &a);
        void notifyStop();

        void serverCommunication();
        void heartBeat();

        void startPetri(Json::Value const &paylod);
        void evaluate(Json::Value const &payload);
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
        std::unique_ptr<Petri::Socket> _socket;
        Petri::Socket _client;
        std::atomic_bool _running = {false};

        PetriDynamicLib &_petriNetFactory;
        std::unique_ptr<PetriDebug> _petri;
        std::mutex _sendMutex;
        std::mutex _breakpointsMutex;
        std::set<Action *> _breakpoints;
    };

    std::string const &DebugServer::getVersion() {
        static auto const version = "1.3.3"s;
        return version;
    }

    DebugServer::DebugServer(PetriDynamicLib &petri)
            : _internals(std::make_unique<Internals>(*this, petri)) {}

    DebugServer::~DebugServer() {
        this->join();
    }

    void DebugServer::start() {
        _internals->_running = true;
        _internals->_receptionThread = std::thread(&DebugServer::Internals::serverCommunication, &*_internals);
    }

    void DebugServer::stop() {
        _internals->_running = false;
        _internals->_client.shutdown();
        this->join();
    }

    void DebugServer::join() const {
        if(_internals->_receptionThread.joinable()) {
            _internals->_receptionThread.join();
        }
        if(_internals->_heartBeat.joinable()) {
            _internals->_heartBeat.join();
        }
    }

    bool DebugServer::running() const {
        return _internals->_running;
    }

    PetriDebug *DebugServer::currentPetriNet() {
        return _internals->_petri.get();
    }

    void DebugServer::addActiveState(Action &a) {
        _internals->addActiveState(a);
    }

    void DebugServer::removeActiveState(Action &a) {
        _internals->removeActiveState(a);
    }

    void DebugServer::notifyStop() {
        _internals->notifyStop();
    }

    void DebugServer::Internals::addActiveState(Action &a) {
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

    void DebugServer::Internals::removeActiveState(Action &a) {
        std::lock_guard<std::mutex> lk(_stateChangeMutex);
        auto it = _activeStates.find(&a);
        if(it == _activeStates.end() || it->second == 0) {
            throw std::runtime_error("Trying to remove an inactive state!");
        }
        --it->second;

        _stateChange = true;
        _stateChangeCondition.notify_all();
    }

    void DebugServer::Internals::notifyStop() {
        this->sendObject(this->json("ack", "stopped"));
    }

    void DebugServer::Internals::serverCommunication() {
        setThreadName("DebugServer " + _petriNetFactory.name());

        _socket = std::make_unique<Socket>();
        if(!_socket->listen(_petriNetFactory.port())) {
            _running = false;
        }

        if(_running) {
            std::cout << "Debug session for Petri net " << _petriNetFactory.name() << " started." << std::endl;
        }

        while(_running) {
            std::cout << "Waiting for the debugger to attach…" << std::endl;
            _socket->setBlocking(false);
            while(_running && !_socket->accept(_client)) {
                std::this_thread::sleep_for(20ms);
            }
            _socket->setBlocking(true);
            _client.setBlocking(true);

            if(!_running) {
                break;
            }

            std::cout << "Debugger connected!" << std::endl;

            try {
                while(_running && _client.getState() == Socket::SOCK_ACCEPTED) {
                    auto const root = this->receiveObject();
                    auto const &type = root["type"];

                    if(type == "hello") {
                        if(root["payload"]["version"] != DebugServer::getVersion()) {
                            this->sendObject(this->error("The server (version " + DebugServer::getVersion() +
                                                         ") is incompatible with your client!"));
                            throw std::runtime_error("The server (version " + DebugServer::getVersion() +
                                                     ") is incompatible with your client!");
                        } else {
                            Json::Value ehlo;
                            ehlo["type"] = "ehlo";
                            ehlo["version"] = DebugServer::getVersion();
                            this->sendObject(ehlo);
                            _heartBeat = std::thread(&DebugServer::Internals::heartBeat, this);
                        }
                    } else if(type == "start") {
                        this->startPetri(root["payload"]);
                    } else if(type == "detach") {
                        this->clearPetri();
                        this->sendObject(this->json("detach", "kbye"));

                        break;
                    } else if(type == "detachAndExit") {
                        this->clearPetri();
                        this->sendObject(this->json("detachAndExit", "kbye"));
                        _running = false;

                        break;
                    } else if(type == "stop") {
                        this->clearPetri();
                        this->sendObject(this->json("ack", "stop"));
                    } else if(type == "pause") {
                        this->setPause(true);
                        this->sendObject(this->json("ack", "pause"));
                    } else if(type == "resume") {
                        this->setPause(false);
                        this->sendObject(this->json("ack", "resume"));
                    } else if(type == "reload") {
                        this->clearPetri();
                        _petriNetFactory.reload();
                        _petriNetFactory.hash();
                        _petri = _petriNetFactory.createDebug();
                        _petri->setObserver(&_that);
                        std::cout << "Reloaded Petri Net." << std::endl;
                        std::cout << "New hash: " << _petriNetFactory.hash() << std::endl;
                        this->sendObject(this->json("ack", "reload"));
                    } else if(type == "breakpoints") {
                        this->updateBreakpoints(root["payload"]);
                    } else if(type == "evaluate") {
                        this->evaluate(root["payload"]);
                    }
                }
            } catch(std::exception const &e) {
                this->sendObject(this->json("detach", e.what()));
                std::cerr << "Caught exception, detaching from client: " << e.what() << std::endl;
            }
            _client.shutdown();
            _stateChangeCondition.notify_all();
            if(_heartBeat.joinable())
                _heartBeat.join();

            this->clearPetri();

            std::cout << "Disconnected!" << std::endl;
        }

        _running = false;
        _socket = nullptr;
        _stateChangeCondition.notify_all();

        if(_petri) {
            _petri->stop();
        }
    }

    void DebugServer::Internals::updateBreakpoints(Json::Value const &breakpoints) {
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

    void DebugServer::Internals::heartBeat() {
        setThreadName("DebugServer " + _petriNetFactory.name() + " heart beat");
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

    void DebugServer::Internals::startPetri(Json::Value const &payload) {
        if(!_petriNetFactory.loaded()) {
            try {
                _petriNetFactory.load();
            } catch(std::exception const &e) {
                this->sendObject(
                this->error("An exception occurred upon dynamic lib loading ("s + e.what() + ")!"));
                std::cerr << "An exception occurred upon dynamic lib loading (" << e.what() << ")!"
                          << std::endl;
            }
        }
        if(_petriNetFactory.loaded()) {
            if(payload["hash"].asString() != _petriNetFactory.hash()) {
                std::cout << payload["hash"].asString() << " " << _petriNetFactory.hash() << std::endl;
                this->sendObject(this->error("You are trying to run a Petri net that is different "
                                             "from the one which is compiled!"));
                std::cerr << "You are trying to run a Petri net that is different "
                             "from the one which is compiled!"
                          << std::endl;
                _petriNetFactory.unload();
            } else {
                if(!_petri) {
                    _petri = _petriNetFactory.createDebug();
                    _petri->setObserver(&_that);
                } else if(_petri->running()) {
                    throw std::runtime_error("The petri net is already running!");
                }

                this->sendObject(this->json("ack", "start"));

                auto const obj = this->receiveObject();
                this->updateBreakpoints(obj["payload"]);

                _petri->run();
            }
        }
    }

    void DebugServer::Internals::evaluate(Json::Value const &payload) {
        std::string result, lib;
        try {
            lib = payload["lib"].asString();
            std::string language = payload["language"].asString();
            DynamicLib dl(false, lib);
            dl.load();
            auto eval = dl.loadSymbol<char *(void *)>(_petriNetFactory.prefix() + "_evaluate"s);

            void *petriNet = nullptr;
            if(language == "C") {
                // Some circumvolutions required to pass a C petri net handle to the
                // evaluate function.
                ::PetriNet *pn = new ::PetriNet;
                pn->notOwned = _petri.get();
                petriNet = pn;
            }

            else {
                petriNet = _petri.get();
            }
            char *evalBuffer = eval(petriNet);

            if(language == "C") {
                // Some circumvolutions required to pass a C petri net handle to the
                // evaluate function.
                ::PetriNet *pn = static_cast<::PetriNet *>(petriNet);
                delete pn;
            }

            if(evalBuffer == nullptr) {
                throw std::runtime_error("Invalid evaluation result");
            }
            result = evalBuffer;
            free(evalBuffer);
        } catch(std::exception const &e) {
            result = "Could not evaluate the symbol, reason: "s + e.what();
        }
        Json::Value answer;
        answer["eval"] = result;
        answer["lib"] = lib;

        this->sendObject(this->json("evaluation", answer));
    }

    void DebugServer::Internals::clearPetri() {
        if(_petri != nullptr) {
            _petri = nullptr;
        }
        _activeStates.clear();
    }

    void DebugServer::Internals::setPause(bool pause) {
        if(!_petri || !_petri->running())
            throw std::runtime_error("Petri net is not running!");

        if(pause) {
            _petri->actionsPool().pause();
        } else {
            _petri->actionsPool().resume();
        }
    }

    Json::Value DebugServer::Internals::receiveObject() {
        std::vector<uint8_t> vect = _socket->receiveNewMsg(_client);

        std::string msg(vect.begin(), vect.end());

        Json::Value root;
        Json::Reader reader;
        if(!reader.parse(&msg.data()[0], &msg.data()[msg.length()], root)) {
            std::cerr << "Invalid debug message received from client: \"" << msg << "\"" << std::endl;
            throw std::runtime_error("Invalid debug message received!");
        }

        return root;
    }

    void DebugServer::Internals::sendObject(Json::Value const &o) {
        std::lock_guard<std::mutex> lk(_sendMutex);

        Json::FastWriter writer;
        writer.omitEndingLineFeed();

        std::string s = writer.write(o);

        _socket->sendMsg(_client, s.c_str(), s.size());
    }

    Json::Value DebugServer::Internals::json(std::string const &type, Json::Value const &payload) {
        Json::Value err;
        err["type"] = type;
        err["payload"] = payload;

        return err;
    }

    Json::Value DebugServer::Internals::error(std::string const &error) {
        return this->json("error", error);
    }
}
