//
//  Transition.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_Transition_h
#define Petri_Transition_h

#include "Common.h"
#include "Callable.h"
#include <chrono>

namespace Petri {

	using namespace std::chrono_literals;

	class Action;

	struct TransitionCallableBase {
	public:
		virtual bool operator()(actionResult_t res) = 0;
		virtual std::unique_ptr<TransitionCallableBase> copy_ptr() const = 0;
	};

	template<typename CallableType>
	struct TransitionCallable : public TransitionCallableBase {
		TransitionCallable(CallableType const &c) : _c(c) {}
		TransitionCallable(TransitionCallable const &c) : _c(c._c) {}

		virtual bool operator()(actionResult_t res) override { return _c(res); }

		virtual std::unique_ptr<TransitionCallableBase> copy_ptr() const override { return std::make_unique<TransitionCallable<CallableType>>(*this); }

	private:
		CallableType _c;
	};

	template<typename CallableType>
	auto make_transition_callable(CallableType &&c) {
		return TransitionCallable<CallableType>(c);
	}

	/**
	 * A transition linking 2 Action, composing a PetriNet.
	 */
	class Transition : public CallableTimeout<uint64_t> {
	public:
		Transition(Transition const &t) : CallableTimeout<uint64_t>(this->ID()), _previous(t._previous), _next(t._next), _name(t._name), _delayBetweenEvaluation(t._delayBetweenEvaluation) {
			this->setCondition(t.condition());
		}

		/**
		 * Creates an Transition object, containing a nullptr test, allowing the end of execution of Action 'previous' to provoke
		 * the execution of Action 'next', if the test is fulfilled.
		 * @param previous The starting point of the Transition
		 * @param next The arrival point of the Transition
		 */
		Transition(Action &previous, Action &next) : CallableTimeout(0), _previous(previous), _next(next) {}

		/**
		 * Checks whether the Transition can be crossed
		 * @param actionResult The result of the Action 'previous'. This is useful when the Transition's test uses this value.
		 * @return The result of the test, true meaning that the Transition can be crossed to enable the action 'next'
		 */
		bool isFulfilled(actionResult_t actionResult) const {
			return (*_test)(actionResult);
		}

		/**
		 * Returns the condition associated to the Transition
		 * @return The condition associated to the Transition
		 */
		TransitionCallableBase const &condition() const {
			return *_test;
		}

		/**
		 * Changes the condition associated to the Transition
		 * @param test The new condition to associate to the Transition
		 */
		void setCondition(TransitionCallableBase const &test) {
			_test = test.copy_ptr();
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
		std::unique_ptr<TransitionCallableBase> _test;
		Action &_previous;
		Action &_next;
		std::string _name;
		
		// Default delay between evaluation
		std::chrono::nanoseconds _delayBetweenEvaluation = 10ms;
	};

}

#endif
