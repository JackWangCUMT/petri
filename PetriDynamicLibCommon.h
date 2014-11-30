//
//  PetriDynamicLibCommon.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_PetriDynamicLibCommon_h
#define IA_Pe_tri_PetriDynamicLibCommon_h

#include <memory>
#include "PetriUtils.h"

class PetriDynamicLibCommon {
public:
	/**
	 * Creates the dynamic library wrapper, and loads it, making possible to create the PetriNet objects.
	 */
	PetriDynamicLibCommon() = default;
	PetriDynamicLibCommon(PetriDynamicLibCommon const &pn) = delete;
	PetriDynamicLibCommon &operator=(PetriDynamicLibCommon const &pn) = delete;

	PetriDynamicLibCommon(PetriDynamicLibCommon &&pn) = default;
	PetriDynamicLibCommon &operator=(PetriDynamicLibCommon &&pn) = default;
	virtual ~PetriDynamicLibCommon() {
		this->unload();
	}

	/**
	 * Creates the PetriNet object according to the code contained in the dynamic library.
	 * @return The PetriNet object wrapped in a std::unique_ptr
	 */
	std::unique_ptr<PetriNet> create() {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}

		void *ptr = _createPtr();
		return std::unique_ptr<PetriNet>(static_cast<PetriNet *>(ptr));
	}

	/**
	 * Creates the PetriDebug object according to the code contained in the dynamic library.
	 * @return The PetriDebug object wrapped in a std::unique_ptr
	 */
	std::unique_ptr<PetriDebug> createDebug() {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}

		void *ptr = _createDebugPtr();
		return std::unique_ptr<PetriDebug>(static_cast<PetriDebug *>(ptr));
	}

	/**
	 * Returns the SHA1 hash of the dynamic library. It uniquely identifies the code of the PetriNet,
	 * so that a different or modified petri net has a different hash print
	 * @return The dynamic library hash
	 */
	std::string hash() const {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}
		return std::string(_hashPtr());
	}

	/**
	 * Returns the name of the Petri net.
	 * @return The name of the Petri net
	 */
	virtual std::string name() const = 0;

	/**
	 * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to debugger connection.
	 * @return The TCP port which will be used by DebugSession
	 */
	virtual std::uint16_t port() const = 0;

	/**
	 * Unloads the code of the dynamic library previously loaded, and loads the code contained in a possibly updated dylib.
	 */
	void reload() {
		this->unload();
		this->load();
	}

	/**
	 * Returns whether the dylib code resides in memory or not
	 * @return The loaded state of the dynamic library
	 */
	bool loaded() const {
		return _libHandle != nullptr;
	}

	/**
	 * Loads the dynamic library associated to this wrapper.
	 */
	virtual void load() = 0;

	/**
	 * Removes the dynamic library associated to this wrapper from memory.
	 */
	void unload() {
		if(this->loaded())
			dlclose(_libHandle);

		_libHandle = nullptr;
		_createPtr = nullptr;
		_createDebugPtr = nullptr;
		_hashPtr = nullptr;
	}

protected:
	void *_libHandle = nullptr;
	void *(*_createPtr)() = nullptr;
	void *(*_createDebugPtr)() = nullptr;
	char const *(*_hashPtr)() = nullptr;
};

#endif
