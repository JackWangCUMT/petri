//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"
#include "DebugServer.h"

#if !defined(PREFIX) || !defined(CLASS_NAME) || !defined(LIB_PATH) || !defined(PORT)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class CLASS_NAME : public PetriDynamicLibCommon {
public:
	/**
	 * Creates the dynamic library wrapper, and loads it, making possible to create the PetriNet objects.
	 */
	CLASS_NAME() {
		this->load();
	}

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
	virtual std::uint16_t port() const override {
		return PORT;
	}

private:
	virtual void load() override {
		if(_libHandle != nullptr) {
			return;
		}

		_libHandle = dlopen(LIB_PATH, RTLD_NOW);
		if(_libHandle == nullptr) {
			throw std::runtime_error("Unable to load the dynamic library!");
		}
		_createPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_create"));
		_createDebugPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_createDebug"));
		_hashPtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getHash"));
	}
};

