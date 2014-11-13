//
//  ThreadPool.h
//  IA Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef IA_Pe_tri_ThreadPool_h
#define IA_Pe_tri_ThreadPool_h

#include <memory>
#include <thread>
#include <mutex>
#include <atomic>
#include <queue>
#include <vector>
#include <future>
#include "KillableThread.h"

template<typename _ReturnType>
class ThreadPool {
	using ReturnType = _ReturnType;

	struct TaskManager {
		// We want a steady clock (no adjustments, only ticking forward in time), but it would be better if we got an high resolution clock.
		using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;
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

		// Void version
		template<typename _Helper = void>
		typename std::enable_if<(std::is_void<_Helper>::value, std::is_void<ReturnType>::value), void>::type execute() {
			_task->operator()();
			this->signalCompletion();
		}

		//Non-void version
		template<typename _Helper = void>
		typename std::enable_if<(std::is_void<_Helper>::value, !std::is_void<ReturnType>::value), void>::type execute() {
			_res = std::move(_task->operator()());
			this->signalCompletion();
		}

		void signalCompletion() {
			_valOK = true;
			_cv.notify_all();
		}

		std::condition_variable _cv;
		std::mutex _mut;
		std::atomic_bool _valOK = {false};

		/*std::chrono::nanoseconds _timeout;
		std::chrono::time_point<ClockType> _timeoutDate;*/

		// Cannot allocate void value;
		VoidProofReturnType _res;
		std::shared_ptr<CallableBase<ReturnType>> _task;
	};
public:
	class TaskResult {
		friend class ThreadPool;
	public:
		TaskResult() = default;

		// Blocks until result is made available by the worker thread, and returns it
		// Not available for ResultType == void specialization
		template<typename _Helper = void>
		typename std::enable_if<(std::is_void<_Helper>::value, !std::is_void<ReturnType>::value), ReturnType>::type
		returnValue() {
			return _proxy ? _proxy->returnValue() : throw std::runtime_error("Proxy not associated with a task!");
		}

		void waitForCompletion() {
			_proxy ? _proxy->waitForCompletion() : throw std::runtime_error("Proxy not associated with a task!");
		}

		bool available() {
			return _proxy ? static_cast<bool>(_proxy->_valOK) : false;
		}

	private:
		std::shared_ptr<TaskManager> _proxy;
	};

public:
	ThreadPool(std::size_t capacity) : _workerThreads(capacity) {
		for(auto &t : _workerThreads) {
			t = std::thread(std::bind(&ThreadPool::work, this));
		}
	}

	~ThreadPool() {
		if(_pendingTasks > 0) {
			std::cerr << "Some tasks are still running!" << std::endl;
			std::terminate();
		}
	}

	std::size_t threadCount() const {
		return _workerThreads.size();
	}

	void addThread() {
		_workerThreads.emplace_back(std::bind(&ThreadPool::work, this));
	}

	void join() {
		// Dirty hack…
		while(_pendingTasks > 0) {
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
		}

		this->cancel();
	}

	void cancel() {
		_alive = false;
		_taskQueue.push(nullptr);
		_taskAvailable.notify_all();

		for(auto &t : _workerThreads) {
			if(t.joinable())
				t.join();
		}

		_pendingTasks = 0;
	}

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

/*	template<typename Blabla = void>
	typename std::enable_if<std::is_same<void, Blabla>::value && std::is_same<ReturnType, std::uint64_t>::value, std::future<ReturnType>>::type
	addTask2(std::unique_ptr<std::function<std::uint64_t()>> task) {
		return std::async(std::launch::async, *task);
	}*/

private:
	void work() {
		while(_alive) {
			std::unique_lock<std::mutex> lk(_availabilityMutex);
			_taskAvailable.wait(lk, [this]() { return !_taskQueue.empty(); });

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

	std::atomic_bool _alive = {true};
	std::atomic_uint _pendingTasks = {0};
	std::vector<std::thread> _workerThreads;
};


#endif
