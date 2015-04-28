//
//  Callable.h
//  Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef Petri_Callable_h
#define Petri_Callable_h

#include <tuple>
#include <memory>

namespace Petri {

	/**
	 * An abstract class which encapsulates a function, method, lambda or functor call with all of his parameters,
	 * for calling when requested. The parameters can optionally be lazy-evaluated upon the call.
	 * This abstact class serves the simple purpose of hiding the parameters types, in order to allow simple copying,
	 * when passing the Callable object as a function argument or when inserting in a container among others.
	 */
	template<typename ReturnType>
	struct CallableBase {
		/**
		 * Calls the function method, lambda or functor with its encapsulated parameters. Lazy-evaluated parameters are resolved during this call.
		 * @return The result of the call, same as if the callable object was called as a regulat function-type call.
		 */
		virtual ReturnType operator()() = 0;

		/**
		 * Creates a deep copy of the Callable object, including the callable object and all of its parameters.
		 * It is a shared_ptr, but the pointed-to object is a brand new Callable object.
		 * @return A pointer to a new, deep-copied Callable object
		 */
		virtual std::shared_ptr<CallableBase<ReturnType>> copy_ptr() const = 0;
	};

	/**
	 * This is the actual wrapper of a function, method, lambda or functor call with all of his parameters.
	 * To easily pass the object as a generic Callable, consider using its abstract base class CallableBase.
	 */
	template<typename ReturnType, typename CallableType, typename... Args>
	struct Callable : CallableBase<ReturnType> {
	public:
		/**
		 * Creates a Callable from the given callable object and the given parameters.
		 * @param c The callable object, must provide an operator() method (i.e., it must be a function pointer, a method pointer, a lambda expression or a functor).
		 * @param args The callable object's arguments. c(args...) must be a valid function call.
		 */
		Callable(CallableType const &c, Args ...args) : _c(c), _args(args...) {}

		/**
		 * Creates a deep copy of the Callable.
		 * @param c The Callable object to duplicate.
		 */
		Callable(Callable const &c) : _c(c._c), _args(c._args) {}

		/**
		 * Moves the Callable's internals from c to *this.
		 * @param c The Callable object to move to *this.
		 */
		Callable(Callable &&c) : _c(std::move(c._c)), _args(std::move(c._args)) {}

		/**
		 * Calls the function method, lambda or functor with its encapsulated parameters. Lazy-evaluated parameters are resolved during this call.
		 * @return The result of the call, same as if the callable object was called as a regulat function-type call.
		 */
		virtual ReturnType operator()() override;

		/**
		 * Creates a deep copy of the Callable object, including the callable object and all of its parameters.
		 * It is a shared_ptr, but the pointed-to object is a brand new Callable object.
		 * @return A pointer to a new, deep-copied Callable object
		 */

		virtual std::shared_ptr<CallableBase<ReturnType>> copy_ptr() const override {
			return std::make_shared<Callable<ReturnType, CallableType, Args...>>(*this);
		}

	private:
		CallableType _c;
		std::tuple<Args...> _args;
	};

	/**
	 * Creates a Callable object using the provided callable object and parameters. All the type template paramaters of Callable class
	 * are resolved automatically.
	 * c(args...) must be a valid function call.
	 * @param c The callable object
	 * @param args The parameters to give to the callable object
	 * @return A Callable encasulating the call c(args...)
	 */
	template<typename CallableType, typename... Args>
	auto make_callable(CallableType &&c, Args ...args);

	/**
	 * Creates a Callable object pointer managed by a std::shared_ptr, using the provided callable object and parameters.
	 * All the type template paramaters of Callable class are resolved automatically.
	 * c(args...) must be a valid function call.
	 * @param c The callable object
	 * @param args The parameters to give to the callable object
	 * @return A shared_ptr managing a Callable encasulating the call c(args...)
	 */
	template<typename CallableType, typename... Args>
	auto make_callable_ptr(CallableType &&c, Args ...args);

#include "CallableImpl.hpp"

	using CallableBool = CallableBase<bool>;

	template<typename T>
	struct CallableTimeout {
	public:
		CallableTimeout(T id) : _id(id) { }

		T ID() const {
			return _id;
		}

		void setID(T id) {
			_id = id;
		}

		template<typename ReturnType>
		ReturnType operator()(CallableBase<ReturnType> &callable) {
			callable();
		}
		
	private:
		T _id;
	};

}

#endif
