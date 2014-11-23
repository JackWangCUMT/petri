//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#include "PetriDynamicLibCommon.h"
#include "DebugServer.h"

#if !defined(PREFIX) || !defined(CLASS_NAME) || !defined(LIB_PATH)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class CLASS_NAME : public PetriDynamicLibCommon {
public:
	CLASS_NAME() {
		DebugServer::registerPetriNet(this->name(), *this);
	}

	CLASS_NAME(CLASS_NAME const &pn) = delete;
	CLASS_NAME &operator=(CLASS_NAME const &pn) = delete;

	CLASS_NAME(CLASS_NAME &&pn) : _libHandle(pn._libHandle), _createPtr(pn._createPtr), _createDebugPtr(pn._createDebugPtr), _hashPtr(pn._hashPtr) {
		pn._libHandle = nullptr;
		pn._createPtr = nullptr;
		pn._createDebugPtr = nullptr;
		pn._hashPtr = nullptr;
	}
	CLASS_NAME &operator=(CLASS_NAME &&pn) {
		_libHandle = nullptr;
		_createPtr = nullptr;
		_createDebugPtr = nullptr;
		_hashPtr = nullptr;

		std::swap(_libHandle, pn._libHandle);
		std::swap(_createPtr, pn._createPtr);
		std::swap(_createDebugPtr, pn._createDebugPtr);
		std::swap(_hashPtr, pn._hashPtr);

		return *this;
	}

	virtual ~CLASS_NAME() {
		DebugServer::unregisterPetriNet(this->name());
		
		if(this->loaded())
			dlclose(_libHandle);
	}

	virtual std::unique_ptr<PetriNet> create() override {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}

		void *ptr = _createPtr();
		return std::unique_ptr<PetriNet>(static_cast<PetriNet *>(ptr));
	}

	virtual std::unique_ptr<PetriNet> createDebug() override {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}

		void *ptr = _createDebugPtr();
		return std::unique_ptr<PetriNet>(static_cast<PetriNet *>(ptr));
	}

	virtual std::string hash() const override {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}
		return std::string(_hashPtr());
	}

	virtual std::string name() const override {
		return PREFIX;
	}

	virtual void load() override {
		if(_libHandle != nullptr) {
			return;
		}
		char const *lib_name = LIB_PATH;
		_libHandle = dlopen(lib_name, RTLD_NOW);
		if(_libHandle == nullptr) {
			throw std::runtime_error("Unable to load the dynamic library!");
		}
		_createPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_create"));
		_createDebugPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_createDebug"));
		_hashPtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getHash"));
	}

	virtual void reload() override {
		if(this->loaded())
			dlclose(_libHandle);
		_libHandle = nullptr;
		_createPtr = nullptr;
		_createDebugPtr = nullptr;
		_hashPtr = nullptr;
		this->load();
	}

	virtual bool loaded() const override {
		return _libHandle != nullptr;
	}

private:
	void *_libHandle = nullptr;
	void *(*_createPtr)() = nullptr;
	void *(*_createDebugPtr)() = nullptr;
	char const *(*_hashPtr)() = nullptr;
};

