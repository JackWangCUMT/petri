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
	PetriDynamicLibCommon() = default;
	PetriDynamicLibCommon(PetriDynamicLibCommon const &pn) = delete;
	PetriDynamicLibCommon &operator=(PetriDynamicLibCommon const &pn) = delete;

	PetriDynamicLibCommon(PetriDynamicLibCommon &&pn) = default;
	PetriDynamicLibCommon &operator=(PetriDynamicLibCommon &&pn) = default;
	virtual ~PetriDynamicLibCommon() = default;

	virtual std::unique_ptr<PetriNet> create() = 0;
	virtual std::unique_ptr<PetriNet> createDebug() = 0;

	virtual std::string hash() const = 0;

	virtual std::string name() const = 0;

	virtual void load() = 0;
	virtual void reload() = 0;
	virtual bool loaded() const = 0;
};

#endif
