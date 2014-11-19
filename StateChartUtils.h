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
#include "Condition.h"

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

namespace PetriUtils {
	struct indirect {
		template <class _Tp>
		inline constexpr auto operator()(_Tp&& x) const {
			return *std::forward<_Tp>(x);
		}
	};
	struct adressof {
		template <class _Tp>
		inline constexpr auto operator()(_Tp&& x) const {
			return &std::forward<_Tp>(x);
		}
	};
	struct preincr {
		template <class _Tp>
		inline constexpr auto &operator()(_Tp&& x) const {
			return ++std::forward<_Tp>(x);
		}
	};
	struct predecr {
		template <class _Tp>
		inline constexpr auto &operator()(_Tp&& x) const {
			return --std::forward<_Tp>(x);
		}
	};
	struct postincr {
		template <class _Tp>
		inline constexpr auto operator()(_Tp&& x) const {
			return std::forward<_Tp>(x)++;
		}
	};
	struct postdecr {
		template <class _Tp>
		inline constexpr auto operator()(_Tp&& x) const {
			return std::forward<_Tp>(x)--;
		}
	};

	struct shift_left {
		template <class _T1, class _T2>
		inline constexpr auto operator()(_T1&& t, _T2&& u) const {
			return std::forward<_T1>(t) << std::forward<_T2>(u);
		}
	};

	struct shift_right {
		template <class _T1, class _T2>
		inline constexpr auto operator()(_T1&& t, _T2&& u) const {
			return std::forward<_T1>(t) >> std::forward<_T2>(u);
		}
	};
}

#include "StateChart.h"

#endif
