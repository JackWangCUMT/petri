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
//  Atomic.h
//  Pétri
//
//  Created by Rémi on 28/04/2015.
//

#ifndef Petri_Atomic_h
#define Petri_Atomic_h

#include <mutex>
#include "PetriUtils.h"

namespace Petri {

	class Atomic {
	public:
		Atomic() : _value(0), _lock(_mutex, std::defer_lock) {

		}

		auto &value() {
			return _value;
		}

		auto getLock() {
			return std::unique_lock<std::mutex>{_mutex, std::defer_lock};
		}

	private:
		std::int64_t _value;
		std::unique_lock<std::mutex> _lock;
		std::mutex _mutex;
	};
	
}


#endif
