//
//  PetriDynamicLibCommon.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_PetriDynamicLibCommon_h
#define Petri_PetriDynamicLibCommon_h

#include "DynamicLib.h"
#include <memory>
#include "PetriUtils.h"
#include "DebugServer.h"
#include "PetriDebug.h"

namespace Petri {

	template<typename _ActionResult>
	class PetriDynamicLibCommon : public DynamicLib {
	public:
		/**
		 * Creates the dynamic library wrapper. It still needs to be loaded to make it possible to create the PetriNet objects.
		 */
		PetriDynamicLibCommon() = default;
		PetriDynamicLibCommon(PetriDynamicLibCommon const &pn) = delete;
		PetriDynamicLibCommon &operator=(PetriDynamicLibCommon const &pn) = delete;

		PetriDynamicLibCommon(PetriDynamicLibCommon &&pn) = default;
		PetriDynamicLibCommon &operator=(PetriDynamicLibCommon &&pn) = default;
		virtual ~PetriDynamicLibCommon() = default;

		/**
		 * Creates the PetriNet object according to the code contained in the dynamic library.
		 * @return The PetriNet object wrapped in a std::unique_ptr
		 */
		std::unique_ptr<PetriNet<_ActionResult>> create() {
			if(!this->loaded()) {
				throw std::runtime_error("Dynamic library not loaded!");
			}

			void *ptr = _createPtr();
			return std::unique_ptr<PetriNet<_ActionResult>>(static_cast<PetriNet<_ActionResult> *>(ptr));
		}

		/**
		 * Creates the PetriDebug object according to the code contained in the dynamic library.
		 * @return The PetriDebug object wrapped in a std::unique_ptr
		 */
		std::unique_ptr<PetriDebug<_ActionResult>> createDebug() {
			if(!this->loaded()) {
				throw std::runtime_error("Dynamic library not loaded!");
			}

			void *ptr = _createDebugPtr();
			return std::unique_ptr<PetriDebug<_ActionResult>>(static_cast<PetriDebug<_ActionResult> *>(ptr));
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
		virtual uint16_t port() const = 0;

		/**
		 * Loads the dynamic library associated to this wrapper.
		 * @throws std::runtime_error on two occasions: when the dylib could not be found (wrong path, missing file, wrong architecture or other error), or when the debug server's code has been changed (impliying the dylib has to be recompiled).
		 */
		virtual void load() override {
			if(this->loaded()) {
				return;
			}

			this->DynamicLib::load();

			std::string const prefix = this->prefix();

			// Accesses the newly loaded symbols
			_createPtr = this->loadSymbol<void *()>((prefix + "_create").c_str());
			_createDebugPtr = this->loadSymbol<void *()>((prefix + "_createDebug").c_str());
			_hashPtr = this->loadSymbol<char const *()>((prefix + "_getHash").c_str());

			// Checks that the dylib is more recent than the last change to the debug server
			/*auto APIDatePtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, (prefix + "_getAPIDate").c_str()));
			 auto libDate = DebugServer::getDateFromTimestamp(APIDatePtr());
			 auto serverDate = DebugServer::getAPIdate();

			 if(serverDate > libDate) {
			 this->unload();

			 std::cerr << "The dynamic library for Petri net " << prefix << " is out of date and must be recompiled!" << std::endl;
			 throw std::runtime_error("The dynamic library is out of date and must be recompiled!");
			 }*/
		}

		/**
		 * Gives access to the path of the dynamic library archive, relative to the executable path.
		 * @return The relative path of the dylib
		 */
		virtual std::string path() const override {
			return "./" + this->name() + ".so";
		}

		virtual char const *prefix() const = 0;
		
	protected:
		void *(*_createPtr)() = nullptr;
		void *(*_createDebugPtr)() = nullptr;
		char const *(*_hashPtr)() = nullptr;
	};

}

#endif
