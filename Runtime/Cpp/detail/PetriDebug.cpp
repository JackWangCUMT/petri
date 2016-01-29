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
//  PetriDebug.cpp
//  Pétri
//
//  Created by Rémi on 27/11/2014.
//

#include "../Action.h"
#include "../DebugServer.h"
#include "../PetriDebug.h"
#include "PetriNetImpl.h"

namespace Petri {

    struct PetriDebug::Internals : PetriNet::Internals {
        Internals(PetriDebug &pn, std::string const &name)
                : PetriNet::Internals(pn, name) {}

        void stateEnabled(Action &a) override;
        void stateDisabled(Action &a) override;

        DebugServer *_observer = nullptr;
        std::unordered_map<uint64_t, Action *> _statesMap;
    };

    void PetriDebug::Internals::stateEnabled(Action &a) {
        if(_observer) {
            _observer->addActiveState(a);
        }
    }

    void PetriDebug::Internals::stateDisabled(Action &a) {
        if(_observer) {
            _observer->removeActiveState(a);
        }
    }

    PetriDebug::PetriDebug(std::string const &name)
            : PetriNet(std::make_unique<PetriDebug::Internals>(*this, name)) {}

    PetriDebug::~PetriDebug() = default;


    void PetriDebug::setObserver(DebugServer *session) {
        static_cast<Internals &>(*_internals)._observer = session;
    }
    Action &PetriDebug::addAction(Action action, bool active) {
        auto &a = this->PetriNet::addAction(std::move(action), active);
        static_cast<Internals &>(*_internals)._statesMap[a.ID()] = &a;

        return a;
    }

    void PetriDebug::stop() {
        if(static_cast<Internals &>(*_internals)._observer) {
            static_cast<Internals &>(*_internals)._observer->notifyStop();
        }
        this->PetriNet::stop();
    }

    Action *PetriDebug::stateWithID(uint64_t id) const {
        auto it = static_cast<Internals &>(*_internals)._statesMap.find(id);
        if(it != static_cast<Internals &>(*_internals)._statesMap.end())
            return it->second;
        else
            return nullptr;
    }

    ThreadPool<void> &PetriDebug::actionsPool() {
        return _internals->_actionsPool;
    }
}
