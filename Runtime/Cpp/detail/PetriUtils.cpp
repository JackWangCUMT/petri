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
//  PetriUtils.cpp
//  Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "../Common.h"
#include "../PetriUtils.h"
#include <iostream>
#include <random>
#include <thread>

namespace Petri {
    void setThreadName(char const *name) {
#if __LINUX__
        pthread_setname_np(pthread_self(), name);
#elif __APPLE__
        pthread_setname_np(name);
#endif
    }

    void setThreadName(std::string const &name) {
        setThreadName(name.c_str());
    }

    namespace Utility {
        namespace {
            std::random_device _rd;

            std::default_random_engine _engine{_rd()};
        }
        actionResult_t pause(std::chrono::nanoseconds const &delay) {
            std::this_thread::sleep_for(delay);
            return {};
        }

        actionResult_t printAction(std::string const &name, std::uint64_t id) {
            std::cout << "Action " << name << ", ID " << id << " completed." << std::endl;
            return {};
        }

        actionResult_t doNothing() {
            return {};
        }

        int64_t random(int64_t lowerBound, int64_t upperBound) {
            return std::uniform_int_distribution<int64_t>{lowerBound, upperBound}(_engine);
        }
    }
}
