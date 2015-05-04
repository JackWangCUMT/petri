//
//  PetriUtils.cpp
//  IA Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "PetriUtils.h"
#include <iostream>
#include <thread>
#include "Common.h"

namespace Petri {
	void setThreadName(char const *name) {
#if __LINUX__
		pthread_setname_np(pthread_self(), name);
#elif __APPLE__
		pthread_setname_np(name);
#endif
	}

	void setThreadName(std::string const &name) {
		setThreadName(name.c_str());
	}
	
	namespace PetriUtils {
		actionResult_t pause(std::chrono::nanoseconds const &delay) {
			std::this_thread::sleep_for(delay);
			return {};
		}

		actionResult_t printAction(std::string const &name, std::uint64_t id) {
			std::cout << "Action " << name << ", ID " << id << " completed." << std::endl;
			return {};
		}

		actionResult_t doNothing() {
			return {};
		}
	}
}
