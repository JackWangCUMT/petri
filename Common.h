//
//  Common.h
//  Pétri
//
//  Created by Rémi on 15/04/2015.
//

#ifndef Petri_Common_h
#define Petri_Common_h

#include <string>
#include <cstdint>

namespace Petri {
	
	void setThreadName(char const *name);
	void setThreadName(std::string const &name);

	using actionResult_t = std::int32_t;

	template<typename T>
	struct HasID {
	public:
		HasID(T id) : _id(id) { }

		T ID() const {
			return _id;
		}

		void setID(T id) {
			_id = id;
		}

	private:
		T _id;
	};

}


#endif
