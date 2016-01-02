using System;
using System.Collections.Generic;

namespace Petri.Runtime
{
    /**
     * A state composing a PetriNet.
     */
    public class Action : Entity
    {
        public Action(IntPtr handle)
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
        public Action(UInt64 id, string name, ParametrizedActionCallableDel action, UInt64 requiredTokens)
        {
            var c = WrapForNative.Wrap(action, name);
            _parametrizedCallback = c;
            Handle = Interop.Action.PetriAction_createWithParam(id, name, c, requiredTokens);
        }

        ~Action() {
            Interop.Action.PetriAction_destroy(Handle);
        }


        /**
         * Adds a Transition to the Action.
         * @param transition the transition to be added
         */
        public void AddTransition(Transition transition)
        {
            Interop.Action.PetriAction_addTransition(Handle, transition.Handle);
        }

        /**
         * Adds a Transition to the Action.
         * @param id the id of the Transition
         * @param name the name of the transition to be added
         * @param next the Action following the transition to be added
         * @param cond the condition of the Transition to be added
         * @return The newly created transition.
         */
        public Transition AddTransition(UInt64 id, string name, Action next, TransitionCallableDel cond)
        {
            var handle = Interop.Action.PetriAction_addNewTransition(Handle, id, name, next.Handle, cond);

            var t = new Transition(handle);

            return t;
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

        // Ensures the callback's lifetime is the same as the instance's one to avoid unexpected GC during native code invocation.
        private ActionCallableDel _callback;
        private ParametrizedActionCallableDel _parametrizedCallback;
    }
}

