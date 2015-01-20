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

	virtual void load() override {
		if(_libHandle != nullptr) {
			return;
		}

		auto serverDate = DebugServer::getAPIdate();
		auto const path = this->name() + ".so";

		_libHandle = dlopen(path.c_str(), RTLD_LAZY);

		if(_libHandle == nullptr) {
			logError("Unable to load the dynamic library at path \"", path, "\"!\n", "Reason: ", dlerror());

			throw std::runtime_error("Unable to load the dynamic library at path \"" + path + "\"!");
		}
		_createPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_create"));
		_createDebugPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_createDebug"));
		_hashPtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getHash"));

		auto APIDatePtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getAPIDate"));
		char const *format = "%b %d %Y %H:%M:%S";
		std::tm tm;
		strptime(APIDatePtr(), format, &tm);
		auto libDate = std::chrono::system_clock::from_time_t(std::mktime(&tm));

		if(serverDate > libDate) {
			this->unload();

			throw std::runtime_error("The dynamic library is out of date and must be recompiled!");
		}
	}
};

