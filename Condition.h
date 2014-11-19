//
//  Condition.h
//  IA Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef IA_Pe_tri_Condition_h
#define IA_Pe_tri_Condition_h

#include <chrono>

struct ConditionBase : CallableBool {
	virtual bool operator()() {
		return this->isFulfilled();
	}

	virtual bool isFulfilled() = 0;
	virtual void willTest() {}
	virtual void didTest() {}
};

template<typename ConditionType>
struct ConditionBaseCopyPtr : ConditionBase {
	virtual std::shared_ptr<CallableBase<bool>> copy_ptr() const override {
		return std::make_shared<ConditionType>(static_cast<ConditionType const &>(*this));
	}
};

struct TimeoutCondition : ConditionBaseCopyPtr<TimeoutCondition> {
	// We want a steady clock (no adjustments, only ticking forward in time), but it would be better if we got an high resolution clock.
	using ClockType = std::conditional<std::chrono::high_resolution_clock::is_steady, std::chrono::high_resolution_clock, std::chrono::steady_clock>::type;
	
	template <class Rep, class Period>
	TimeoutCondition(std::chrono::duration<Rep, Period> d) : _timeout(std::chrono::duration_cast<std::chrono::nanoseconds>(d)) {

	}

	virtual void willTest() override {
		_referencePoint = ClockType::now();
	}

	virtual bool isFulfilled() override {
		return ClockType::now() - _referencePoint >= _timeout;
	}

private:
	std::chrono::time_point<ClockType> _referencePoint;
	std::chrono::nanoseconds const _timeout;
};

struct Condition : public ConditionBaseCopyPtr<Condition> {
	Condition(CallableBool const &test) : _test(test.copy_ptr()) {}
	Condition(Condition const &cond) : _test(cond._test->copy_ptr()) {}
	Condition(std::shared_ptr<CallableBool> const &test) : _test(test) {}
	virtual bool isFulfilled() override {
		return _test->operator()();
	}

private:
	std::shared_ptr<CallableBool> _test;
};

// Creates a Callable<bool, Cond, Args...> from parameters and encapsulates it in the condition
template<typename ConditionType, typename Cond, typename... Args>
std::shared_ptr<ConditionType> make_condition_ptr(Cond &&cond, Args &&...args) {
	return std::make_shared<ConditionType>(make_callable(std::forward<Cond>(cond), std::forward<Args>(args)...));
}

#endif
