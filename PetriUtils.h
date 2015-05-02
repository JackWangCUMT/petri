//
//  StateChartUtils.h
//  Pétri
//
//  Created by Rémi on 12/11/2014.
//

#ifndef Petri_PetriUtils_h
#define Petri_PetriUtils_h

#include <functional>
#include <memory>
#include <thread>

namespace Petri {

	using namespace std::chrono_literals;

	enum class ActionResult {
		OK,
		NOK
	};

	namespace PetriUtils {
		template<class _Tp>
		class ref_wrapper {
		public:
			ref_wrapper(_Tp &t) : _ptr(std::addressof(t)) {}

			template<typename T>
			operator T() const {
				return static_cast<T>(*_ptr);
			}
			operator _Tp &() const {
				return *_ptr;
			}

		private:
			ref_wrapper(_Tp &&) = delete;
			_Tp *_ptr;
		};

		template<typename _Tp>
		inline auto wrap_ref(_Tp &ref) {
			return ref_wrapper<_Tp>(ref);
		}

		struct indirect {
			template<class _Tp>
			inline constexpr auto operator()(_Tp&& x) const {
				return *std::forward<_Tp>(x);
			}
		};
		struct addressof {
			template<class _Tp>
			inline constexpr auto operator()(_Tp&& x) const {
				return &std::forward<_Tp>(x);
			}
		};
		struct preincr {
			template<class _Tp>
			inline constexpr auto &operator()(_Tp&& x) const {
				return ++std::forward<_Tp>(x);
			}
		};
		struct predecr {
			template<class _Tp>
			inline constexpr auto &operator()(_Tp&& x) const {
				return --std::forward<_Tp>(x);
			}
		};
		struct postincr {
			template<class _Tp>
			inline constexpr auto operator()(_Tp&& x) const {
				return std::forward<_Tp>(x)++;
			}
		};
		struct postdecr {
			template<class _Tp>
			inline constexpr auto operator()(_Tp&& x) const {
				return std::forward<_Tp>(x)--;
			}
		};
		struct shift_left {
			template<class _T1, class _T2>
			inline constexpr auto operator()(_T1&& t, _T2&& u) const {
				return std::forward<_T1>(t) << std::forward<_T2>(u);
			}
		};
		struct shift_right {
			template<class _T1, class _T2>
			inline constexpr auto operator()(_T1&& t, _T2&& u) const {
				return std::forward<_T1>(t) >> std::forward<_T2>(u);
			}
		};
		struct identity {
			template<class _Tp>
			inline constexpr auto operator()(_Tp&& x) const {
				return std::forward<_Tp>(x);
			}
		};
		struct assign {
			template<class _T1, class _T2>
			inline constexpr auto operator()(ref_wrapper<_T1> t, _T2&& u) const {
				return std::forward<_T1 &>(t) = u;
			}
		};

		template<typename _ActionResult>
		_ActionResult pause(std::chrono::nanoseconds const &delay) {
			std::this_thread::sleep_for(delay);
			return {};
		}
		
		template<typename _ActionResult>
		inline _ActionResult printAction(std::string const &name, std::uint64_t id) {
			std::cout << "Action " << name << ", ID " << id << " completed." << std::endl;
			return _ActionResult{};
		}

		template<typename _ActionResult>
		inline _ActionResult doNothing() {
			return {};
		}
	}

}

#endif
