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

#include "../Cpp/PetriDynamicLib.h"
#include "Types.hpp"

namespace Petri {
    class PtrPetriDynamicLib : public PetriDynamicLib {
    public:
        PtrPetriDynamicLib(void *(*createPtr)(),
                           void *(*createDebugPtr)(),
                           char const *(*hashPtr)(),
                           char const *(*namePtr)(),
                           uint16_t (*portPtr)(),
                           char const *(*prefixPtr)())
                : _namePtr(namePtr)
                , _portPtr(portPtr)
                , _prefixPtr(prefixPtr) {
            _createPtr = createPtr;
            _createDebugPtr = createDebugPtr;
            _hashPtr = hashPtr;
        }
        PtrPetriDynamicLib(PtrPetriDynamicLib const &) = delete;
        PtrPetriDynamicLib &operator=(PtrPetriDynamicLib const &) = delete;

        PtrPetriDynamicLib(PtrPetriDynamicLib &&) = default;
        PtrPetriDynamicLib &operator=(PtrPetriDynamicLib &&) = default;
        virtual ~PtrPetriDynamicLib() = default;

        virtual std::unique_ptr<Petri::PetriNet> create() override {
            if(!this->loaded()) {
                throw std::runtime_error("Dynamic library not loaded!");
            }

            void *ptr = _createPtr();
            ::PetriNet *cPetriNet = static_cast<::PetriNet *>(ptr);
            Petri::PetriNet *petriNet = cPetriNet->petriNet.release();
            return std::unique_ptr<Petri::PetriNet>(petriNet);
        }

        virtual std::unique_ptr<Petri::PetriDebug> createDebug() override {
            if(!this->loaded()) {
                throw std::runtime_error("Dynamic library not loaded!");
            }

            void *ptr = _createDebugPtr();
            ::PetriNet *cPetriNet = static_cast<::PetriNet *>(ptr);
            Petri::PetriDebug *petriNet = static_cast<Petri::PetriDebug *>(cPetriNet->petriNet.release());
            return std::unique_ptr<Petri::PetriDebug>(petriNet);
        }

        /**
         * Returns the name of the Petri net.
         * @return The name of the Petri net
         */
        virtual std::string name() const override {
            return _namePtr();
        }

        /**
         * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to
         * debugger connection.
         * @return The TCP port which will be used by DebugSession
         */
        virtual uint16_t port() const override {
            return _portPtr();
        }

        virtual char const *prefix() const override {
            return _prefixPtr();
        }

        virtual void load() override {}

        virtual bool loaded() const override {
            return true;
        }

    private:
        char const *(*_namePtr)();
        uint16_t (*_portPtr)();
        char const *(*_prefixPtr)();
    };
}


#endif /* PtrDynamicLib_h */
