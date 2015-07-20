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
//  Common.h
//  Pétri
//
//  Created by Rémi on 15/04/2015.
//

#ifndef Petri_Common_h
#define Petri_Common_h

#include <string>
#include <cstdint>
#include "C/Types.h"
#include <list>

namespace Petri {
	
	void setThreadName(char const *name);
	void setThreadName(std::string const &name);

	using actionResult_t = Petri_actionResult_t;

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

		/**
		 * Adds a variable to the entity's associated ones.
		 * @param id The new variable to add.
		 */
		void addVariable(std::uint_fast32_t id) {
			_vars.push_back(id);
		}

		/**
		 * Returns a list of the associated Atomic variables' IDs.
		 * @return The list of variabels of the entity.
		 */
		std::list<uint_fast32_t> const &getVariables() const {
			return _vars;
		}

		/**
		 * Returns a list of the associated Atomic variables' IDs.
		 * @return The list of variabels of the entity.
		 */
		std::list<uint_fast32_t> &getVariables() {
			return _vars;
		}

	private:
		T _id;
		std::list<std::uint_fast32_t> _vars;
	};

}


#endif
