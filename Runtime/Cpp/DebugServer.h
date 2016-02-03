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
#include <chrono>
#include <string>

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

        /**
         * Waits for the debug server session to end.
         */
        void join() const;

        /**
         * Retrieves the currently running petri net, if any, and nullptr otherwise.
         * @return The currently running petri net.
         */
        PetriDebug *currentPetriNet();

    protected:
        void addActiveState(Action &a);
        void removeActiveState(Action &a);
        void notifyStop();

    private:
        struct Internals;
        std::unique_ptr<Internals> _internals;
    };
}

#endif
