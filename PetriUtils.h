//
//  StateChartUtils.h
//  IA Pétri
//
//  Created by Rémi on 12/11/2014.
//

#ifndef IA_Pe_tri_PetriUtils_h
#define IA_Pe_tri_PetriUtils_h

#include <functional>
#include <memory>
#include <dlfcn.h>
#include "Log.h"
#include "Condition.h"
#include "PetriNet.h"
#include "PetriDebug.h"

using namespace std::chrono_literals;

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

namespace PetriUtils {
	inline ResultatAction defaultAction(Action *a) {
		logInfo("Action " + a->name() + ", ID " + std::to_string(a->ID()) + " exécutée.");
		return ResultatAction::REUSSI;
	}
}

#endif
