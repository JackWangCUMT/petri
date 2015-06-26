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
//  Action.h
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#ifndef Petri_Action_C
#define Petri_Action_C

#include <stdint.h>
#include <stdlib.h>
#include "Transition.h"
#include "Types.h"

#ifdef __cplusplus
extern "C" {
#endif

	typedef Petri_actionResult_t (*callable_t)();

	typedef struct PetriAction PetriAction;

	/**
	 * Creates an empty action, associated to a null CallablePtr.
	 */
	PetriAction *PetriAction_createEmpty();

	/**
	 * Creates an empty action, associated to a copy ofthe specified Callable.
	 * @param action The Callable which will be copied
	 */
	PetriAction *PetriAction_create(uint64_t id, char const *name, callable_t action, size_t requiredTokens);

	/**
	 * Destroys a PetriAction instance created by one of the PetriAction_create functions.
	 * @param action The PetriAction instance to destroy.
	 */
	void PetriAction_destroy(PetriAction *action);

	/**
	 * Returns the ID of the PetriAction.
	 * @param action The PetriAction to query.
	 */
	uint64_t PetriAction_getID(PetriAction *action);

	/**
	 * Changes the ID of the PetriAction.
	 * @param action The PetriAction to change.
	 * @param id The new ID.
	 */
	void PetriAction_setID(PetriAction *action, uint64_t id);

	/**
	 * Adds a PetriTransition to the PetriAction. The PetriTransition handle is invalidated after this call.
	 * @param action The PetriAction instance to add the Transition to.
	 * @param transition The transition to be added
	 */
	void PetriAction_addTransition(PetriAction *action, PetriTransition *transition);

	/**
	 * Adds a PetriTransition to the PetriAction.
	 * @param action The PetriAction instance to add the PetriTransition to.
	 * @param id The id of the Transition
	 * @param name The name of the transition to be added
	 * @param next The Action following the transition to be added
	 * @param cond The condition of the Transition to be added
	 */
	void PetriAction_createAndAddTransition(PetriAction *action, uint64_t id, char const *name, PetriAction *next, callable_t cond);

	/**
	 * Changes the action associated to the PetriAction
	 * @param action The PetriAction instance of which the action will be changed.
	 * @param a The Callable which will be copied and put in the Action
	 */
	void PetriAction_setAction(PetriAction *action, callable_t a);

	/**
	 * Returns the required tokens of the Action to be activated, i.e. the count of Actions which must lead to *this and terminate for *this to activate.
	 * @return The required tokens of the Action
	 */
	size_t PetriAction_getRequiredTokens(PetriAction *action);

	/**
	 * Changes the required tokens of the Action to be activated.
	 * @param requiredTokens The new required tokens count
	 * @return The required tokens of the Action
	 */
	void PetriAction_setRequiredTokens(PetriAction *action, size_t requiredTokens);

	/**
	 * Gets the current tokens count given to the Action by its preceding Actions.
	 * @return The current tokens count of the Action
	 */
	size_t PetriAction_getCurrentTokens(PetriAction *action);

	/**
	 * Returns the name of the Action.
	 * @return The name of the Action
	 */
	char const *PetriAction_getName(PetriAction *action);

	/**
	 * Sets the name of the Action
	 * @param name The name of the Action
	 */
	void PetriAction_setName(PetriAction *action, char const *name);

#ifdef __cplusplus
}
#endif


#endif /* Action_c */
