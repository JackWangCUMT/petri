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
//  PetriDynamicLib.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"

#if !defined(PETRI_PREFIX) || !defined(PETRI_CLASS_NAME) || !defined(PETRI_PORT) || !defined(PETRI_ENUM)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class PETRI_CLASS_NAME : public Petri::PetriDynamicLibCommon {
public:
	/**
	 * Creates the dynamic library wrapper. You still need to call the load() method to access the wrapped functions.
	 */
	PETRI_CLASS_NAME() = default;

	PETRI_CLASS_NAME(PETRI_CLASS_NAME const &pn) = delete;
	PETRI_CLASS_NAME &operator=(PETRI_CLASS_NAME const &pn) = delete;

	PETRI_CLASS_NAME(PETRI_CLASS_NAME &&pn) = delete;
	PETRI_CLASS_NAME &operator=(PETRI_CLASS_NAME &&pn) = delete;

	virtual ~PETRI_CLASS_NAME() = default;

	/**
	 * Returns the name of the Petri net.
	 * @return The name of the Petri net
	 */
	virtual std::string name() const override {
		return PETRI_PREFIX;
	}

	/**
	 * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to debugger connection.
	 * @return The TCP port which will be used by DebugSession
	 */
	virtual uint16_t port() const override {
		return PETRI_PORT;
	}

	virtual char const *prefix() const override {
		return PETRI_PREFIX;
	}
};

