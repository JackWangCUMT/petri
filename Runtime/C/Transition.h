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
//  Transition.h
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#ifndef Petri_Transition_C
#define Petri_Transition_C

#include "Types.h"
#include <stdbool.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif
struct PetriTransition;
struct PetriAction;
struct PetriNet;

// typedef struct PetriTransition PetriTransition;

typedef bool (*transitionCallable_t)(Petri_actionResult_t);
typedef bool (*parametrizedTransitionCallable_t)(struct PetriNet *, Petri_actionResult_t);

/**
 * Destroys a PetriAction instance created by one of the PetriAction_create functions.
 * @param transition The PetriTransition instance to destroy.
 */
void PetriTransition_destroy(struct PetriTransition *transition);

struct PetriAction *PetriTransition_getPrevious(struct PetriTransition *transition);
struct PetriAction *PetriTransition_getNext(struct PetriTransition *transition);

/**
 * Returns the ID of the PetriTransition.
 * @param transition The PetriTransition to query.
 */
uint64_t PetriTransition_getID(struct PetriTransition *transition);

/**
 * Changes the ID of the PetriTransition.
 * @param transition The PetriTransition to change.
 * @param id The new ID.
 */
void PetriTransition_setID(struct PetriTransition *transition, uint64_t id);

/**
 * Checks whether the PetriTransition can be crossed
 * @param transition The PetriTransition instance to test against.
 * @param actionResult The result of the Action 'previous'. This is useful when the
 * PetriTransition's test uses this value.
 * @return The result of the test, true meaning that the PetriTransition can be crossed to enable
 * the action 'next'
 */
bool PetriTransition_isFulfilled(struct PetriTransition *transition, struct PetriNet *petriNet, Petri_actionResult_t actionResult);

/**
 * Changes the condition associated to the PetriTransition
 * @param transition The PetriTransition instance to change.
 * @param test The new condition to associate to the PetriTransition
 */
void PetriTransition_setCondition(struct PetriTransition *transition, transitionCallable_t test);
void PetriTransition_setConditionWithParam(struct PetriTransition *transition, parametrizedTransitionCallable_t test);

/**
 * Gets the name of the PetriTransition.
 * @param transition The PetriTransition instance to query.
 * @return The name of the PetriTransition.
 */
char const *PetriTransition_getName(struct PetriTransition *transition);

/**
 * Changes the name of the PetriTransition.
 * @param transition The PetriTransition instance to change.
 * @param name The new name of the PetriTransition.
 */
void PetriTransition_setName(struct PetriTransition *transition, char const *name);

/**
 * The delay in microseconds between successive evaluations of the PetriTransition. The runtime will
 * not try to evaluate
 * the PetriTransition with a delay smaller than this delay after a previous evaluation, but only
 * for one execution of PetriAction 'previous'
 * @param transition The PetriTransition instance to query.
 * @return The minimal delay between two evaluations of the PetriTransition.
 */
uint64_t PetriTransition_getDelayBetweenEvaluation(struct PetriTransition *transition);

/**
 * Changes the delay between successive evaluations of the PetriTransition.
 * @param transition The PetriTransition instance to change.
 * @param usDelay The new minimal delay in microseconds between two evaluations of the
 * PetriTransition.
 */
void PetriTransition_setDelayBetweenEvaluation(struct PetriTransition *transition, uint64_t usDelay);

/**
 * References the variable in the transition
 * @param transition The transition
 * @param id The identifier of the variable
 */
void PetriTransition_addVariable(struct PetriTransition *transition, uint32_t id);

#ifdef __cplusplus
}
#endif

#endif /* Transition_c */
