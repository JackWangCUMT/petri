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

#include "PetriDynamicLib.h"

namespace Petri {

    class MemberPetriDynamicLib : public Petri::PetriDynamicLib {
    public:
        MemberPetriDynamicLib(bool c_petriDynamicLib, std::string name, std::string prefix, uint16_t port)
                : PetriDynamicLib(c_petriDynamicLib)
                , _name(name)
                , _prefix(prefix)
                , _port(port) {}

        MemberPetriDynamicLib(MemberPetriDynamicLib const &pn) = delete;
        MemberPetriDynamicLib &operator=(MemberPetriDynamicLib const &pn) = delete;

        MemberPetriDynamicLib(MemberPetriDynamicLib &&pn) = delete;
        MemberPetriDynamicLib &operator=(MemberPetriDynamicLib &&pn) = delete;

        virtual ~MemberPetriDynamicLib() = default;

        virtual std::string name() const override {
            return _name;
        }

        virtual uint16_t port() const override {
            return _port;
        }

        virtual char const *prefix() const {
            return _prefix.c_str();
        }

    private:
        std::string _name, _prefix;
        uint16_t _port;
    };
}


#endif /* PetriDynamicLib_hpp */
