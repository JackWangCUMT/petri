//
//  StateChartUtils.h
//  Pétri
//
//  Created by Rémi on 12/11/2014.
//

#ifndef Petri_PetriUtils_h
#define Petri_PetriUtils_h

#include "Common.h"
#include <chrono>

namespace Petri {

	using namespace std::chrono_literals;

	enum class ActionResult {
		OK,
		NOK
	};

	namespace PetriUtils {
		actionResult_t pause(std::chrono::nanoseconds const &delay);
		actionResult_t printAction(std::string const &name, std::uint64_t id);
		actionResult_t doNothing();
	}

}

#endif
