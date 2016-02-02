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
//  PetriNet.c
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#include "../../Cpp/Action.h"
#include "../../Cpp/Atomic.h"
#include "../../Cpp/PetriDebug.h"
#include "../../Cpp/PetriNet.h"
#include "../Action.h"
#include "../PetriNet.h"
#include "Types.hpp"

PetriNet *PetriNet_create(char const *name) {
    return new PetriNet{std::make_unique<Petri::PetriNet>(name ? name : "")};
}

PetriNet *PetriNet_createDebug(char const *name) {
    return new PetriNet{std::make_unique<Petri::PetriDebug>(name ? name : "")};
}

void PetriNet_destroy(PetriNet *pn) {
    delete pn;
}

void PetriNet_addAction(PetriNet *pn, PetriAction *action, bool active) {
    if(!action->owned) {
        std::cerr << "The action has already been added to a petri net!" << std::endl;
    } else {
        auto &a = pn->petriNet->addAction(std::move(*action->owned), active);
        action->owned.reset();
        action->notOwned = &a;
    }
}

bool PetriNet_isRunning(PetriNet *pn) {
    return pn->petriNet->running();
}

void PetriNet_run(PetriNet *pn) {
    pn->petriNet->run();
}

void PetriNet_stop(PetriNet *pn) {
    pn->petriNet->stop();
}

void PetriNet_join(PetriNet *pn) {
    pn->petriNet->join();
}

void PetriNet_addVariable(PetriNet *pn, uint32_t id) {
    pn->petriNet->addVariable(id);
}

volatile int64_t *PetriNet_getVariable(PetriNet *pn, uint32_t id) {
    return &pn->petriNet->getVariable(id).value();
}

int64_t PetriNet_getVariableValue(struct PetriNet *pn, uint32_t id) {
    return *PetriNet_getVariable(pn, id);
}

void PetriNet_setVariableValue(struct PetriNet *pn, uint32_t id, int64_t value) {
    *PetriNet_getVariable(pn, id) = value;
}

void PetriNet_lockVariable(PetriNet *pn, uint32_t id) {
    pn->petriNet->getVariable(id).getMutex().lock();
}

void PetriNet_unlockVariable(PetriNet *pn, uint32_t id) {
    pn->petriNet->getVariable(id).getMutex().unlock();
}

char const *PetriNet_getName(PetriNet *pn) {
    return pn->petriNet->name().c_str();
}
