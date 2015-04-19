//
//  DynamicLib.h
//
//  Created by Rémi on 25/01/2015.
//

#ifndef Club_Robot_DynamicLib_h
#define Club_Robot_DynamicLib_h

#include <string>
#include <stdexcept>
#include <dlfcn.h>

class DynamicLib {
public:
	/**
	 * Creates the dynamic library wrapper. It still needs to be loaded to access the dylib symbols.
	 */
	DynamicLib() = default;
	DynamicLib(DynamicLib const &pn) = delete;
	DynamicLib &operator=(DynamicLib const &pn) = delete;

	DynamicLib(DynamicLib &&pn) = default;
	DynamicLib &operator=(DynamicLib &&pn) = default;
	virtual ~DynamicLib() {
		this->unload();
	}

	/**
	 * Returns whether the dylib code resides in memory or not
	 * @return The loaded state of the dynamic library
	 */
	bool loaded() const {
		return _libHandle != nullptr;
	}

	/**
	 * Gives access to the path of the dynamic library archive, relative to the executable path.
	 * @return The relative path of the dylib
	 */
	virtual std::string path() const = 0;

	/**
	 * Loads the dynamic library associated to this wrapper.
	 * @throws std::runtime_error when an error occurred (see subclasses doc for the possible errors).
	 */
	virtual void load() {
		if(this->loaded()) {
			return;
		}

		std::string path = this->path();

		_libHandle = dlopen(path.c_str(), RTLD_NOW | RTLD_LOCAL);

		if(_libHandle == nullptr) {
			std::cerr <<"Unable to load the dynamic library at path \"" << path << "\"!\n" << "Reason: " << dlerror() << std::endl;

			throw std::runtime_error("Unable to load the dynamic library at path \"" + path + "\"!");
		}
	}

	/**
	 * Removes the dynamic library associated to this wrapper from memory.
	 */
	void unload() {
		if(this->loaded()) {
			dlclose(_libHandle);
		}

		_libHandle = nullptr;
	}

	/**
	 * Unloads the code of the dynamic library previously loaded, and loads the code contained in a possibly updated dylib.
	 */
	void reload() {
		this->unload();
		this->load();
	}

protected:
	void *_libHandle = nullptr;
};

#endif
