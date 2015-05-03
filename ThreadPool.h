//
//  ThreadPool.h
//  Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef Petri_ThreadPool_h
#define Petri_ThreadPool_h

#include <memory>
#include <thread>
#include <mutex>
#include <atomic>
#include <queue>
#include <vector>
#include <future>
#include "KillableThread.h"
#include "Callable.h"
#include "Common.h"

namespace Petri {

	template<typename _ReturnType>
	class ThreadPool {
		using ReturnType = _ReturnType;

		struct TaskManager {
			// We want a steady clock (no adjustments, only ticking forward in time), but it would be better if we got an high resolution clock.
			using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

			// Defined to char if ReturnType is void, so that we can nevertheless create a member variable of this type
			using VoidProofReturnType = typename std::conditional<std::is_same<ReturnType, void>::value, char, ReturnType>::type;

			TaskManager(std::shared_ptr<CallableBase<ReturnType>> &&task) : _task(std::move(task)) {}//, std::chrono::nanoseconds timeout, VoidProofReturnType returnWhenTimeout = VoidProofReturnType()) : _task(std::move(task)), _timeout(timeout), _res(returnWhenTimeout) {}

			ReturnType returnValue() {
				this->waitForCompletion();

				// No value if void
				return ReturnType(_res);
			}

			void waitForCompletion() {
				std::unique_lock<std::mutex> lk(_mut);
				_cv.wait(lk, [this]() { return _valOK == true; });
			}

			// Void version, simply exectutes the task
			template<typename _Helper = void>
			std::enable_if_t<(std::is_void<_Helper>::value, std::is_void<ReturnType>::value), void> execute() {
				_task->operator()();
				this->signalCompletion();
			}

			//Non-void version, executes the task and stores it in _res
			template<typename _Helper = void>
			std::enable_if_t<(std::is_void<_Helper>::value, !std::is_void<ReturnType>::value), void> execute() {
				_res = std::move(_task->operator()());
				this->signalCompletion();
			}

			// Signals the completion of the task to _cv, usually to the thread which called waitForCompletion()
			void signalCompletion() {
				_valOK = true;
				_cv.notify_all();
			}

			std::condition_variable _cv;
			std::mutex _mut;
			std::atomic_bool _valOK = {false};

			/*std::chrono::nanoseconds _timeout;
			 std::chrono::time_point<ClockType> _timeoutDate;*/

			VoidProofReturnType _res;
			std::shared_ptr<CallableBase<ReturnType>> _task;
		};
	public:
		class TaskResult {
			friend class ThreadPool;
		public:
			TaskResult() = default;

			/**
			 * Gets the return value of the task, blocks the calling thread until the result is made available.
			 * Not available for ResultType == void specialization
			 * @return The return value associated to the task and computed by the worker thread
			 */
			template<typename _Helper = void>
			std::enable_if_t<(std::is_void<_Helper>::value, !std::is_void<ReturnType>::value), ReturnType>
			returnValue() {
				return _proxy ? _proxy->returnValue() : throw std::runtime_error("Proxy not associated with a task!");
			}

			/**
			 * Blocks the calling thread until the task is complete and the result is available (no result for tasks returning void).
			 */
			void waitForCompletion() {
				_proxy ? _proxy->waitForCompletion() : throw std::runtime_error("Proxy not associated with a task!");
			}

			/**
			 * Checks whether the task result is available.
			 * @return Availability of the task result
			 */
			bool available() {
				return _proxy ? static_cast<bool>(_proxy->_valOK) : false;
			}

		private:
			std::shared_ptr<TaskManager> _proxy;
		};

	public:
		/**
		 * Creates the thread pool.
		 * @param capacity Max number of concurrent task at a given time
		 * @param name     This string is used for debug purposes: it gives a name to each worker threads,
		 *                 allowing for fast thread discimination when run through a debugger
		 */
		ThreadPool(std::size_t capacity, std::string const &name = "") : _workerThreads(capacity), _name(name) {
			int count = 0;
			for(auto &t : _workerThreads) {
				t = std::thread(&ThreadPool::work, this, _name + "_worker " + std::to_string(count++));
			}
		}

