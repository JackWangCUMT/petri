//
//  PetriDynamicLib.h
//  Club Robot
//
//  Created by Rémi on 22/11/2014.
//

#if !defined(PREFIX) || !defined(CLASS_NAME) || !defined(LIB_PATH)
#error "Do not include this file manually, let the C++ code generator use it for you!"
#endif

class CLASS_NAME {
public:
	CLASS_NAME() = default;
	CLASS_NAME(CLASS_NAME const &pn) = delete;
	CLASS_NAME &operator=(CLASS_NAME const &pn) = delete;

	CLASS_NAME(CLASS_NAME &&pn) : _libHandle(pn._libHandle), _createPtr(pn._createPtr), _hashPtr(pn._hashPtr) {
		pn._libHandle = nullptr;
		pn._createPtr = nullptr;
		pn._hashPtr = nullptr;
	}
	CLASS_NAME &operator=(CLASS_NAME &&pn) {
		_libHandle = nullptr;
		_createPtr = nullptr;
		_hashPtr = nullptr;

		std::swap(_libHandle, pn._libHandle);
		std::swap(_createPtr, pn._createPtr);
		std::swap(_hashPtr, pn._hashPtr);

		return *this;
	}

	~CLASS_NAME() {
		if(this->loaded())
			dlclose(_libHandle);
	}

	std::unique_ptr<PetriNet> create() {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}

		void *ptr = _createPtr();
		return std::unique_ptr<PetriNet>(static_cast<PetriNet *>(ptr));
	}

	std::string getHash() {
		if(!this->loaded()) {
			throw std::runtime_error("Dynamic library not loaded!");
		}
		return std::string(_hashPtr());
	}

	void load() {
		if(_libHandle != nullptr) {
			return;
		}
		char const *lib_name = LIB_PATH;
		_libHandle = dlopen(lib_name, RTLD_NOW);
		if(_libHandle == nullptr) {
			throw std::runtime_error("Unable to load the dynamic library!");
		}
		_createPtr = reinterpret_cast<void *(*)()>(dlsym(_libHandle, PREFIX "_create"));
		_hashPtr = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, PREFIX "_getHash"));
	}

	void reload() {
		if(this->loaded())
			dlclose(_libHandle);
		_libHandle = nullptr;
		_createPtr = nullptr;
		_hashPtr = nullptr;
		this->load();
	}

	bool loaded() const {
		return _libHandle != nullptr;
	}

private:
	void *_libHandle = nullptr;
	void *(*_createPtr)() = nullptr;
	char const *(*_hashPtr)() = nullptr;
};
