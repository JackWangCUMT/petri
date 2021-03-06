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
//  Action.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_Action_h
#define Petri_Action_h

#include "Callable.h"
#include "Transition.h"
#include <list>
#include <mutex>

namespace Petri {

    using namespace std::chrono_literals;

    class PetriNet;

    using ActionCallableBase = CallableBase<actionResult_t>;
    using ParametrizedActionCallableBase = CallableBase<actionResult_t, PetriNet &>;

    template <typename CallableType>
    auto make_action_callable(CallableType &&c) {
        return Callable<CallableType, actionResult_t>(c);
    }

    template <typename CallableType>
    auto make_param_action_callable(CallableType &&c) {
        return Callable<CallableType, actionResult_t, PetriNet &>(c);
    }

    /**
     * A state composing a PetriNet.
     */
    class Action : public Entity {
        friend class PetriNet;

    public:
        friend class Petri::Transition;
        /**
         * Creates an empty action, associated to a null CallablePtr.
         */
        Action();

        /**
         * Creates an empty action, associated to a copy of the specified Callable.
         * @param id The ID of the new action.
         * @param name The name of the new action.
         * @param action The Callable which will be called when the action is run.
         * @param requiredTokens The number of tokens that must be inside the active action for it
         * to execute.
         */
        Action(uint64_t id, std::string const &name, ActionCallableBase const &action, size_t requiredTokens);
        Action(uint64_t id, std::string const &name, actionResult_t (*action)(), size_t requiredTokens);

        /**
         * Creates an empty action, associated to a copy of the specified Callable.
         * @param id The ID of the new action.
         * @param name The name of the new action.
         * @param action The Callable which will be called when the action is run.
         * @param requiredTokens The number of tokens that must be inside the active action for it
         * to execute.
         */
        Action(uint64_t id, std::string const &name, ParametrizedActionCallableBase const &action, size_t requiredTokens);
        Action(uint64_t id, std::string const &name, actionResult_t (*action)(PetriNet &), size_t requiredTokens);

        Action(Action &&) noexcept;
        Action(Action const &) = delete;

        ~Action();

        /**
         * Adds a Transition to the Action.
         * @param id the id of the Transition
         * @param name the name of the transition to be added
         * @param next the Action following the transition to be added
         * @param cond the condition of the Transition to be added
         * @return The newly created transition.
         */
        Transition &addTransition(uint64_t id,
                                  std::string const &name,
                                  Action &next,
                                  ParametrizedTransitionCallableBase const &cond);
        Transition &
        addTransition(uint64_t id, std::string const &name, Action &next, TransitionCallableBase const &cond);
        Transition &addTransition(uint64_t id, std::string const &name, Action &next, bool (*cond)(actionResult_t));
        Transition &
        addTransition(uint64_t id, std::string const &name, Action &next, bool (*cond)(PetriNet &, actionResult_t));

        /**
         * Adds a Transition to the Action.
         * @param next the Action following the transition to be added
         * @return The newly created transition.
         */
        Transition &addTransition(Action &next);

        /**
         * Returns the Callable asociated to the action. An Action with a null Callable must not
         * invoke this method!
         * @return The Callable of the Action
         */
        ParametrizedActionCallableBase &action() noexcept;

        /**
         * Changes the Callable associated to the Action
         * @param action The Callable which will be copied and put in the Action
         */
        void setAction(ActionCallableBase const &action);
        void setAction(actionResult_t (*action)());

        /**
         * Changes the Callable associated to the Action
         * @param action The Callable which will be copied and put in the Action
         */
        void setAction(ParametrizedActionCallableBase const &action);
        void setAction(actionResult_t (*action)(PetriNet &));

        /**
         * Returns the required tokens of the Action to be activated, i.e. the count of Actions
         * which must lead to *this and terminate for *this to activate.
         * @return The required tokens of the Action
         */
        std::size_t requiredTokens() const noexcept;

        /**
         * Changes the required tokens of the Action to be activated.
         * @param requiredTokens The new required tokens count
         */
        void setRequiredTokens(std::size_t requiredTokens) noexcept;

        /**
         * Gets the current tokens count given to the Action by its preceding Actions.
         * @return The current tokens count of the Action
         */
        std::size_t currentTokens() noexcept;

        /**
         * Returns the name of the Action.
         * @return The name of the Action
         */
        std::string const &name() const noexcept;

        /**
         * Sets the name of the Action
         * @param name The name of the Action
         */
        void setName(std::string const &name) noexcept(std::is_nothrow_move_constructible<std::string>::value);

        /**
         * Returns the transitions exiting the Action.
         */
        std::list<Transition> const &transitions() const noexcept;

    private:
        std::size_t &currentTokensRef() noexcept;
        std::mutex &tokensMutex() noexcept;

        Transition &addTransition(Transition t);

        struct Internals;
        std::unique_ptr<Internals> _internals;
    };
}

#endif
