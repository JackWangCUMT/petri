//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"
#include "DebugServer.h"
#include <chrono>
#include <ctime>
#include <iomanip>
#include <cstring>

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

	/**
	 * Loads the dynamic library associated to this wrapper.
	 * @throws std::runtime_error on two occasions: when the dylib could not be found (wrong path, missing file, wrong architecture or other error), or when the debug server's code has been changed (impliying the dylib has to be recompiled).
	 */
	virtual void load() override {
		if(_libHandle != nullptr) {
			return;
		}

		auto const path = "./" + this->name() + ".so";

		_libHandle = dlopen(path.c_str(), RTLD_NOW | RTLD_GLOBAL);

		if(_libHandle == nullptr) {
			logError("Unable to load the dynamic library at path \"", path, "\"!\n", "Reason: ", dlerror());

			throw std::runtime_error("Unable to load the dynamic library at path \"" + path + "\"!");
		}

		// Accesses the newly loaded symbols
		_createPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_create"));
		_createDebugPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_createDebug"));
		_hashPtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getHash"));

		// Checks that the dylib is more recent than the last change to the debug server
		auto APIDatePtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getAPIDate"));
		char const *format = "%b %d %Y %H:%M:%S";
		std::tm tm;
		std::memset(&tm, 0, sizeof(tm));
		
		strptime(APIDatePtr(), format, &tm);
		auto libDate = std::chrono::system_clock::from_time_t(std::mktime(&tm));
		auto serverDate = DebugServer::getAPIdate();

		if(serverDate > libDate) {
			this->unload();

			logError("The dynamic library  for Petri net ", PREFIX, " is out of date and must be recompiled!");
			throw std::runtime_error("The dynamic library is out of date and must be recompiled!");
		}
	}
};

