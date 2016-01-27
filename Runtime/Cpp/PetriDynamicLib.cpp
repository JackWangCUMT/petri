/*
 * Copyright (c) 2016 Rémi Saurel
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
//  PetriDynamicLib.cpp
//  TestPetri
//
//  Created by Rémi on 27/01/2016.
//

#include "../C/PetriNet.h"
#include "../C/Types.hpp"
#include "PetriDynamicLib.h"

namespace Petri {
    PetriDynamicLib::~PetriDynamicLib() = default;

    std::unique_ptr<PetriNet> PetriDynamicLib::create() {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createPtr();

        if(_c_dynamicLib) {
            ::PetriNet *cPetriNet = static_cast<::PetriNet *>(ptr);
            ptr = cPetriNet->petriNet.release();
        }

        return std::unique_ptr<PetriNet>(static_cast<PetriNet *>(ptr));
    }

    std::unique_ptr<PetriDebug> PetriDynamicLib::createDebug() {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createDebugPtr();

        if(_c_dynamicLib) {
            ::PetriNet *cPetriNet = static_cast<::PetriNet *>(ptr);
            ptr = cPetriNet->petriNet.release();
        }

        return std::unique_ptr<PetriDebug>(static_cast<PetriDebug *>(ptr));
    }
}
