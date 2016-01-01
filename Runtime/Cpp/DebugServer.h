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
//  DebugServer.h
//  Pétri
//
//  Created by Rémi on 23/11/2014.
//

#ifndef Petri_DebugServer_h
#define Petri_DebugServer_h

#include "PetriNet.h"
#include "Socket.h"
#include <json/json.h>
#include <atomic>
#include <chrono>
#include <condition_variable>
#include <mutex>
#include <set>
#include <string>
#include <thread>

namespace Petri {

    using namespace std::string_literals;
    using namespace std::chrono_literals;

    class PetriDynamicLib;
    class PetriDebug;

    class DebugServer {
        friend class PetriDebug;

    public:
        /**
         * Returns the DebugServer API's version
         * @return The current version of the API.
         */
        static std::string const &getVersion();

        /**
         * Returns the date on which the API was compiled.
         * @return The API compilation date.
         */
        static std::chrono::system_clock::time_point getAPIdate();

        /*
         * Converts a timestamp string to a date.
         * @param timestamp The timestamp to convert.
         * @return The conversion result.
         */
        static std::chrono::system_clock::time_point getDateFromTimestamp(char const *timestamp);

        /**
         * Creates the DebugServer and binds it to the provided dynamic library.
         * @param petri The dynamic lib from which the debug server operates.
         */
        DebugServer(PetriDynamicLib &petri);

        /**
         * Destroys the debug server. If the server is running, a deleted or out of scope
         * DebugServer
         * object will wait for the connected client to end the debug session to continue the
         * program exectution.
         */
        ~DebugServer();

        DebugServer(DebugServer const &) = delete;
        DebugServer &operator=(DebugServer const &) = delete;

        /**
         * Starts the debug server by listening on the debug port of the bound dynamic library,
         * making it ready to receive a debugger connection.
         */
        void start();

        /**
         * Stops the debug server. After that, the debugging port is unbound.
         */
        void stop();

        /**
         * Checks whether the debug server is running or not.
         * @return true if the server is running, false otherwise.
         */
        bool running() const;

    protected:
        void addActiveState(Action &a);
        void removeActiveState(Action &a);
        void notifyStop();

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
        Petri::Socket _socket;
        Petri::Socket _client;
        std::atomic_bool _running = {false};

        PetriDynamicLib &_petriNetFactory;
        std::unique_ptr<PetriDebug> _petri;
        std::mutex _sendMutex;
        std::mutex _breakpointsMutex;
        std::set<Action *> _breakpoints;
    };
}

#endif
