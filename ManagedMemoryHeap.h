//
//  ThreadMemoryPool.h
//  IA Pétri
//
//  Created by Rémi on 06/07/2014.
//

#ifndef IA_Pe_tri_ManagedMemoryHeap_h
#define IA_Pe_tri_ManagedMemoryHeap_h

#include <thread>
#include <iostream>
#include <set>
#include <cstdlib>
#include <unordered_map>
#include <functional>
#include <atomic>
#include <mutex>

#define safe_alloc ManagedMemoryHeap::make_unique

namespace Petri {

	class ManagedMemoryHeap {
		friend void *::operator new(size_t);
		friend void ::operator delete(void *) noexcept;
		struct SetPair {
			using first_type = void *;
			using second_type = std::size_t;

			SetPair(void *p, std::size_t s) : first(p), second(s) {};
			void *first;
			std::size_t second;
		};

		struct HolderBase {
			HolderBase(ManagedMemoryHeap *heap) : _heap(heap) {
				SetPair p(this, std::numeric_limits<SetPair::second_type>::max());
				_heap->_allocatedObjects.insert(std::move(p));
			}

			virtual ~HolderBase() {
				auto it = _heap->_allocatedObjects.find(SetPair(this, 0));
				if(it != _heap->_allocatedObjects.end()) {
					_heap->_allocatedObjects.erase(it);
				}
			}

			ManagedMemoryHeap *_heap;
		};

		template<typename Type>
		struct Holder : public Type, public HolderBase {
			template<typename... Args>
			Holder(ManagedMemoryHeap *heap, Args &&...args) : Type(args...), HolderBase(heap) {}
		};

	public:
		template<typename T>
		static void dtorAndFree(T *ptr) {
			auto heap = ManagedMemoryHeap::_managedHeaps.getHeap(std::this_thread::get_id());
			if(heap) {
				auto it = heap->_allocatedObjects.find(SetPair(ptr, 0));
				if(it != heap->_allocatedObjects.end() && it->second == std::numeric_limits<decltype(it->second)>::max()) {
					auto base = reinterpret_cast<HolderBase *>(ptr);
					base->~HolderBase();
					std::free(ptr);
				}
			}
		}

		ManagedMemoryHeap() = default;
		ManagedMemoryHeap(ManagedMemoryHeap const &) = delete;
		~ManagedMemoryHeap() {
			this->clear();
			this->removeDefaultForThread();
		}

		ManagedMemoryHeap &operator=(ManagedMemoryHeap const &) = delete;
		ManagedMemoryHeap &operator=(ManagedMemoryHeap &&other) {
			if(other._registeredThread != std::thread::id()) {
				ManagedMemoryHeap::_managedHeaps.makeDefaultForThread(this, other._registeredThread);
			}

			_registeredThread = other._registeredThread;
			other._registeredThread = std::thread::id();
			_allocatedObjects = std::move(other._allocatedObjects);

			return *this;
		}

		template<typename T, typename... Args>
		static std::unique_ptr<T, decltype(&ManagedMemoryHeap::dtorAndFree<T>)> make_unique(Args &&...args) {
			ManagedMemoryHeap *heap = ManagedMemoryHeap::_managedHeaps.getHeap(std::this_thread::get_id());
			T *returnVal = nullptr;

			if(heap) {
				if(heap->_enterCritical)
					heap->_enterCritical();
				returnVal = static_cast<T *>(std::malloc(sizeof(Holder<T>)));
				new(returnVal) Holder<T>(heap, args...);
				if(heap->_exitCritical)
					heap->_exitCritical();
			}
			else {
				returnVal = static_cast<T *>(std::malloc(sizeof(T)));
				new(returnVal) T(args...);
			}

			return std::unique_ptr<T, decltype(&ManagedMemoryHeap::dtorAndFree<T>)>(returnVal, &ManagedMemoryHeap::dtorAndFree<T>);
		}

