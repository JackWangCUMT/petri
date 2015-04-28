//
//  CallableImpl.hpp
//  Pétri
//
//  Created by Rémi on 30/11/2014.
//

namespace Callable_detail {
	template<bool Last, typename CallableType, typename Tuple, typename... Args2>
	struct callImpl {};

	template<bool MemberFunction, typename CallableType, typename... Args2>
	struct callFinal {};

	template<bool Pointer, typename CallableType, typename... Args2>
	struct callFinalMember {};

	template<typename Tuple, bool callable, int N>
	struct getArg {};

	template<typename Tuple, bool pointer, int N>
	struct dereference {};

	template<typename T>
	struct isCallable : std::integral_constant<bool, false> {};

	template<typename ReturnType, typename CallableType, typename... Args>
	struct isCallable<Callable<ReturnType, CallableType, Args...> &> : std::integral_constant<bool, true> {};

	template<typename ReturnType>
	struct isCallable<std::shared_ptr<CallableBase<ReturnType>> &> : std::integral_constant<bool, true> {};

	template<typename T>
	struct isPointer : std::integral_constant<bool, false> {};

	template<typename ReturnType>
	struct isPointer<std::shared_ptr<CallableBase<ReturnType>> &> : std::integral_constant<bool, true> {};

	// Not a member function pointer, regular call
	template<typename CallableType, typename... Args2>
	struct callFinal<false, CallableType, Args2...> {
		static auto call(CallableType &callable, Args2 ...args) -> decltype(callable(args...)) {
			return callable(args...);
		}
	};

	// Member function pointer, so we have to call it with the first argument as *this object
	template<typename CallableType, typename Arg, typename... Args2>
	struct callFinal<true, CallableType, Arg, Args2...> {
		static auto call(CallableType &callable, Arg &arg, Args2 ...args) -> decltype(callFinalMember<std::is_pointer<std::remove_reference_t<Arg>>::value, CallableType, Arg, Args2...>::call(callable, arg, args...)) {
			return callFinalMember<std::is_pointer<std::remove_reference_t<Arg>>::value, CallableType, Arg, Args2...>::call(callable, arg, args...);
		}
	};

	template<typename CallableType, typename Arg, typename... Args2>
	struct callFinalMember<false, CallableType, Arg, Args2...> {
		static auto call(CallableType &callable, Arg &arg, Args2 ...args) -> decltype((arg.*callable)(args...)) {
			return (arg.*callable)(args...);
		}
	};

	template<typename CallableType, typename Arg, typename... Args2>
	struct callFinalMember<true, CallableType, Arg, Args2...> {
		static auto call(CallableType &callable, Arg &arg, Args2 ...args) -> decltype((arg->*callable)(args...)) {
			return (arg->*callable)(args...);
		}
	};

	// Intermediate calls to unroll the argument list, and call each item of this list if they are Callable
	template<typename Tuple, typename CallableType, typename... Args2>
	struct callImpl<false, CallableType, Tuple, Args2...> {
		static constexpr bool isCallableValue = isCallable<decltype(std::get<sizeof...(Args2)>(std::declval<Tuple &>()))>::value;

		typedef getArg<Tuple, isCallableValue, sizeof...(Args2)> getArgType;
		typedef callImpl<sizeof...(Args2) + 1 == std::tuple_size<Tuple>(), CallableType, Tuple, Args2..., typename getArgType::type> NextImplType;

		static auto call(CallableType &callable, Tuple &tuple, Args2 ...args) -> decltype(NextImplType::call(callable, tuple, args..., getArgType::call(tuple))) {
			return NextImplType::call(callable, tuple, args..., getArgType::call(tuple));
		}
	};

	// Final call to make the actual function call
	template<typename Tuple, typename CallableType, typename... Args2>
	struct callImpl<true, CallableType, Tuple, Args2...> {
		static auto call(CallableType &callable, Tuple &tuple, Args2 ...args) -> decltype(callFinal<std::is_member_function_pointer<CallableType>::value, CallableType, Args2...>::call(callable, args...)) {
			return callFinal<std::is_member_function_pointer<CallableType>::value, CallableType, Args2...>::call(callable, args...);
		}
	};

	// Argument is a Callable
	template<typename Tuple, int N>
	struct getArg<Tuple, true, N> {
		typedef decltype(std::get<N>(std::declval<Tuple &>())) elementType;

		static constexpr bool isPointerValue = isPointer<elementType>::value;
		typedef dereference<Tuple, isPointerValue, N> dereferenceType;

		typedef typename std::add_const<decltype(dereferenceType::call(std::declval<Tuple &>()))>::type type;

		static type call(Tuple &tuple) {
			return dereferenceType::call(tuple);
		}
	};

	// Argument is not a Callable
	template<typename Tuple, int N>
	struct getArg<Tuple, false, N> {
		typedef typename std::add_const<decltype(std::get<N>(std::declval<Tuple &>()))>::type type;

		static type call(Tuple &tuple) {
			return std::get<N>(tuple);
		}
	};

	// Pointer
	template<typename Tuple, int N>
	struct dereference<Tuple, true, N> {
		typedef decltype(std::get<N>(std::declval<Tuple &>())) elementType;
		typedef typename std::add_const<decltype(std::declval<elementType>()->operator()())>::type type;

		static type call(Tuple &tuple) {
			return std::get<N>(tuple)->operator()();
		}
	};

	// Not pointer
	template<typename Tuple, int N>
	struct dereference<Tuple, false, N> {
		typedef decltype(std::get<N>(std::declval<Tuple &>())) elementType;
		typedef typename std::add_const<decltype(std::declval<elementType>()())>::type type;

		static type call(Tuple &tuple) {
			return std::get<N>(tuple)();
		}
	};
}

template<typename ReturnType, typename CallableType, typename... Args>
ReturnType Callable<ReturnType, CallableType, Args...>::operator()() {
	return Callable_detail::callImpl<std::tuple_size<decltype(_args)>() == 0, CallableType, decltype(_args)>::call(_c, _args);
}

template<typename CallableType, typename... Args>
auto make_callable(CallableType &&c, Args ...args) {
	typedef std::tuple<Args...> Tuple;
	typedef decltype(Callable_detail::callImpl<sizeof...(Args) == 0, CallableType, Tuple>::call(c, std::declval<Tuple &>())) ComputedReturnType;

	return Callable<ComputedReturnType, CallableType, Args...>(c, args...);
}

template<typename CallableType, typename... Args>
auto make_callable_ptr(CallableType &&c, Args ...args) {
	typedef std::tuple<Args...> Tuple;
	typedef decltype(Callable_detail::callImpl<sizeof...(Args) == 0, CallableType, Tuple>::call(c, std::declval<Tuple &>())) ComputedReturnType;

	return static_cast<std::shared_ptr<CallableBase<ComputedReturnType>>>(std::make_shared<Callable<ComputedReturnType, CallableType, Args...>>(c, args...));
}
