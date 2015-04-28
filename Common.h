//
//  Common.h
//  Pétri
//
//  Created by Rémi on 15/04/2015.
//

#ifndef Petri_Common_h
#define Petri_Common_h

#include <thread>
#include <string>
#include <chrono>

namespace Petri {
	
	namespace PetriCommon {
		inline void setThreadName(char const *name) {
#if __LINUX__
			pthread_setname_np(pthread_self(), name);
#elif __APPLE__
			pthread_setname_np(name);
#endif
		}

		inline void setThreadName(std::string const &name) {
			setThreadName(name.c_str());
		}
	}

}


#endif
