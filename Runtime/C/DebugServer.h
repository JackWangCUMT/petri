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
//  DebugServer.h
//  Petri
//
//  Created by Rémi on 04/07/2015.
//

#ifndef DebugServer_c
#define DebugServer_c

#include "PetriDynamicLib.h"
#include <stdbool.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

// typedef struct PetriDebugServer PetriDebugServer;

/**
 * Returns the DebugServer API's version
 * @return The current version of the API.
 */
char const *PetriDebugServer_getVersion();

/**
 * Creates the DebugServer and binds it to the provided dynamic library.
 * @param petri The dynamic lib from which the debug server operates.
 */
struct PetriDebugServer *PetriDebugServer_create(struct PetriDynamicLib *petri);

/**
 * Destroys the debug server. If the server is running, this call will wait for the connected client
 * to end the debug session to continue the program exectution.
 * @param server The debug server to operate on.
 */
void PetriDebugServer_destroy(struct PetriDebugServer *server);

/**
 * Starts the debug server by listening on the debug port of the bound dynamic library, making it
 * ready to receive a debugger connection.
 * @param server The debug server to operate on.
 */
void PetriDebugServer_start(struct PetriDebugServer *server);

/**
 * Stops the debug server. After that, the debugging port is unbound.
 * @param server The debug server to operate on.
 */
void PetriDebugServer_stop(struct PetriDebugServer *server);

/**
 * Checks whether the debug server is running or not.
 * @param server The debug server to operate on.
 * @return true if the server is running, false otherwise.
 */
bool PetriDebugServer_isRunning(struct PetriDebugServer *server);

#ifdef __cplusplus
}
#endif

#endif /* DebugServer_c */
