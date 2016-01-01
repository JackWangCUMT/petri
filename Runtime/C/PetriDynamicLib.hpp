//
//  PetriDynamicLib.hpp
//  Petri
//
//  Created by Rémi on 02/07/2015.
//  Copyright © 2015 Rémi. All rights reserved.
//

#ifndef PetriDynamicLib_hpp
#define PetriDynamicLib_hpp

#include "../PetriDynamicLib.h"
#include "Types.hpp"

class CPetriDynamicLib : public Petri::PetriDynamicLib {
public:
    CPetriDynamicLib(std::string name, std::string prefix, uint16_t port)
            : _name(name)
            , _prefix(prefix)
            , _port(port) {}

    CPetriDynamicLib(CPetriDynamicLib const &pn) = delete;
    CPetriDynamicLib &operator=(CPetriDynamicLib const &pn) = delete;

    CPetriDynamicLib(CPetriDynamicLib &&pn) = delete;
    CPetriDynamicLib &operator=(CPetriDynamicLib &&pn) = delete;

    virtual ~CPetriDynamicLib() = default;

    /**
     * Creates the PetriNet object according to the code contained in the dynamic library.
     * @return The PetriNet object wrapped in a std::unique_ptr
     */
    virtual std::unique_ptr<Petri::PetriNet> create() override {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createPtr();
        PetriNet *cPetriNet = static_cast<PetriNet *>(ptr);
        Petri::PetriNet *petriNet = cPetriNet->petriNet.release();
        return std::unique_ptr<Petri::PetriNet>(petriNet);
    }

    /**
     * Creates the PetriDebug object according to the code contained in the dynamic library.
     * @return The PetriDebug object wrapped in a std::unique_ptr
     */
    virtual std::unique_ptr<Petri::PetriDebug> createDebug() override {
        if(!this->loaded()) {
            throw std::runtime_error("Dynamic library not loaded!");
        }

        void *ptr = _createDebugPtr();
        PetriNet *cPetriNet = static_cast<PetriNet *>(ptr);
        Petri::PetriDebug *petriNet = static_cast<Petri::PetriDebug *>(cPetriNet->petriNet.release());
        return std::unique_ptr<Petri::PetriDebug>(petriNet);
    }

    virtual std::string name() const override {
        return _name;
    }

    virtual uint16_t port() const override {
        return _port;
    }

    virtual char const *prefix() const override {
        return _prefix.c_str();
    }

private:
    std::string _name, _prefix;
    uint16_t _port;
};


#endif /* PetriDynamicLib_hpp */
