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
//  Transition.cpp

//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#define PETRI_NEEDS_GET_TRANSITION

#include "../Transition.h"
#include "Transition.hpp"
#include "Types.hpp"
#include <chrono>

void PetriTransition_destroy(PetriTransition *transition) {
    delete transition;
}

PetriAction *PetriTransition_getPrevious(struct PetriTransition *transition) {
    return new PetriAction{nullptr, &getTransition(transition).previous()};
}

PetriAction *PetriTransition_getNext(struct PetriTransition *transition) {
    return new PetriAction{nullptr, &getTransition(transition).next()};
}

uint64_t PetriTransition_getID(PetriTransition *transition) {
    return getTransition(transition).ID();
}

void PetriTransition_setID(PetriTransition *transition, uint64_t id) {
    return getTransition(transition).setID(id);
}

bool PetriTransition_isFulfilled(PetriNet *petriNet, PetriTransition *transition, Petri_actionResult_t actionResult) {
    return getTransition(transition).isFulfilled(*petriNet->petriNet, actionResult);
}

void PetriTransition_setCondition(PetriTransition *transition, transitionCallable_t test) {
    getTransition(transition).setCondition(Petri::make_transition_callable(test));
}

void PetriTransition_setConditionWithParam(PetriTransition *transition, parametrizedTransitionCallable_t test) {
    getTransition(transition).setCondition(getParametrizedTransitionCallable(test));
}

char const *PetriTransition_getName(PetriTransition *transition) {
    return getTransition(transition).name().c_str();
}

void PetriTransition_setName(PetriTransition *transition, char const *name) {
    getTransition(transition).setName(name);
}

uint64_t PetriTransition_getDelayBetweenEvaluation(PetriTransition *transition) {
    return std::chrono::duration_cast<std::chrono::microseconds>(getTransition(transition).delayBetweenEvaluation()).count();
}

void PetriTransition_setDelayBetweenEvaluation(PetriTransition *transition, uint64_t usDelay) {
    getTransition(transition).setDelayBetweenEvaluation(std::chrono::microseconds(usDelay));
}
