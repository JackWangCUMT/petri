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
//  PetriDebug.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_PetriDebug_h
#define Petri_PetriDebug_h

#include "PetriNet.h"

namespace Petri {

    class DebugServer;
    template <typename _ReturnType>
    class ThreadPool;

    class PetriDebug : public PetriNet {
    public:
        PetriDebug(std::string const &name);

        virtual ~PetriDebug();

        /**
         * Adds an Action to the PetriNet. The net must not be running yet.
         * @param action The action to add
         * @param active Controls whether the action is active as soon as the net is started or not
         */
        virtual Action &addAction(Action action, bool active = false) override;

        /**
         * Sets the observer of the PetriDebug object. The observer will be notified by some of the
         * Petri net events, such as when a state is activated or disabled.
         * @param session The observer which will be notified of the events
         */
        void setObserver(DebugServer *session);

        /**
         * Retrieves the underlying ThreadPool object.
         * @return The underlying ThreadPool
         */
        ThreadPool<void> &actionsPool();

        /**
         * Finds the state associated to the specified ID, or nullptr if not found.
         * @param id The ID to match with a state.
         * @return The state matching ID
         */
        Action *stateWithID(uint64_t id) const;

        void stop() override;

    protected:
        struct Internals;
    };
}

#endif