		void clear() {
			while(!_allocatedObjects.empty()) {
				auto &pair = *_allocatedObjects.begin();
				// Found an instance of our custom object holder
				if(pair.second == std::numeric_limits<decltype(pair.second)>::max()) {
					auto ptr = static_cast<HolderBase *>(pair.first);
					ptr->~HolderBase();
					std::free(ptr);
				}
				else {
					std::cerr << "Leaked object: " << pair.first << std::endl;
					_allocatedObjects.erase(pair);
				}
			}
		}

		void makeDefaultForThread(std::thread::id id) {
			static std::thread::id nobody;

			if(_registeredThread != nobody && _registeredThread != id) {
				throw std::logic_error("A ManagedMemoryHeap cannot currently serve more than 1 thread");
			}

			_registeredThread = id;
			ManagedMemoryHeap::_managedHeaps.makeDefaultForThread(this, id);
		}

		void removeDefaultForThread() {
			if(_registeredThread != std::thread::id()) {
				ManagedMemoryHeap::_managedHeaps.makeDefaultForThread(nullptr, _registeredThread);
				_registeredThread = std::thread::id();
			}
		}

		// Recommended to provide those, as  a memory allocation could otherwise be interrupted, leading the heap in an inconsistent state.
		// The handlers have to behaves like a recursive lock in order to not interfere with the user's code.
		// Set to nullptr to reset a handler.
		void setCriticalHandlers(std::function<void()> &&enterCritical, std::function<void()> &&exitrCritical) {
			_enterCritical = std::move(enterCritical);
			_exitCritical = std::move(enterCritical);
		}

		template <typename T>
		class MallocAllocator: public std::allocator<T> {
		public:
			typedef T value_type;
			typedef size_t size_type;
			typedef T* pointer;
			typedef const T* const_pointer;

			MallocAllocator() noexcept: std::allocator<T>() {}
			MallocAllocator(const MallocAllocator &a) noexcept : std::allocator<T>(a) {}
			template <class U>
			MallocAllocator(const MallocAllocator<U> &a) noexcept : std::allocator<T>(a) {}
			~MallocAllocator() noexcept { }

			template<typename _Tp1>
			struct rebind {
				typedef MallocAllocator<_Tp1> other;
			};

			pointer allocate(size_type n, void const *hint = nullptr) {
				return reinterpret_cast<pointer>(std::malloc(sizeof(value_type) * n));
			}

			void deallocate(pointer p, size_type n) {
				std::free(p);
			}
		};

	private:
		std::thread::id _registeredThread;
		struct PairComparator {
			bool operator()(SetPair const &p1, SetPair const &p2) {
				return p1.first < p2.first;
			}
		};

		std::set<SetPair, PairComparator, MallocAllocator<void *>> _allocatedObjects;
		std::function<void()> _enterCritical, _exitCritical;

		struct ManagedHeaps {
			using MapContent = std::pair<std::thread::id const, ManagedMemoryHeap *>;
			using UsingMap = std::unordered_map<MapContent::first_type, MapContent::second_type, std::hash<std::thread::id>, std::equal_to<MapContent::first_type>, ManagedMemoryHeap::MallocAllocator<MapContent>>;

			ManagedHeaps() {
				_memoryHeaps = new UsingMap;
				_memoryHeaps.load()->operator[](std::thread::id()) = nullptr;
			}

			~ManagedHeaps() {
				UsingMap *p = _memoryHeaps;
				_memoryHeaps = nullptr;
				delete p;
			}

			void makeDefaultForThread(ManagedMemoryHeap *heap, std::thread::id threadId) {
				auto map = _memoryHeaps.load();
				if(!map) {
					throw std::runtime_error("ManagedMemoryHeap not initialized!");
				}

				_heapsMutex.lock();
				map->operator[](threadId) = heap;
				_heapsMutex.unlock();
			}

			ManagedMemoryHeap *getHeap(std::thread::id id) {
				if(_memoryHeaps) {
					auto map = const_cast<UsingMap const *>(_memoryHeaps.load());
					auto it = map->find(id);
					if(it != map->end())
						return it->second;
				}
				return nullptr;
			}
			
		private:
			std::atomic<UsingMap *>_memoryHeaps = {nullptr};
			std::mutex _heapsMutex;
			
		};
		static ManagedHeaps _managedHeaps;
	};

}

#endif
