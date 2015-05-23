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

#include <string>
#include "PetriNet.h"
#include "jsoncpp/include/json.h"
#include <set>
#include <chrono>
#include <mutex>
#include <condition_variable>
#include <thread>
#include "Socket.h"
#include <atomic>

namespace Petri {
	
	using namespace std::string_literals;
	using namespace std::chrono_literals;

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

		void start();
		void stop();
		bool running() const;

		void addActiveState(Action &a);
		void removeActiveState(Action &a);
		void notifyStop();

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
		Petri::Socket _socket;
		Petri::Socket _client;
		std::atomic_bool _running = {false};

		PetriDynamicLibCommon &_petriNetFactory;
		std::unique_ptr<PetriDebug> _petri;
		std::mutex _sendMutex;
		std::mutex _breakpointsMutex;
		std::set<Action *> _breakpoints;
	};

}

#endif
