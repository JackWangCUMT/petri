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
//  PetriUtils.h
//  Petri
//
//  Created by Rémi on 01/07/2015.
//

#ifndef PetriUtils_c
#define PetriUtils_c

#include "Types.h"
#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

struct PetriDynamicLib;

enum ActionResult { OK, NOK };

Petri_actionResult_t PetriUtility_pause(uint64_t usdelay);
Petri_actionResult_t PetriUtility_printAction(char const *name, uint64_t id);
Petri_actionResult_t PetriUtility_doNothing();

bool PetriUtility_returnTrue(Petri_actionResult_t res);

struct PetriDynamicLib *Petri_loadPetriDynamicLib(char const *path, char const *prefix);

#ifdef __cplusplus
}
#endif

#endif /* PetriUtils_c */
