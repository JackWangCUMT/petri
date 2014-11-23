//
//  DebugServer.h
//  IA Pétri
//
//  Created by Rémi on 23/11/2014.
//

#ifndef IA_Pe_tri_DebugServer_h
#define IA_Pe_tri_DebugServer_h

#include "PetriDynamicLibCommon.h"

namespace DebugServer {
	void registerPetriNet(std::string const &name, PetriDynamicLibCommon &petriNet);
	void unregisterPetriNet(std::string const &name);

	void init();
	void exit();

	void initSession(std::string const &name, std::string const &hostname, std::uint16_t port);
	void exitSession(std::string const &name);
}

#endif
