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

class Action;

/**
 * A transition linking 2 Action, composing a PetriNet.
 */
class Transition : public CallableTimeout<std::uint64_t> {
public:
	/**
	 * Creates an Transition object, containing a nullptr test, allowing the end of execution of Action 'previous' to provoke
	 * the execution of Action 'next', if the test is fulfilled.
	 * @param previous The starting point of the Transition
	 * @param next The arrival point of the Transition
	 */
	Transition(Action &previous, Action &next) : CallableTimeout(0), _previous(previous), _next(next) {}

	/**
	 * Checks whether the Transition can be crossed
	 * @param resultatAction The result of the Action 'previous'. This is useful when the Transition's test uses this value.
	 * @return The result of the test, true meaning that the Transition can be crossed to enable the action 'next'
	 */
	bool isFulfilled(ResultatAction resultatAction) const {
		_result = resultatAction;

		return _test->isFulfilled();
	}

	/**
	 * Invoked just before the execution of Action 'previous'.
	 */
	void actionStarted() {
		_test->willTest();
	}

	/**
	 * Invoked just after the execution of Action 'previous'.
	 */
	void actionEnded() {
		_test->didTest();
	}

	/**
	 * Returns the condition associated to the Transition
	 * @return The condition associated to the Transition
	 */
	ConditionBase const &condition() const {
		return *_test;
	}

	/**
	 * Changes the condition associated to the Transition
	 * @param test The new condition to associate to the Transition
	 */
	void setCondition(ConditionBase const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test.copy_ptr());
	}

	/**
	 * Changes the condition associated to the Transition
	 * @param test The new condition to associate to the Transition
	 */
	void setCondition(std::shared_ptr<ConditionBase> const &test) {
		_test = std::static_pointer_cast<ConditionBase>(test);
	}

	/**
	 * Gets the default Condition of this Transition, i.e. the test returning true when 'previous' execution returned the
	 * specified parameter, and false otherwise.
	 * @param result The Action return code to test the execution of 'previous' Action against
	 * @return The default Condition of the Transition
	 */
	std::shared_ptr<ConditionBase> compareResult(ResultatAction result) const {
		return make_condition_ptr<Condition>(make_callable(&Transition::checkResult, std::cref(_result), result));
	}

	/**
	 * Gets the Action 'previous', the starting point of the Transition.
	 * @return The Action 'previous', the starting point of the Transition.
	 */
	Action &previous() {
		return _previous;
	}

	/**
	 * Gets the Action 'next', the arrival point of the Transition.
	 * @return The Action 'next', the arrival point of the Transition.
	 */
	Action &next() {
		return _next;
	}

	/**
	 * Gets the name of the Transition.
	 * @return The name of the Transition.
	 */
	std::string const &name() const {
		return _name;
	}

	/**
	 * Changes the name of the Transition.
	 * @param The new name of the Transition.
	 */
	void setName(std::string const &name) {
		_name = name;
	}

	/**
	 * The delay between successive evaluations of the Transition. The runtime will not try to evaluate
	 * the Transition with a delay smaller than this delay after a previous evaluation, but only for one execution of Action 'previous'
	 * @return The minimal delay between two evaluations of the Transition.
	 */
	std::chrono::nanoseconds delayBetweenEvaluation() const {
		return _delayBetweenEvaluation;
	}

	/**
	 * Changes the delay between successive evaluations of the Transition.
	 * @param delay The new minimal delay between two evaluations of the Transition.
	 */
	void setDelayBetweenEvaluation(std::chrono::nanoseconds delay) {
		_delayBetweenEvaluation = delay;
	}

private:
	static bool checkResult(std::atomic<ResultatAction> const &r1, ResultatAction r2) {
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
