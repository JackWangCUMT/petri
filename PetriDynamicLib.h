//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"

#if !defined(PETRI_PREFIX) || !defined(PETRI_CLASS_NAME) || !defined(PETRI_PORT) || !defined(PETRI_ENUM)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class PETRI_CLASS_NAME : public PetriDynamicLibCommon<PETRI_ENUM> {
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

