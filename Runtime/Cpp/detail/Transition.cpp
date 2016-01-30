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
//  Transition.cpp
//  Pétri
//
//  Created by Rémi on 09/05/2015.
//

#include "../Action.h"
#include "../Transition.h"

namespace Petri {

    struct Transition::Internals {
        Internals(Action &previous, Action &next)
                : _previous(&previous)
                , _next(&next) {}

        Internals(std::string const &name, Action &previous, Action &next, ParametrizedTransitionCallableBase const &cond)
                : _name(name)
                , _previous(&previous)
                , _next(&next)
                , _test(cond.copy_ptr()) {}

        std::string _name;
        Action *_previous;
        Action *_next;
        std::unique_ptr<ParametrizedTransitionCallableBase> _test;

        // Default delay between evaluation
        std::chrono::nanoseconds _delayBetweenEvaluation = 10ms;
    };

    Transition::Transition(Action &previous, Action &next)
            : Entity(0)
            , _internals(std::make_unique<Internals>(previous, next)) {}

    Transition::Transition(uint64_t id, std::string const &name, Action &previous, Action &next, ParametrizedTransitionCallableBase const &cond)
            : Entity(id)
            , _internals(std::make_unique<Internals>(name, previous, next, cond)) {}

    Transition::~Transition() = default;
    Transition::Transition(Transition &&) noexcept = default;

    void Transition::setPrevious(Action &previous) noexcept {
        _internals->_previous = &previous;
    }
    void Transition::setNext(Action &next) noexcept {
        _internals->_next = &next;
    }

    bool Transition::isFulfilled(PetriNet &pn, actionResult_t actionResult) const {
        return (*_internals->_test)(pn, actionResult);
    }

    ParametrizedTransitionCallableBase const &Transition::condition() const noexcept {
        return *_internals->_test;
    }

    void Transition::setCondition(TransitionCallableBase const &test) {
        auto copy = test.copy_ptr();
        auto shared_copy = std::shared_ptr<TransitionCallableBase>(copy.release());
        this->setCondition(
                        make_param_transition_callable([shared_copy](PetriNet &, actionResult_t a) { return shared_copy->operator()(a); }));
    }

    void Transition::setCondition(ParametrizedTransitionCallableBase const &test) {
        _internals->_test = test.copy_ptr();
    }

    Action &Transition::previous() noexcept {
        return *_internals->_previous;
    }

    Action &Transition::next() noexcept {
        return *_internals->_next;
    }

    std::string const &Transition::name() const noexcept {
        return _internals->_name;
    }

    void Transition::setName(std::string const &name) {
        _internals->_name = name;
    }

    std::chrono::nanoseconds Transition::delayBetweenEvaluation() const {
        return _internals->_delayBetweenEvaluation;
    }

    void Transition::setDelayBetweenEvaluation(std::chrono::nanoseconds delay) {
        _internals->_delayBetweenEvaluation = delay;
    }
}