		~ThreadPool() {
			if(_pendingTasks > 0) {
				std::cerr << "Some tasks are still running!" << std::endl;
				throw std::runtime_error("The thread pool is being destroyed while some of its tasks are still pending!");
			}
			if(_alive) {
				std::cerr << "Thread pool is still alive!" << std::endl;
				throw std::runtime_error("The thread pool is strill alive!");
			}
		}

		/**
		 * Returns the worker threads count, i.e. the max number of concurrent tasks at a given time.
		 * @return The current worker threads count
		 */
		std::size_t threadCount() const {
			return _workerThreads.size();
		}

		/**
		 * Increments the worker threads count, i.e. allows one more concurrent task to run.
		 */
		void addThread() {
			if(!_alive)
				throw std::runtime_error("The thread pool is not alive anymore!");
			_workerThreads.emplace_back(&ThreadPool::work, this, _name + "_worker " + std::to_string(_workerThreads.size()));
		}

		/**
		 * Pauses the calling thread until there is no more pending tasks.
		 */
		void join() {
			if(_alive) {
				// Dirty hack…
				while(_pendingTasks > 0) {
					std::this_thread::sleep_for(std::chrono::milliseconds(10));
				}

				this->cancel();
			}
		}

		/**
		 * Stops the execution of the thread pool. Pauses the calling thread until each running task is completed.
		 * Does not run any of the currently pending taks, simply discards them.
		 */
		void cancel() {
			_alive = false;
			_taskAvailable.notify_all();

			for(auto &t : _workerThreads) {
				if(t.joinable() && t.get_id() != std::this_thread::get_id())
					t.join();
			}

			_pendingTasks = 0;
		}

		/**
		 * Pauses the execution of the thread pool. The tasks that were already running are still executed,
		 * but the current and future pending tasks will remain pending until the resume() method is called.
		 * If the thread pool is not alive, this is a no-op.
		 */
		void pause() {
			if(_alive) {
				_pause = true;
			}
		}

		/**
		 * Resumes the execution of the thread pool. If it wasn't alive and paused before, this is a no-op.
		 */
		void resume() {
			if(_alive) {
				bool d = true;
				if(_pause.compare_exchange_strong(d, false)) {
					_taskAvailable.notify_all();
				}
			}
		}

		/**
		 * Adds a task to the thread pool.
		 * @param task The task to be addes.
		 * @return A proxy object allowing the user to wait for the task completion, query the task completion status and get the task return value
		 */
		TaskResult addTask(std::shared_ptr<CallableBase<ReturnType>> task) {//, std::chrono::nanoseconds timeout) {
			TaskResult result;
			// task must be kept alive until execution finishes
			result._proxy = std::make_shared<TaskManager>(std::move(task));

			std::lock_guard<std::mutex> lk(_availabilityMutex);
			++_pendingTasks;
			_taskQueue.push(result._proxy);
			_taskAvailable.notify_one();

			return result;
		}

	private:
		void work(std::string const &name) {
			PetriCommon::setThreadName(name);

			while(_alive) {
				std::unique_lock<std::mutex> lk(_availabilityMutex);
				_taskAvailable.wait(lk, [this]() { return (!_taskQueue.empty() && !_pause) || !_alive; });

				if(!_alive)
					return;

				auto taskManager = std::move(_taskQueue.front());
				_taskQueue.pop();

				lk.unlock();

				taskManager->execute();

				--_pendingTasks;
			}
		}

		std::queue<std::shared_ptr<TaskManager>> _taskQueue;
		std::condition_variable _taskAvailable;
		std::mutex _availabilityMutex;
		
		std::atomic_bool _pause = {false};
		std::atomic_bool _alive = {true};
		std::atomic_uint _pendingTasks = {0};
		std::vector<std::thread> _workerThreads;
		std::string const _name;
	};

}

#endif
