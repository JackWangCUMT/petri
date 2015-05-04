//
//  DynamicLib.cpp
//  IA Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "DynamicLib.h"
#include <dlfcn.h>
#include <iostream>

namespace Petri {

	void DynamicLib::load() {
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
	void DynamicLib::unload() {
		if(this->loaded()) {
			if(dlclose(_libHandle) != 0) {
				std::cerr <<"Unable to unload the dynamic library!\n" << "Reason: " << dlerror() << std::endl;
			}
		}

		_libHandle = nullptr;
	}

	void *DynamicLib::_loadSymbol(const std::string &name) {
		return dlsym(_libHandle, name.c_str());
	}
	
}