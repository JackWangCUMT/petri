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
//  Action.cpp
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#define PETRI_NEEDS_GET_ACTION

#include "Action.h"
#include "../Action.h"
#include "Types.hpp"
#include <memory>

PetriAction *PetriAction_createEmpty() {
	return new PetriAction{std::make_unique<Petri::Action>(), nullptr};
}

PetriAction *PetriAction_create(uint64_t id, char const *name, callable_t action, size_t requiredTokens) {
	return new PetriAction{std::make_unique<Petri::Action>(id, name, Petri::make_action_callable(action), requiredTokens), nullptr};
}

void PetriAction_destroy(PetriAction *action) {
	delete action;
}

uint64_t PetriAction_getID(PetriAction *action) {
	return getAction(action).ID();
}

void PetriAction_setID(PetriAction *action, uint64_t id) {
	return getAction(action).setID(id);
}

void PetriAction_addTransition(PetriAction *action, PetriTransition *transition) {
	getAction(action).addTransition(std::move(*transition->owned));
	transition->owned.reset();
}

void PetriAction_createAndAddTransition(PetriAction *action, uint64_t id, char const *name, PetriAction *next, transitionCallable_t cond) {
	getAction(action).addTransition(id, name, getAction(next), Petri::make_transition_callable(cond));
}

void PetriAction_setAction(PetriAction *action, callable_t a) {
	getAction(action).setAction(Petri::make_action_callable(a));
}

size_t PetriAction_getRequiredTokens(PetriAction *action) {
	return getAction(action).requiredTokens();
}

void PetriAction_setRequiredTokens(PetriAction *action, size_t requiredTokens) {
	getAction(action).setRequiredTokens(requiredTokens);
}

size_t PetriAction_getCurrentTokens(PetriAction *action) {
	return getAction(action).currentTokens();
}

char const *PetriAction_getName(PetriAction *action) {
	return getAction(action).name().c_str();
}

void PetriAction_setName(PetriAction *action, char const *name) {
	getAction(action).setName(name);
}
