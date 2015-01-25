//
//  SymbolEvaluator.h
//  Club Robot
//
//  Created by Rémi on 25/01/2015.
//

#ifndef Club_Robot_SymbolEvaluator_h
#define Club_Robot_SymbolEvaluator_h

#include "DynamicLib.h"

class SymbolEvaluator : public DynamicLib {
public:
	/**
	 * Creates the symbol evaluator.
	 */
	SymbolEvaluator(std::string prefix) : DynamicLib(), _prefix(prefix) {};
	SymbolEvaluator(SymbolEvaluator const &pn) = delete;
	SymbolEvaluator &operator=(SymbolEvaluator const &pn) = delete;

	SymbolEvaluator(SymbolEvaluator &&pn) = default;
	SymbolEvaluator &operator=(SymbolEvaluator &&pn) = default;
	virtual ~SymbolEvaluator() = default;

	/**
	 * Returns a string representation of the evaluated symbol
	 * @return a string representation of the evaluated symbol
	 * @throws std::runtime_error when the symbol could not be loaded.
	 */
	std::string evaluate() const {
		if(!this->loaded()) {
			throw std::runtime_error("");
		}

		return std::string(_evaluate());
	}

	/**
	 * Loads the dynamic library associated to this wrapper.
	 * @throws nothing
	 */
	virtual void load() override {
		if(this->loaded()) {
			return;
		}

		try {
			this->DynamicLib::load();

			// Accesses the newly loaded symbols
			_evaluate = reinterpret_cast<char const *(*)()>(dlsym(_libHandle, (_prefix + "_evaluate").c_str()));
			if(_evaluate == nullptr) {
				this->unload();
			}
		}
		catch(std::exception &e) {}
	}

	/**
	 * Gives access to the path of the dynamic library archive, relative to the executable path.
	 * @return The relative path of the dylib
	 */
	virtual std::string path() const override {
		return "./" + _prefix + "_evaluator" + ".so";
	}

protected:
	char const *(*_evaluate)() = nullptr;
	std::string const _prefix;
};

#endif
