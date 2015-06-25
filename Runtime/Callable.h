//
//  Callable.h
//  IA Pétri
//
//  Created by Rémi on 09/05/2015.
//

#ifndef Petri_Callable_h
#define Petri_Callable_h

#include <memory>
#include <type_traits>

template<typename ReturnType, typename ...Args>
struct CallableBase {
	virtual ReturnType operator()(Args ...) = 0;
	virtual std::unique_ptr<CallableBase<ReturnType, Args...>> copy_ptr() const = 0;
};

template<typename CallableType, typename ReturnType, typename ...Args>
struct Callable : public CallableBase<ReturnType, Args...> {
	Callable(CallableType const &c) : _c(c) {}
	Callable(Callable const &c) : _c(c._c) {}

	virtual ReturnType operator()(Args ...args) override { return _c(args...); }

	virtual std::unique_ptr<CallableBase<ReturnType, Args...>> copy_ptr() const override {
		return static_cast<std::unique_ptr<CallableBase<ReturnType, Args...>>>(std::make_unique<Callable<CallableType, ReturnType, Args...>>(*this));
	}

private:
	CallableType _c;
};

#endif
