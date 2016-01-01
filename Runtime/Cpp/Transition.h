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
//  Transition.h
//  Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef Petri_Transition_h
#define Petri_Transition_h

#include "Callable.h"
#include "Common.h"
#include <chrono>

namespace Petri {

    using namespace std::chrono_literals;

    class Action;

    using TransitionCallableBase = CallableBase<bool, actionResult_t>;

    template <typename CallableType>
    auto make_transition_callable(CallableType &&c) {
        return Callable<CallableType, std::result_of_t<CallableType(actionResult_t)>, actionResult_t>(c);
    }

    /**
     * A transition linking 2 Action, composing a PetriNet.
     */
    class Transition : public HasID<uint64_t> {
    public:
        /**
         * Creates an Transition object, containing a nullptr test, allowing the end of execution of
         * Action 'previous' to provoke
         * the execution of Action 'next', if the test is fulfilled.
         * @param previous The starting point of the Transition
         * @param next The arrival point of the Transition
         */
        Transition(Action &previous, Action &next);

        /**
         * Creates an Transition object, containing a nullptr test, allowing the end of execution of
         * Action 'previous' to provoke
         * the execution of Action 'next', if the test is fulfilled.
         * @param previous The starting point of the Transition
         * @param next The arrival point of the Transition
         */
        Transition(uint64_t id, std::string const &name, Action &previous, Action &next, TransitionCallableBase const &cond);

        /**
         * Checks whether the Transition can be crossed
         * @param actionResult The result of the Action 'previous'. This is useful when the
         * Transition's test uses this value.
         * @return The result of the test, true meaning that the Transition can be crossed to enable
         * the action 'next'
         */
        bool isFulfilled(actionResult_t actionResult) const;

        /**
         * Returns the condition associated to the Transition
         * @return The condition associated to the Transition
         */
        TransitionCallableBase const &condition() const;

        /**
         * Changes the condition associated to the Transition
         * @param test The new condition to associate to the Transition
         */
        void setCondition(TransitionCallableBase const &test);

        /**
         * Gets the Action 'previous', the starting point of the Transition.
         * @return The Action 'previous', the starting point of the Transition.
         */
        Action &previous();

        /**
         * Gets the Action 'next', the arrival point of the Transition.
         * @return The Action 'next', the arrival point of the Transition.
         */
        Action &next();

        /**
         * Gets the name of the Transition.
         * @return The name of the Transition.
         */
        std::string const &name() const;

        /**
         * Changes the name of the Transition.
         * @param The new name of the Transition.
         */
        void setName(std::string const &name);

        /**
         * The delay between successive evaluations of the Transition. The runtime will not try to
         * evaluate
         * the Transition with a delay smaller than this delay after a previous evaluation, but only
         * for one execution of Action 'previous'
         * @return The minimal delay between two evaluations of the Transition.
         */
        std::chrono::nanoseconds delayBetweenEvaluation() const;

        /**
         * Changes the delay between successive evaluations of the Transition.
         * @param delay The new minimal delay between two evaluations of the Transition.
         */
        void setDelayBetweenEvaluation(std::chrono::nanoseconds delay);

    private:
        std::unique_ptr<TransitionCallableBase> _test;
        Action &_previous;
        Action &_next;
        std::string _name;

        // Default delay between evaluation
        std::chrono::nanoseconds _delayBetweenEvaluation = 10ms;
    };
}

#endif
