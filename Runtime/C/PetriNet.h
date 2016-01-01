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
//  PetriNet.h
//  Petri
//
//  Created by Rémi on 25/06/2015.
//

#ifndef Petri_PetriNet_C
#define Petri_PetriNet_C

#include <stdbool.h>
#include <stdint.h>

#include "Action.h"

#ifdef __cplusplus
extern "C" {
#endif

// typedef struct PetriNet PetriNet;

/**
 * Creates the PetriNet, assigning it a name which serves debug purposes
 * @param name The name to assign to the PetriNet, or a designated one if empty or NULL
 * @return The PetriNet instance, or NULL if an error occurred.
 */
struct PetriNet *PetriNet_create(char const *name);

/**
 * Creates the PetriNet, along with some debugging facilities.
 * @param name The name to assign to the PetriNet, or a designated one if empty or NULL
 * @return The PetriNet instance, or NULL if an error occurred.
 */
struct PetriNet *PetriNet_createDebug(char const *name);

/**
 * Destroys a PetriNet instance created by the PetriNet_create* functions.
 * @param pn The PetriNet instance to destroy.
 */
void PetriNet_destroy(struct PetriNet *pn);

/**
 * Adds a PetriAction to the PetriNet. The net must not be running yet.
 * Once this function has been called, the handle to the action may not
 * be added to a petri net again, but is still needs to be free()d.
 * @param pn The Petri Net to add add the action to
 * @param action The action to add
 * @param active Controls whether the action is active as soon as the petri net is started or not.
 */
void PetriNet_addAction(struct PetriNet *pn, struct PetriAction *action, bool active);

/**
 * Checks whether the net is running.
 * @param pn The Petri Net on which the test will be performed
 * @return true means that the net has been started, and we can not add any more action to it now.
 */
bool PetriNet_isRunning(struct PetriNet *pn);

/**
 * Starts the Petri net. It must not be already running. If no states are initially active, this is
 * a no-op.
 * @param pn The Petri Net to start
 */
void PetriNet_run(struct PetriNet *pn);

/**
 * Stops the Petri net. It blocks the calling thread until all running states are finished,
 * but do not allows new states to be enabled. If the net is not running, this is a no-op.
 * @param pn The Petri Net to stop.
 */
void PetriNet_stop(struct PetriNet *pn);

/**
 * Blocks the calling thread until the Petri net has completed its whole execution.
 * @param pn The Petri Net to join.
 */
void PetriNet_join(struct PetriNet *pn);

/**
 * Adds an Atomic variable designated by the specified id.
 * @param pn The Petri Net to add the variable to.
 * @param id The id of the new Atomic variable.
 */
void PetriNet_addVariable(struct PetriNet *pn, uint32_t id);

/**
 * Gets the value of the Atomic variable designated by the specified id.
 * @param pn The Petri Net to add the variable to.
 * @param id The id of the new Atomic variable.
 * @return The value of the Atomic variable.
 */
int64_t PetriNet_getVariable(struct PetriNet *pn, uint32_t id);

/**
 * Locks the Atomic variable designated by the specified id.
 * @param pn The Petri Net containing the variable to lock.
 * @param id The id of the Atomic variable.
 */
void PetriNet_lockVariable(struct PetriNet *pn, uint32_t id);

/**
 * Unlocks the Atomic variable designated by the specified id, provided it has already been locked.
 * The behavior is unspecified otherwise.
 * @param pn The Petri Net containing the variable to unlock.
 * @param id The id of the Atomic variable.
 */
void PetriNet_unlockVariable(struct PetriNet *pn, uint32_t id);

char const *PetriNet_getName(struct PetriNet *pn);

#ifdef __cplusplus
}
#endif

#endif /* PetriNet_c */
