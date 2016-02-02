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
//  DebugServer.c
//  Petri
//
//  Created by Rémi on 04/07/2015.
//

#include "../../Cpp/DebugServer.h"
#include "../DebugServer.h"
#include "Types.hpp"

char const *PetriDebugServer_getVersion() {
    return Petri::DebugServer::getVersion().c_str();
}

PetriDebugServer *PetriDebugServer_create(PetriDynamicLib *petri) {
    return new PetriDebugServer{std::make_unique<Petri::DebugServer>(*petri->lib)};
}

void PetriDebugServer_destroy(PetriDebugServer *server) {
    delete server;
}

void PetriDebugServer_start(PetriDebugServer *server) {
    server->server->start();
}

void PetriDebugServer_stop(PetriDebugServer *server) {
    server->server->stop();
}

bool PetriDebugServer_isRunning(PetriDebugServer *server) {
    return server->server->running();
}
