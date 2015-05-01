//
//  Condition.h
//  Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef Petri_Condition_h
#define Petri_Condition_h

#include <chrono>
#include "Callable.h"
#include <functional>

namespace Petri {

	template<typename _ActionResult>
	struct ConditionBase {
		virtual bool isFulfilled(_ActionResult result) = 0;
		virtual void willTest() {}
		virtual void didTest() {}
	};

	template<template <typename> class ConditionType, typename _ActionResult>
	struct ConditionBaseCopyPtr : ConditionBase<_ActionResult> {
		virtual std::shared_ptr<ConditionBase<_ActionResult> > copy_ptr() const {
			return std::make_shared<ConditionType<_ActionResult>>(static_cast<ConditionType<_ActionResult> const &>(*this));
		}
	};

	template<typename _ActionResult>
	struct TimeoutCondition : ConditionBaseCopyPtr<TimeoutCondition, _ActionResult> {
		// We want a steady clock (no adjustments, only ticking forward in time), but it would be better if we got an high resolution clock.
		using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;

		template <class Rep, class Period>
		TimeoutCondition(std::chrono::duration<Rep, Period> d) : _timeout(std::chrono::duration_cast<std::chrono::nanoseconds>(d)) {

		}

		TimeoutCondition(TimeoutCondition<_ActionResult> const &) = default;

		virtual void willTest() override {
			_referencePoint = ClockType::now();
		}

		virtual bool isFulfilled(_ActionResult result) override {
			return ClockType::now() - _referencePoint >= _timeout;
		}

	private:
		std::chrono::time_point<ClockType> _referencePoint;
		std::chrono::nanoseconds const _timeout;
	};

	template<typename _ActionResult>
	struct Condition : public ConditionBaseCopyPtr<Condition, _ActionResult> {
		Condition(std::function<bool(_ActionResult)> const &test) : _test(test) {}
		Condition(Condition const &cond) : _test(cond._test) {}
		virtual bool isFulfilled(_ActionResult result) override {
			return _test.operator()(result);
		}

	private:
		std::function<bool(_ActionResult)> _test;
	};

	// Creates a Callable<bool, Cond, Args...> from parameters and encapsulates it in the condition
	template<typename _ActionResult, template<typename> class ConditionType, typename Cond, typename... Args>
	std::shared_ptr<ConditionType<_ActionResult>> make_condition_ptr(Cond &&cond, Args &&...args) {
		return std::make_shared<ConditionType<_ActionResult>>(make_callable(std::forward<Cond>(cond), std::forward<Args>(args)...));
	}

}

#endif
