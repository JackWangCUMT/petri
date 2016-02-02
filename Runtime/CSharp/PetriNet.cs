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

using System;
using System.Collections.Generic;

namespace Petri.Runtime
{
    public class PetriNet : CInterop
    {
        internal PetriNet(IntPtr handle)
        {
            Handle = handle;
            if(Handle == IntPtr.Zero) {
                throw new Exception("The petri net could not be loaded!");
            }
        }

        /**
         * Creates the PetriNet, assigning it a name which serves debug purposes (see ThreadPool constructor).
         * @param name the name to assign to the PetriNet or a designated one if left empty
         */
        public PetriNet(string name = "")
        {
            Handle = Interop.PetriNet.PetriNet_create(name);
        }

        ~PetriNet()
        {
            Interop.PetriNet.PetriNet_destroy(Handle);
        }

        /**
         * Adds an Action to the PetriNet. The net must not be running yet.
         * @param action The action to add
         * @param active Controls whether the action is active as soon as the net is started or not
         */
        public virtual void AddAction(Action action, bool active = false)
        {
            Interop.PetriNet.PetriNet_addAction(Handle, action.Handle, active);
        }

        /**
         * Checks whether the net is running.
         * @return true means that the net has been started, and we can not add any more action to it now.
         */
        public bool IsRunning {
            get {
                return Interop.PetriNet.PetriNet_isRunning(Handle);
            }
        }

        /**
         * Starts the Petri net. It must not be already running. If no states are initially active, this is a no-op.
         */
        public virtual void Run()
        {
            Interop.PetriNet.PetriNet_run(Handle);
        }

        /**
         * Stops the Petri net. It blocks the calling thread until all running states are finished,
         * but do not allows new states to be enabled. If the net is not running, this is a no-op.
         */
        public virtual void Stop()
        {
            Interop.PetriNet.PetriNet_stop(Handle);
        }

        /**
         * Blocks the calling thread until the Petri net has completed its whole execution.
         */
        public virtual void Join()
        {
            Interop.PetriNet.PetriNet_join(Handle);
        }

        /**
         * Adds an Atomic variable designated by the specified id.
         * @param id the id of the new Atomic variable
         */
        public void AddVariable(UInt32 id)
        {
            Interop.PetriNet.PetriNet_addVariable(Handle, id);
        }

        /**
         * Gets an atomic variable previously added to the Petri net. Trying to retrieve a non existing variable will throw an exception.
         * @param the id of the Atomic to retrieve.
         */
        public IntPtr GetVariable(UInt32 id)
        {
            return Interop.PetriNet.PetriNet_getVariable(Handle, id);
        }

        public string Name {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.PetriNet.PetriNet_getName(Handle));
            }
        }
    }
}

