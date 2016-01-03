/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

//
//  Action.cpp
//  Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "Action.h"
#include <list>
#include <mutex>

namespace Petri {

    struct Action::Internals {
        Internals() = default;
        Internals(std::string const &name, size_t requiredTokens)
                : _name(name)
                , _requiredTokens(requiredTokens) {}
        std::list<Transition> _transitions;
        std::list<std::reference_wrapper<Transition>> _transitionsLeadingToMe;
        std::unique_ptr<ParametrizedActionCallableBase> _action;
        std::string _name;
        std::size_t _requiredTokens = 1;

        std::size_t _currentTokens;
        std::mutex _tokensMutex;
    };

    Action::Action()
            : HasID(0)
            , _internals(std::make_unique<Internals>()) {}

    /**
     * Creates an empty action, associated to a copy of the specified Callable.
     * @param action The Callable which will be copied
     */
    Action::Action(uint64_t id, std::string const &name, ActionCallableBase const &action, size_t requiredTokens)
            : HasID(id)
            , _internals(std::make_unique<Internals>(name, requiredTokens)) {
        this->setAction(action);
    }
    Action::Action(uint64_t id, std::string const &name, actionResult_t (*action)(), size_t requiredTokens)
            : Action(id, name, make_action_callable(action), requiredTokens) {}

    /**
     * Creates an empty action, associated to a copy of the specified Callable.
     * @param action The Callable which will be copied
     */
    Action::Action(uint64_t id, std::string const &name, ParametrizedActionCallableBase const &action, size_t requiredTokens)
            : HasID(id)
            , _internals(std::make_unique<Internals>(name, requiredTokens)) {
        this->setAction(action);
    }
    Action::Action(uint64_t id, std::string const &name, actionResult_t (*action)(PetriNet &), size_t requiredTokens)
            : Action(id, name, make_param_action_callable(action), requiredTokens) {}

    Action::Action(Action &&a)
            : HasID<uint64_t>(a.ID())
            , _internals(std::move(a._internals)) {
        for(auto &t : _internals->_transitions) {
            t.setPrevious(*this);
        }
        for(Transition &t : _internals->_transitionsLeadingToMe) {
            t.setNext(*this);
        }
    }

    Action::~Action() = default;

    Transition &Action::addTransition(Transition t) {
        _internals->_transitions.push_back(std::move(t));

        Transition &returnValue = _internals->_transitions.back();
        returnValue.next()._internals->_transitionsLeadingToMe.push_back(returnValue);

        return returnValue;
    }

    Transition &Action::addTransition(Action &next) {
        return this->addTransition(Transition(*this, next));
    }

    Transition &
    Action::addTransition(uint64_t id, std::string const &name, Action &next, TransitionCallableBase const &cond) {
        return this->addTransition(Transition(id, name, *this, next, cond));
    }
    Transition &Action::addTransition(uint64_t id, std::string const &name, Action &next, bool (*cond)(actionResult_t)) {
        return addTransition(id, name, next, make_transition_callable(cond));
    }

    /**
     * Returns the Callable asociated to the action. An Action with a null Callable must not invoke
     * this method!
     * @return The Callable of the Action
     */
    ParametrizedActionCallableBase &Action::action() {
        return *_internals->_action;
    }

    /**
     * Changes the Callable associated to the Action
     * @param action The Callable which will be copied and put in the Action
     */
    void Action::setAction(ActionCallableBase const &action) {
        auto copy = action.copy_ptr();
        auto shared_copy = std::shared_ptr<ActionCallableBase>(copy.release());
        this->setAction(
        make_param_action_callable([shared_copy](PetriNet &) { return shared_copy->operator()(); }));
    }
    void Action::setAction(actionResult_t (*action)()) {
        this->setAction(make_action_callable(action));
    }

    /**
     * Changes the Callable associated to the Action
     * @param action The Callable which will be copied and put in the Action
     */
    void Action::setAction(ParametrizedActionCallableBase const &action) {
        _internals->_action = action.copy_ptr();
    }
    void Action::setAction(actionResult_t (*action)(PetriNet &)) {
        this->setAction(make_param_action_callable(action));
    }

    /**
     * Returns the required tokens of the Action to be activated, i.e. the count of Actions which
     * must lead to *this and terminate for *this to activate.
     * @return The required tokens of the Action
     */
    std::size_t Action::requiredTokens() const {
        return _internals->_requiredTokens;
    }

    /**
     * Changes the required tokens of the Action to be activated.
     * @param requiredTokens The new required tokens count
     * @return The required tokens of the Action
     */
    void Action::setRequiredTokens(std::size_t requiredTokens) {
        _internals->_requiredTokens = requiredTokens;
    }

    /**
     * Gets the current tokens count given to the Action by its preceding Actions.
     * @return The current tokens count of the Action
     */
    std::size_t Action::currentTokens() {
        return _internals->_currentTokens;
    }

    std::size_t &Action::currentTokensRef() {
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
    std::list<Transition> const &Action::transitions() const {
        return _internals->_transitions;
    }
}