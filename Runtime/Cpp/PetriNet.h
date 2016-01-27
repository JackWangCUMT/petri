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
//  PetriNet.h
//  Pétri
//
//  Created by Rémi on 27/06/2014.
//

#ifndef Petri_PetriNet_h
#define Petri_PetriNet_h

#include <memory>
#include <string>

namespace Petri {

    class Atomic;
    class Action;

    class PetriNet {
    public:
        /**
         * Creates the PetriNet, assigning it a name which serves debug purposes (see ThreadPool
         * constructor).
         * @param name the name to assign to the PetriNet or a designated one if left empty
         */
        PetriNet(std::string const &name = "");

        virtual ~PetriNet();

        /**
         * Adds an Action to the PetriNet. The net must not be running yet.
         * @param action The action to add
         * @param active Controls whether the action is active as soon as the net is started or not
         */
        virtual Action &addAction(Action action, bool active = false);

        /**
         * Checks whether the net is running.
         * @return true means that the net has been started, and we can not add any more action to
         * it now.
         */
        bool running() const;

        /**
         * Starts the Petri net. It must not be already running. If no states are initially active,
         * this is a no-op.
         */
        virtual void run();

        /**
         * Stops the Petri net. It blocks the calling thread until all running states are finished,
         * but do not allows new states to be enabled. If the net is not running, this is a no-op.
         */
        virtual void stop();

        /**
         * Blocks the calling thread until the Petri net has completed its whole execution.
         */
        virtual void join();

        /**
         * Adds an Atomic variable designated by the specified id.
         * @param id the id of the new Atomic variable
         */
        void addVariable(std::uint_fast32_t id);

        /**
         * Gets an atomic variable previously added to the Petri net. Trying to retrieve a non
         * existing variable will throw an exception.
         * @param id the id of the Atomic to retrieve.
         */
        Atomic &getVariable(std::uint_fast32_t id);

        std::string const &name() const;

    protected:
        struct Internals;
        PetriNet(std::unique_ptr<Internals> internals);
        std::unique_ptr<Internals> _internals;
    };
}

#endif
