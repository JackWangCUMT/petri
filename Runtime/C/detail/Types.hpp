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
//  Types.h
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#ifndef Petri_Types_hpp
#define Petri_Types_hpp

#include "../../Cpp/Action.h"
#include "../../Cpp/DebugServer.h"
#include "../../Cpp/PetriNet.h"
#include "../../Cpp/Transition.h"
#include <memory>

class CPetriDynamicLib;

#ifndef NO_C_PETRI_NET

#include "../PetriNet.h"

struct PetriNet {
    std::unique_ptr<Petri::PetriNet> owned;
    Petri::PetriNet *notOwned;
};

#endif

struct PetriAction {
    std::unique_ptr<Petri::Action> owned;
    Petri::Action *notOwned;
};

struct PetriTransition {
    std::unique_ptr<Petri::Transition> owned;
    Petri::Transition *notOwned;
};

struct PetriDynamicLib {
    std::unique_ptr<Petri::PetriDynamicLib> lib;
};

struct PetriDebugServer {
    std::unique_ptr<Petri::DebugServer> server;
    ::PetriNet cHandle;
};

namespace {
// The following #ifdef prevent unused functions warning.
#ifdef PETRI_NEEDS_GET_ACTION
    Petri::Action &getAction(PetriAction *action) {
        if(action->owned) {
            return *action->owned;
        } else {
            return *action->notOwned;
        }
    }
#endif
#ifdef PETRI_NEEDS_GET_TRANSITION
    Petri::Transition &getTransition(PetriTransition *transition) {
        if(transition->owned) {
            return *transition->owned;
        } else {
            return *transition->notOwned;
        }
    }
#endif
#ifdef PETRI_NEEDS_GET_PETRINET
    Petri::PetriNet &getPetriNet(PetriNet *pn) {
        if(pn->owned) {
            return *pn->owned;
        } else {
            return *pn->notOwned;
        }
    }
#endif
}

#endif /* Types_h */
