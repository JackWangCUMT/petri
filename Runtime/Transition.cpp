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

#include "Transition.h"
#include "Action.h"

namespace Petri {

	Transition::Transition(Action &previous, Action &next) : HasID(0), _previous(previous), _next(next) {}

	Transition::Transition(uint64_t id, std::string const &name, Action &previous, Action &next, TransitionCallableBase const &cond) : HasID(id), _name(name), _previous(previous), _next(next), _test(cond.copy_ptr()) {}

	bool Transition::isFulfilled(actionResult_t actionResult) const {
		return (*_test)(actionResult);
	}

	TransitionCallableBase const &Transition::condition() const {
		return *_test;
	}

	void Transition::setCondition(TransitionCallableBase const &test) {
		_test = test.copy_ptr();
	}

	Action &Transition::previous() {
		return _previous;
	}

	Action &Transition::next() {
		return _next;
	}

	std::string const &Transition::name() const {
		return _name;
	}

	void Transition::setName(std::string const &name) {
		_name = name;
	}

	std::chrono::nanoseconds Transition::delayBetweenEvaluation() const {
		return _delayBetweenEvaluation;
	}

	void Transition::setDelayBetweenEvaluation(std::chrono::nanoseconds delay) {
		_delayBetweenEvaluation = delay;
	}

}
