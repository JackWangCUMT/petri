/*
 * Copyright (c) 2016 Rémi Saurel
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
//  Transition.hpp
//  Petri
//
//  Created by Rémi on 30/01/2016.
//

#ifndef PETRI_Transition_hpp
#define PETRI_Transition_hpp

#include "../Transition.h"
#include "Types.hpp"

namespace {
    auto getParametrizedTransitionCallable(parametrizedTransitionCallable_t transition) {
        return Petri::make_param_transition_callable([transition](Petri::PetriNet &pn, Petri_actionResult_t a) {
            PetriNet petriNet{std::unique_ptr<Petri::PetriNet>(&pn)};

            auto result = transition(&petriNet, a);

            petriNet.petriNet.release();

            return result;
        });
    }
}


#endif /* Transition_h */
