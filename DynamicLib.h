//
//  DynamicLib.h
//  Pétri
//
//  Created by Rémi on 25/01/2015.
//

#ifndef Petri_DynamicLib_h
#define Petri_DynamicLib_h

#include <string>
#include <stdexcept>
#include <dlfcn.h>

namespace Petri {

	class DynamicLib {
	public:
		/**
		 * Creates the dynamic library wrapper. It still needs to be loaded to access the dylib symbols.
		 */
		DynamicLib(std::string const &path = "") : _path(path) {}
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
		virtual std::string path() const {
			return _path;
		}

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

		/**
		 * Loads the specified function symbol and returns it as a function pointer.
		 */
		template<typename FuncType>
		FuncType *loadSymbol(std::string const &name) {
			if(!this->loaded()) {
				throw std::runtime_error("Dynamic library not loaded!");
			}

			void *sym = dlsym(_libHandle, name.c_str());

			if(sym == nullptr) {
				throw std::runtime_error("Could not find symbol " + name + " in the library!");
			}

			return reinterpret_cast<FuncType *>(sym);
		}
		
	protected:
		void *_libHandle = nullptr;
		std::string const _path;
	};

}

#endif
