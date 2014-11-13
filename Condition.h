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
	virtual bool isCheckResult() const = 0;
	virtual void willTest() {}
	virtual void didTest() {}
};

template<typename ConditionType, bool CheckResult>
struct ConditionBaseCopyPtr : ConditionBase {
	virtual std::shared_ptr<CallableBase<bool>> copy_ptr() const override {
		return std::make_shared<ConditionType>(static_cast<ConditionType const &>(*this));
	}

	virtual bool isCheckResult() const override {
		return CheckResult;
	}
};

struct TimeoutCondition : ConditionBaseCopyPtr<TimeoutCondition, false> {
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

template<typename ConditionType>
struct UnaryCondition : public ConditionBaseCopyPtr<ConditionType, false> {
	UnaryCondition(CallableBool const &test) : _test(test.copy_ptr()) {}
	UnaryCondition(UnaryCondition const &cond) : _test(cond._test->copy_ptr()) {}
	UnaryCondition(std::shared_ptr<CallableBool> const &test) : _test(test) {}

	std::shared_ptr<CallableBool> _test;
};

template<typename ConditionType>
struct BinaryCondition : public ConditionBaseCopyPtr<ConditionType, false> {
	BinaryCondition(CallableBool const &c1, CallableBool const &c2) : _c1(c1.copy_ptr()), _c2(c2.copy_ptr()) {}
	BinaryCondition(BinaryCondition const &cond) : _c1(cond._c1->copy_ptr()), _c2(cond._c2->copy_ptr()) {}
	BinaryCondition(std::shared_ptr<CallableBool> const &c1, std::shared_ptr<CallableBool> const &c2) : _c1(c1), _c2(c2) {}

	std::shared_ptr<CallableBool> _c1, _c2;
};

struct Condition : public UnaryCondition<Condition> {
	virtual bool isFulfilled() override {
		return _test->operator()();
	}
	using UnaryCondition::UnaryCondition;
};

struct NotCondition : public UnaryCondition<NotCondition> {
	virtual bool isFulfilled() override {
		return !_test->operator()();
	}
	using UnaryCondition::UnaryCondition;
};

struct AndCondition : public BinaryCondition<AndCondition> {
	virtual bool isFulfilled() override {
		return _c1->operator()() && _c2->operator()();
	}

	using BinaryCondition::BinaryCondition;
};

struct OrCondition : public BinaryCondition<OrCondition> {
	virtual bool isFulfilled() override {
		return _c1->operator()() || _c2->operator()();
	}
	using BinaryCondition::BinaryCondition;
};

// Creates a Callable<bool, Cond, Args...> from parameters and encapsulates it in the unary condition
template<typename ConditionType, typename Cond, typename... Args>
std::enable_if_t<std::is_base_of<UnaryCondition<ConditionType>, ConditionType>::value, std::shared_ptr<ConditionType>> make_condition_ptr(Cond &&cond, Args &&...args) {
	return std::make_shared<ConditionType>(make_callable(std::forward<Cond>(cond), std::forward<Args>(args)...));
}

// Creates a Callable<bool, Cond, Args...> from parameters and encapsulates it in the binary condition
template<typename ConditionType, typename Cond1, typename Cond2>
std::enable_if_t<std::is_base_of<BinaryCondition<ConditionType>, ConditionType>::value, std::shared_ptr<ConditionType>> make_condition_ptr(Cond1 const &cond1, Cond2 const &cond2) {
	return std::make_shared<ConditionType>(cond1, cond2);
}

#endif
