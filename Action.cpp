//
//  Action.cpp
//  IA Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "Action.h"
#include <list>
#include <mutex>

namespace Petri {

	struct Action::Internals {
		std::list<std::shared_ptr<Transition>> _transitions;
		std::shared_ptr<CallableBase<actionResult_t>> _action;
		std::string _name;
		std::size_t _requiredTokens = 1;

		std::size_t _currentTokens;
		std::mutex _tokensMutex;
	};

	Action::Action() : CallableTimeout(0), _internals(std::make_unique<Internals>()) {}

	/**
	 * Creates an empty action, associated to a copy ofthe specified Callable.
	 * @param action The Callable which will be copied
	 */
	Action::Action(CallableBase<actionResult_t> const &action) : CallableTimeout(0), _internals(std::make_unique<Internals>()) {
		this->setAction(action);
	}

	Action::~Action() {
		
	}

	/**
	 * Adds a Transition to the Action.
	 * @param transition the transition to be added
	 */
	void Action::addTransition(std::shared_ptr<Transition> &transition) {
		_internals->_transitions.push_back(transition);
	}

	/**
	 * Returns the Callable asociated to the action. An Action with a null Callable must not invoke this method!
	 * @return The Callable of the Action
	 */
	CallableBase<actionResult_t> &Action::action() {
		return *_internals->_action;
	}

	/**
	 * Changes the Callable associated to the Action
	 * @param action The Callable which will be copied and put in the Action
	 */
	void Action::setAction(CallableBase<actionResult_t> const &action) {
		_internals->_action = action.copy_ptr();
	}

	/**
	 * Returns the required tokens of the Action to be activated, i.e. the count of Actions which must lead to *this and terminate for *this to activate.
	 * @return The required tokens of the Action
	 */
	std::size_t Action::requiredTokens() const {
		return _internals->_requiredTokens;
	}

	/**
	 * Changes the required tokens of the Action to be activated.
	 * @return The required tokens of the Action
	 */
	void Action::setRequiredTokens(std::size_t requiredTokens) {
		_internals->_requiredTokens = requiredTokens;
	}

	/**
	 * Gets the current tokens count given to the Action by its preceding Actions.
	 * @return The current tokens count of the Action
	 */
	std::size_t &Action::currentTokens() {
		return _internals->_currentTokens;
	}

	std::mutex &Action::tokensMutex() {
		return _internals->_tokensMutex;
	}

	/**
	 * Returns the name of the Action.
	 * @return The name of the Action
	 */
	std::string const &Action::name() const {
		return _internals->_name;
	}

	/**
	 * Sets the name of the Action
	 * @param name The name of the Action
	 */
	void Action::setName(std::string const &name) {
		_internals->_name = name;
	}

	/**
	 * Returns the transitions exiting the Action.
	 * @param name The exiting transitions of the Action
	 */
	std::list<std::shared_ptr<Transition>> const &Action::transitions() const {
		return _internals->_transitions;
	}

}