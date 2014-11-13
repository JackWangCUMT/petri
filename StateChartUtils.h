//
//  StateChartUtils.h
//  IA Pétri
//
//  Created by Rémi on 12/11/2014.
//

#ifndef IA_Pe_tri_StateChartUtils_h
#define IA_Pe_tri_StateChartUtils_h

#include <functional>
#include "Log.h"

using namespace std::chrono_literals;

enum class ResultatAction {
	ActionReussie,
	ActionEchec
};

struct CheckResultCondition : ConditionBaseCopyPtr<CheckResultCondition, true> {
	CheckResultCondition(std::atomic<ResultatAction> const &toCheck, ResultatAction expected) : _toCheck(toCheck), _expected(expected) {}

	ResultatAction expected() const {
		return _expected;
	}

	virtual bool isFulfilled() override {
	return _toCheck.load() == _expected;
}
std::atomic<ResultatAction> const &_toCheck;
ResultatAction const _expected;
};

#include "StateChart.h"

#endif
