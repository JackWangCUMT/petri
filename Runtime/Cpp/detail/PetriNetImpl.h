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
//  PetriImp.h
//  Pétri
//
//  Created by Rémi on 04/05/2015.
//

#ifndef IA_Pe_tri_PetriImp_h
#define IA_Pe_tri_PetriImp_h

#include "../Action.h"
#include "../Atomic.h"
#include "../Common.h"
#include "../Transition.h"
#include "ThreadPool.h"
#include <atomic>
#include <cassert>
#include <deque>
#include <list>
#include <map>
#include <mutex>
#include <queue>
#include <set>
#include <thread>
#include <unordered_map>

namespace Petri {
    enum { InitialThreadsActions = 1 };
    using ClockType =
    std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

    struct PetriNet::Internals {

        Internals(PetriNet &pn, std::string const &name)
                : _actionsPool(InitialThreadsActions, name.empty() ? "Anonymous PetriNet" : name)
                , _name(name.empty() ? "Anonymous PetriNet" : name)
                , _this(pn) {}
        virtual ~Internals() {}

        // This method is executed concurrently on the thread pool.
        virtual void executeState(Action &a);

        virtual void stateEnabled(Action &) {}
        virtual void stateDisabled(Action &) {}

        void enableState(Action &a);
        void disableState(Action &a);
        void swapStates(Action &oldAction, Action &newAction);

        std::condition_variable _activationCondition;
        std::multiset<Action *> _activeStates;
        std::mutex _activationMutex;

        std::atomic_bool _running = {false};
        ThreadPool<void> _actionsPool;

        std::string const _name;
        std::list<std::pair<Action, bool>> _states;
        std::list<Transition> _transitions;

        std::map<std::uint_fast32_t, std::unique_ptr<Atomic>> _variables;

        PetriNet &_this;
    };
}


#endif
