//
//  KillableThread.h
//  IA Pétri
//
//  Created by Rémi on 06/07/2014.
//

#ifndef IA_Pe_tri_KillableThread_h
#define IA_Pe_tri_KillableThread_h

#include <thread>
#include <atomic>
#include <mutex>
#include <memory>
#include <condition_variable>
#include "ManagedMemoryHeap.h"
#include <csignal>
#include "setjmp.h"

#define stop_point KillableThread::stopIfRequested()

namespace Petri {

	class KillableThread : public std::thread {
	private:
		struct KillThread {};
		struct ThreadState {
			ThreadState() {}
			ManagedMemoryHeap _managedHeap;
			std::atomic_bool _alive = {false};
			KillableThread *_kt = nullptr;
			std::atomic_int _criticalSection = {0};
			std::atomic_bool _killPending = {false};

			jmp_buf _buf;
		};
	public:
		KillableThread() : _state(new ThreadState) {

		}

		KillableThread(KillableThread &&other) : _state(nullptr) {
			*this = std::move(other);
		}

		template<typename Function, typename... Args, typename = typename std::enable_if<!std::is_same<typename std::decay<Function>::type, KillableThread>::value>::type>
		explicit KillableThread(Function&& f, Args&&... args) : _state(new ThreadState) {
			this->enterCriticalSection();

			auto waitInit = std::make_shared<std::condition_variable>();
			this->std::thread::operator=(std::thread::thread(&KillableThread::launchThread<Function, Args...>, _state.load(), this, waitInit, std::move(f), std::move(args)...));

			std::mutex waitMutex;
			std::unique_lock<std::mutex> lk(waitMutex);
			waitInit->wait(lk, [this]() { return _state.load()->_alive.load(); });
		}

		~KillableThread() {
			delete _state.load();
		}

		static void signalHandler(int sig) {
			void *param = pthread_getspecific(_amIAlive._key);
			auto state = static_cast<ThreadState *>(param);
			longjmp(state->_buf, 1);
		}

		// WARNING:
		// Killing a thread invokes longmp. longjmp in a function where non-trivially typed (i.e. with custom destructor) automatic variables invokes undefined behaviour.
		//
		// From my tests, the actual behaviour is what one should expect:
		// killing a thread kills it without any cleanup of automatic variables, or lock release, or file close.
		//
		// The best and recommended way of achieving a good cleanup is allocating variables on an instance (automatically associated with the KillableThread on creation) of ManagedMemoryHeap,
		// which combined with RAII objects (std::lock_guard, std::unique_lock etc.) will safely release resources busy at the point of the thread brutal, heavy metal death.
		void kill() {
			if(this->alive()) {
				if(!this->criticalSection()) {
					_state.load()->_killPending = false;
					pthread_kill(this->native_handle(), SIGUSR1);
				}
				else {
					_state.load()->_killPending = true;
				}
			}
		}

		void stop() {
			_state.load()->_alive = false;
		}

		static KillableThread *current() {
			void *param = pthread_getspecific(_amIAlive._key);
			auto state = static_cast<ThreadState *>(param);
			if(state) {
				return state->_kt;
			}

			return nullptr;
		}

		bool criticalSection() const {
			return _state.load()->_criticalSection;
		}

		void enterCriticalSection() {
			++_state.load()->_criticalSection;
		}

		void exitCriticalSection() {
			auto &atom = _state.load()->_criticalSection;
			if(atom > 0)
				--atom;
			else {
				throw std::underflow_error("Too much exitCriticalSection called in a row!");
			}

			if(_state.load()->_killPending) {
				this->kill();
			}
		}

		KillableThread &operator=(KillableThread &&t) {
			delete _state.load();
			_state = t._state.load();
			t._state = nullptr;
			_state.load()->_kt = this;

			this->std::thread::operator=(std::move(t));

			return *this;
		}

		friend void swap(KillableThread &t1, KillableThread &t2) {
			using std::swap;
			ThreadState *s = t1._state;
			t1._state = t2._state.load();
			t2._state = s;
		}

		bool alive() {
			return _state.load()->_alive;
		}

		static void commitSuicide() {
			auto state = static_cast<ThreadState *>(pthread_getspecific(_amIAlive._key));
			if(state) {
				state->_alive = false;
				throw KillThread();
			}
			else
				std::terminate();
		}

		static void stopIfRequested() {
			auto state = static_cast<ThreadState *>(pthread_getspecific(_amIAlive._key));
			if(state && !state->_alive)
				throw KillThread();
		}

	private:
		template<typename Function, typename... Args>
		static void launchThread(ThreadState *state, KillableThread *thread, std::shared_ptr<std::condition_variable> waitInit, Function &&f, Args&&... args) {
			// When pthread_kill sends SIGUSR1 signal, signalHandler longjmp in the try block and throws an exception to gently but immediately kill the thread
			signal(SIGUSR1, &KillableThread::signalHandler);

			state->_managedHeap.makeDefaultForThread(std::this_thread::get_id());
			pthread_setspecific(_amIAlive._key, state);

			state->_kt = thread;
			state->_alive = true;

			waitInit->notify_all();

			try {
				if(!setjmp(state->_buf)) {
					// We can now be killed at will (provided the user didn't forbid that)
					thread->exitCriticalSection();

					f(std::forward<Args>(args)...);
				}
				else {
					funlockfile(stdout);
					funlockfile(stderr);
					std::cout << "jmp, killing" << std::endl;
					throw KillThread();
				}

			}
			catch(KillThread const &) {
				std::cout << "Ending thread" << std::endl;
			}

			state->_alive = false;

			state->_managedHeap.clear();

			pthread_setspecific(_amIAlive._key, nullptr);
		}

		std::atomic<ThreadState *> _state = {nullptr};
		
		struct AmIAlive {
			AmIAlive() {
				pthread_key_create(&_key, nullptr);
			}
			~AmIAlive() {
				pthread_key_delete(_key);
			}
			
			pthread_key_t _key;
		};
		
		static AmIAlive _amIAlive;
	};

}

#endif
