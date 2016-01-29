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
//  PtrDynamicLib.h
//  Petri
//
//  Created by Rémi on 15/01/2016.
//

#ifndef Petri_PtrPetriDynamicLib_h
#define Petri_PtrPetriDynamicLib_h

#include "../Cpp/MemberPetriDynamicLib.h"
#include "Types.hpp"

namespace Petri {

    class PtrPetriDynamicLib : public MemberPetriDynamicLib {
    public:
        PtrPetriDynamicLib(void *(*createPtr)(), void *(*createDebugPtr)(), char const *(*hashPtr)(), char const *name, char const *prefix, std::uint16_t port)
                : MemberPetriDynamicLib(true, name, prefix, port) {
            _createPtr = createPtr;
            _createDebugPtr = createDebugPtr;
            _hashPtr = hashPtr;
        }

        PtrPetriDynamicLib(PtrPetriDynamicLib const &) = delete;
        PtrPetriDynamicLib &operator=(PtrPetriDynamicLib const &) = delete;

        PtrPetriDynamicLib(PtrPetriDynamicLib &&) = default;
        PtrPetriDynamicLib &operator=(PtrPetriDynamicLib &&) = default;
        virtual ~PtrPetriDynamicLib() = default;

        virtual void load() override {}
        virtual void unload() override {}

        virtual bool loaded() const override {
            return true;
        }
    };
}

#endif /* PtrDynamicLib_h */
