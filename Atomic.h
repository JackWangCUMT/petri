//
//  Atomic.h
//  Pétri
//
//  Created by Rémi on 28/04/2015.
//

#ifndef Petri_Atomic_h
#define Petri_Atomic_h

#include <mutex>
#include "PetriUtils.h"

namespace Petri {

	class Atomic {
	public:
		Atomic() : _value(0), _lock(_mutex, std::defer_lock) {

		}

		auto &value() {
			return _value;
		}

		auto getLock() {
			return std::unique_lock<std::mutex>{_mutex, std::defer_lock};
		}

	private:
		std::int64_t _value;
		std::unique_lock<std::mutex> _lock;
		std::mutex _mutex;
	};
	
}


#endif
