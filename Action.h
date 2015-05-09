//
//  Action.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_Action_h
#define Petri_Action_h

#include "Transition.h"
#include <mutex>
#include <list>
#include "Callable.h"

namespace Petri {

	using namespace std::chrono_literals;

	using ActionCallableBase = CallableBase<actionResult_t>;

	template<typename CallableType>
	auto make_action_callable(CallableType &&c) {
		return Callable<CallableType, actionResult_t>(c);
	}

	/**
	 * A state composing a PetriNet.
	 */
	class Action : public HasID<uint64_t> {
	public:
		/**
		 * Creates an empty action, associated to a null CallablePtr.
		 */
		Action();

		/**
		 * Creates an empty action, associated to a copy ofthe specified Callable.
		 * @param action The Callable which will be copied
		 */
		Action(uint64_t id, std::string const &name, ActionCallableBase const &action, size_t requiredTokens);
		Action(Action &&);

		~Action();

		/**
		 * Adds a Transition to the Action.
		 * @param transition the transition to be added
		 */
		void addTransition(Transition transition);
		void addTransition(uint64_t id, std::string const &name, Action &next, TransitionCallableBase const &cond);

		/**
		 * Returns the Callable asociated to the action. An Action with a null Callable must not invoke this method!
		 * @return The Callable of the Action
		 */
		ActionCallableBase &action();

		/**
		 * Changes the Callable associated to the Action
		 * @param action The Callable which will be copied and put in the Action
		 */
		void setAction(ActionCallableBase const &action);

		/**
		 * Returns the required tokens of the Action to be activated, i.e. the count of Actions which must lead to *this and terminate for *this to activate.
		 * @return The required tokens of the Action
		 */
		std::size_t requiredTokens() const;

		/**
		 * Changes the required tokens of the Action to be activated.
		 * @return The required tokens of the Action
		 */
		void setRequiredTokens(std::size_t requiredTokens);

		/**
		 * Gets the current tokens count given to the Action by its preceding Actions.
		 * @return The current tokens count of the Action
		 */
		std::size_t &currentTokens();

		std::mutex &tokensMutex();

		/**
		 * Returns the name of the Action.
		 * @return The name of the Action
		 */
		std::string const &name() const;

		/**
		 * Sets the name of the Action
		 * @param name The name of the Action
		 */
		void setName(std::string const &name);

		/**
		 * Returns the transitions exiting the Action.
		 * @param name The exiting transitions of the Action
		 */
		std::list<Transition> const &transitions() const;

	private:
		struct Internals;
		std::unique_ptr<Internals> _internals;
	};
}

#endif
