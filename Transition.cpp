//
//  Transition.cpp
//  IA Pétri
//
//  Created by Rémi on 09/05/2015.
//

#include "Transition.h"
#include "Action.h"

namespace Petri {

	Transition::Transition(Transition const &t) : HasID<uint64_t>(this->ID()), _previous(t._previous), _next(t._next), _name(t._name), _delayBetweenEvaluation(t._delayBetweenEvaluation) {
		this->setCondition(t.condition());
	}

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
