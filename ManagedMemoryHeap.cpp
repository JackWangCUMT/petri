//
//  ThreadMemoryPool.cpp
//  IA Pétri
//
//  Created by Rémi on 06/07/2014.
//

#include "ManagedMemoryHeap.h"

namespace Petri {

	ManagedMemoryHeap::ManagedHeaps ManagedMemoryHeap::_managedHeaps;

}

using namespace Petri;

void *operator new(size_t bytes) {
	void *p = nullptr;

	ManagedMemoryHeap *currentHeap = ManagedMemoryHeap::_managedHeaps.getHeap(std::this_thread::get_id());

	if(!currentHeap) {
		p = std::malloc(bytes);
	}
	else {
		p = std::malloc(bytes);
		currentHeap->_allocatedObjects.insert(ManagedMemoryHeap::SetPair(p, bytes));
	}

	return p;
}

void operator delete(void *p) noexcept {
	ManagedMemoryHeap *currentPool = ManagedMemoryHeap::_managedHeaps.getHeap(std::this_thread::get_id());

	auto classicDelete = [](void *p) {
		std::free(p);
	};

	if(currentPool) {
		auto it = currentPool->_allocatedObjects.find(ManagedMemoryHeap::SetPair(p, 0));
		if(it == currentPool->_allocatedObjects.end()) {
			classicDelete(p);
		}
		else {
			std::free(p);
			currentPool->_allocatedObjects.erase(it);
		}
	}
	else {
		classicDelete(p);
	}
}
