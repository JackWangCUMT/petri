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
    /**
     * A state composing a PetriNet.
     */
    public class Action : CInterop
    {
        internal Action(IntPtr handle)
        {
            Handle = handle;
        }

        /**
         * Creates an empty action, associated to a null CallablePtr.
         */
        public Action()
        {
            Handle = Interop.Action.PetriAction_createEmpty();
        }

        /**
         * Creates an empty action, associated to a copy of the specified Callable.
         * @param id The ID of the new action.
         * @param name The name of the new action.
         * @param action The Callable which will be called when the action is run.
         * @param requiredTokens The number of tokens that must be inside the active action for it to execute.
         */
        public Action(UInt64 id, string name, ActionCallableDel action, UInt64 requiredTokens)
        {
            var c = WrapForNative.Wrap(action, name);
            _callback = c;
            Handle = Interop.Action.PetriAction_create(id, name, c, requiredTokens);
        }

        /**
         * Creates an empty action, associated to a copy of the specified Callable.
         * @param id The ID of the new action.
         * @param name The name of the new action.
         * @param action The Callable which will be called when the action is run.
         * @param requiredTokens The number of tokens that must be inside the active action for it to execute.
         */
        public Action(UInt64 id,
                      string name,
                      ParametrizedActionCallableDel action,
                      UInt64 requiredTokens)
        {
            var c = WrapForNative.Wrap(action, name);
            _parametrizedCallback = action;
            Handle = Interop.Action.PetriAction_createWithParam(id, name, c, requiredTokens);
        }

        ~Action()
        {
            Interop.Action.PetriAction_destroy(Handle);
        }

        Transition AddTransition(Action next)
        {
            IntPtr handle = Interop.Action.PetriAction_addEmptyTransition(Handle, next.Handle);
            return new Transition(handle);
        }

        /**
         * Adds a Transition to the Action.
         * @param id the id of the Transition
         * @param name the name of the transition to be added
         * @param next the Action following the transition to be added
         * @param cond the condition of the Transition to be added
         * @return The newly created transition.
         */
        public Transition AddTransition(UInt64 id,
                                        string name,
                                        Action next,
                                        TransitionCallableDel cond)
        {
            var handle = Interop.Action.PetriAction_addTransition(Handle,
                                                                  id,
                                                                  name,
                                                                  next.Handle,
                                                                  cond);
            return new Transition(handle);
        }

        public Transition AddTransition(UInt64 id,
                                        string name,
                                        Action next,
                                        ParametrizedTransitionCallableDel cond)
        {
            var handle = Interop.Action.PetriAction_addTransitionWithParam(Handle,
                                                                           id,
                                                                           name,
                                                                           next.Handle,
                                                                           cond);
            return new Transition(handle);
        }

        /**
         * Changes the Callable associated to the Action
         * @param action The Callable which will be copied and put in the Action
         */
        public void SetAction(ActionCallableDel action)
        {
            var c = WrapForNative.Wrap(action, Name);
            _callback = c;
            Interop.Action.PetriAction_setAction(Handle, c);
        }

        /**
         * Changes the Callable associated to the Action
         * @param action The Callable which will be copied and put in the Action
         */
        public void SetAction(ParametrizedActionCallableDel action)
        {
            var c = WrapForNative.Wrap(action, Name);
            _parametrizedCallback = c;
            Interop.Action.PetriAction_setActionParam(Handle, c);
        }

        /**
         * Returns the required tokens of the Action to be activated, i.e. the count of Actions which must lead to *this and terminate for *this to activate.
         * @return The required tokens of the Action
         */
        public UInt64 RequiredTokens {
            get {
                return Interop.Action.PetriAction_getRequiredTokens(Handle);
            }
            set {
                Interop.Action.PetriAction_setRequiredTokens(Handle, value);
            }
        }

        /**
         * Gets the current tokens count given to the Action by its preceding Actions.
         * @return The current tokens count of the Action
         */
        public UInt64 CurrentTokens {
            get {
                return Interop.Action.PetriAction_getCurrentTokens(Handle);
            }
        }

        public string Name {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.Action.PetriAction_getName(Handle));
            }
            set {
                Interop.Action.PetriAction_setName(Handle, value);
            }
        }

        public UInt64 ID {
            get {
                return Interop.Action.PetriAction_getID(Handle);
            }
            set {
                Interop.Action.PetriAction_setID(Handle, value);
            }
        }

        public void AddVariable(UInt32 id)
        {
            Interop.Action.PetriAction_addVariable(Handle, id);
        }

        // Ensures the callback's lifetime is the same as the instance's one to avoid unexpected GC during native code invocation.
        // The warning CS0414 states that the value is never read from, and that's true.
        // But the rationale here is to always keep a reference to the callback so that it is not GC'ed.
        #pragma warning disable 0414
        private ActionCallableDel _callback;
        private ParametrizedActionCallableDel _parametrizedCallback;
        #pragma warning restore 0414
    }
}

