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
//  PetriDynamicLib.hpp
//  Petri
//
//  Created by Rémi on 02/07/2015.
//

#ifndef PetriDynamicLib_hpp
#define PetriDynamicLib_hpp

#include "../Cpp/PetriDynamicLib.h"
#include "Types.hpp"

class CPetriDynamicLib : public Petri::PetriDynamicLib {
public:
    CPetriDynamicLib(std::string name, std::string prefix, uint16_t port)
            : _name(name)
            , _prefix(prefix)
            , _port(port) {}

    CPetriDynamicLib(CPetriDynamicLib const &pn) = delete;
    CPetriDynamicLib &operator=(CPetriDynamicLib const &pn) = delete;

    CPetriDynamicLib(CPetriDynamicLib &&pn) = delete;
    CPetriDynamicLib &operator=(CPetriDynamicLib &&pn) = delete;

    virtual ~CPetriDynamicLib() = default;

    /**
     * Creates the PetriNet object according to the code contained in the dynamic library.
     * @return The PetriNet object wrapped in a std::unique_ptr
     */
    virtual std::unique_ptr<Petri::PetriNet> create() override {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createPtr();
        PetriNet *cPetriNet = static_cast<PetriNet *>(ptr);
        Petri::PetriNet *petriNet = cPetriNet->petriNet.release();
        return std::unique_ptr<Petri::PetriNet>(petriNet);
    }

    /**
     * Creates the PetriDebug object according to the code contained in the dynamic library.
     * @return The PetriDebug object wrapped in a std::unique_ptr
     */
    virtual std::unique_ptr<Petri::PetriDebug> createDebug() override {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createDebugPtr();
        PetriNet *cPetriNet = static_cast<PetriNet *>(ptr);
        Petri::PetriDebug *petriNet = static_cast<Petri::PetriDebug *>(cPetriNet->petriNet.release());
        return std::unique_ptr<Petri::PetriDebug>(petriNet);
    }

    virtual std::string name() const override {
        return _name;
    }

    virtual uint16_t port() const override {
        return _port;
    }

    virtual char const *prefix() const override {
        return _prefix.c_str();
    }

private:
    std::string _name, _prefix;
    uint16_t _port;
};


#endif /* PetriDynamicLib_hpp */
