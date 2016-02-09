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

namespace Petri.Runtime
{
    /**
     * A transition linking 2 Action, composing a PetriNet.
     */
    public class Transition : CInterop
    {
        internal Transition(IntPtr handle, TransitionCallableDel del)
        {
            Handle = handle;
            _callback = del;
        }

        internal Transition(IntPtr handle, ParametrizedTransitionCallableDel del)
        {
            Handle = handle;
            _parametrizedCallback = del;
        }

        protected override void Clean()
        {
            Interop.Transition.PetriTransition_destroy(Handle);
        }

        /**
         * Checks whether the Transition can be crossed
         * @param actionResult The result of the Action 'previous'. This is useful when the Transition's test uses this value.
         * @return The result of the test, true meaning that the Transition can be crossed to enable the action 'next'
         */
        public bool IsFulfilled(PetriNet petriNet, Int32 actionResult)
        {
            return Interop.Transition.PetriTransition_isFulfilled(Handle, petriNet.Handle, actionResult);
        }

        /**
         * Returns the condition associated to the Transition
         * @return The condition associated to the Transition
         */
        public void SetCondition(TransitionCallableDel condition)
        {
            var c = WrapForNative.Wrap(condition, Name);
            _callback = c;
            _parametrizedCallback = null;
            Interop.Transition.PetriTransition_setCondition(Handle, c);
        }

        public void SetCondition(ParametrizedTransitionCallableDel condition)
        {
            var c = WrapForNative.Wrap(condition, Name);
            _parametrizedCallback = c;
            _callback = null;
            Interop.Transition.PetriTransition_setConditionWithParam(Handle, c);
        }

        /**
         * Gets the Action 'previous', the starting point of the Transition.
         * @return The Action 'previous', the starting point of the Transition.
         */
        public Action Previous()
        {
            return new Action(Interop.Transition.PetriTransition_getPrevious(Handle));
        }

        /**
         * Gets the Action 'next', the arrival point of the Transition.
         * @return The Action 'next', the arrival point of the Transition.
         */
        public Action Next()
        {
            return new Action(Interop.Transition.PetriTransition_getNext(Handle));
        }

        /**
         * Gets the name of the Transition.
         * @return The name of the Transition.
         */
        public string Name {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.Transition.PetriTransition_getNext(Handle));
            }
            set {
                Interop.Transition.PetriTransition_setName(Handle, value);
            }
        }

        public UInt64 ID {
            get {
                return Interop.Transition.PetriTransition_getID(Handle);
            }
            set {
                Interop.Transition.PetriTransition_setID(Handle, value);
            }
        }

        /**
         * The delay between successive evaluations of the Transition. The runtime will not try to evaluate
         * the Transition with a delay smaller than this delay after a previous evaluation, but only for one execution of Action 'previous'
         * @return The minimal delay between two evaluations of the Transition.
         */
        public double delayBetweenEvaluation {
            get {
                return Interop.Transition.PetriTransition_getDelayBetweenEvaluation(Handle) / 1.0e6;
            }
            set {
                Interop.Transition.PetriTransition_setDelayBetweenEvaluation(Handle,
                                                                             (UInt64)(value * 1.0e6));
            }
        }

        public void AddVariable(UInt32 id) {
            Interop.Transition.PetriTransition_addVariable(Handle, id);
        }

        // Ensures the callback's lifetime is the same as the instance's one to avoid unexpected GC during native code invocation.
        // The warning CS0414 states that the value is never read from, and that's true.
        // But the rationale here is to always keep a reference to the callback so that it is not GC'ed.
        #pragma warning disable 0414
        private TransitionCallableDel _callback;
        private ParametrizedTransitionCallableDel _parametrizedCallback;
        #pragma warning restore 0414
    }
}

