//
//  Transition.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_Transition_h
#define IA_Pe_tri_Transition_h

#include "Callable.h"
#include "Condition.h"
#include <queue>
#include <list>
#include <unordered_map>
#include "ThreadPool.h"
#include <atomic>
#include <mutex>
#include <thread>
#include <deque>
#include "Log.h"
#include "Commun.h"

using namespace std::chrono_literals;

class Action;

class Transition : public CallableTimeout<std::uint64_t> {
public:
	Transition(Action &previous, Action &next) : CallableTimeout(0), _previous(previous), _next(next) {}

	bool isFulfilled(ResultatAction resultatAction) const {
		_result = resultatAction;

		return _test->isFulfilled();
	}

	void willTest() {
		_test->willTest();
	}

	void didTest() {
		_test->didTest();
	}

	ConditionBase const &condition() const {
		return *_test;
	}

	void setCondition(ConditionBase const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test.copy_ptr());
	}

	void setCondition(std::shared_ptr<ConditionBase> const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test);
	}

	std::shared_ptr<ConditionBase> compareResult(ResultatAction const &r) const {
		return make_condition_ptr<Condition>(make_callable(&Transition::checkResult, std::cref(_result), r));
	}

	Action &previous() {
		return _previous;
	}

	Action &next() {
		return _next;
	}

	std::string const &name() const {
		return _name;
	}

	void setName(std::string const &name) {
		_name = name;
	}

	std::chrono::nanoseconds delayBetweenEvaluation() const {
		return _delayBetweenEvaluation;
	}

	void setDelayBetweenEvaluation(std::chrono::nanoseconds ms) {
		_delayBetweenEvaluation = ms;
	}

	std::atomic<ResultatAction> const &mutableResult() const {
		return _result;
	}

private:
	static bool checkResult(std::atomic<ResultatAction> const &r1, ResultatAction const &r2) {
		return r1 == r2;
	}

	std::shared_ptr<ConditionBase> _test;
	Action &_previous;
	Action &_next;
	std::string _name;
	mutable std::atomic<ResultatAction> _result;

	// Default delay between evaluation
	std::chrono::nanoseconds _delayBetweenEvaluation = 10ms;
};

#endif
