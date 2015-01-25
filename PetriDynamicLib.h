//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"

#if !defined(PREFIX) || !defined(CLASS_NAME) || !defined(PORT)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class CLASS_NAME : public PetriDynamicLibCommon {
public:
	/**
	 * Creates the dynamic library wrapper. You still need to call the load() method to access the wrapped functions.
	 */
	CLASS_NAME() = default;

	CLASS_NAME(CLASS_NAME const &pn) = delete;
	CLASS_NAME &operator=(CLASS_NAME const &pn) = delete;

	CLASS_NAME(CLASS_NAME &&pn) = delete;
	CLASS_NAME &operator=(CLASS_NAME &&pn) = delete;

	virtual ~CLASS_NAME() = default;

	/**
	 * Returns the name of the Petri net.
	 * @return The name of the Petri net
	 */
	virtual std::string name() const override {
		return PREFIX;
	}

	/**
	 * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to debugger connection.
	 * @return The TCP port which will be used by DebugSession
	 */
	virtual uint16_t port() const override {
		return PORT;
	}

	virtual char const *prefix() const override {
		return PREFIX;
	}
};

