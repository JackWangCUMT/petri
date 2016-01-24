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
//  PetriDynamicLib.c
//  Petri
//
//  Created by Rémi on 02/07/2015.
//

#include "PtrPetriDynamicLib.hpp"
#include "PetriDynamicLib.h"
#include "PetriDynamicLib.hpp"
#include "Types.hpp"

PetriDynamicLib *PetriDynamicLib_create(char const *name, char const *prefix, uint16_t port) {
    return new PetriDynamicLib{std::make_unique<CPetriDynamicLib>(name, prefix, port)};
}

PetriDynamicLib *PetriDynamicLib_createWithPtr(void *(*createPtr)(), void *(*createDebugPtr)(), char const *(*hashPtr)(), char const *(*namePtr)(), uint16_t (*portPtr)(), char const *(*prefixPtr)()) {
    return new PetriDynamicLib{std::make_unique<Petri::PtrPetriDynamicLib>(createPtr, createDebugPtr, hashPtr, namePtr, portPtr, prefixPtr)};
}

void PetriDynamicLib_destroy(PetriDynamicLib *lib) {
    delete lib;
}

PetriNet *PetriDynamicLib_createPetriNet(PetriDynamicLib *lib) {
    try {
        return new PetriNet{lib->lib->create()};
    } catch(std::exception &e) {
        std::cerr << e.what() << std::endl;
        return nullptr;
    }
}

PetriNet *PetriDynamicLib_createDebugPetriNet(PetriDynamicLib *lib) {
    try {
        return new PetriNet{lib->lib->createDebug()};
    } catch(std::exception &e) {
        std::cerr << e.what() << std::endl;
        return nullptr;
    }
}

char const *PetriDynamicLib_getHash(PetriDynamicLib *lib) {
    return lib->lib->hash().c_str();
}

char const *PetriDynamicLib_getName(PetriDynamicLib *lib) {
    return lib->lib->name().c_str();
}

uint16_t PetriDynamicLib_getPort(PetriDynamicLib *lib) {
    return lib->lib->port();
}

bool PetriDynamicLib_load(PetriDynamicLib *lib) {
    try {
        lib->lib->load();

        return true;
    } catch(std::exception &e) {
        std::cerr << e.what() << std::endl;
        return false;
    }
}

char const *PetriDynamicLib_getPath(PetriDynamicLib *lib) {
    return lib->lib->path().c_str();
}

char const *PetriDynamicLib_getPrefix(PetriDynamicLib *lib) {
    return lib->lib->prefix();
}
