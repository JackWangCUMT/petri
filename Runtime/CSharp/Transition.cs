using System;

namespace Petri.Runtime
{
    /**
     * A transition linking 2 Action, composing a PetriNet.
     */
    public class Transition : Entity
    {
        public Transition(IntPtr handle)
        {
            Handle = handle;
        }

        /**
         * Creates an Transition object, containing a nullptr test, allowing the end of execution of Action 'previous' to provoke
         * the execution of Action 'next', if the test is fulfilled.
         * @param previous The starting point of the Transition
         * @param next The arrival point of the Transition
         */
        public Transition(Action previous, Action next)
        {
            Handle = Interop.Transition.PetriTransition_createEmpty(previous.Handle, next.Handle);
        }

        /**
         * Creates an Transition object, containing a nullptr test, allowing the end of execution of Action 'previous' to provoke
         * the execution of Action 'next', if the test is fulfilled.
         * @param previous The starting point of the Transition
         * @param next The arrival point of the Transition
         */
        public Transition(UInt64 id, string name, Action previous, Action next, TransitionCallable cond)
        {
            var c = WrapForNative.Wrap(cond, name);
            _callback = c;
            Handle = Interop.Transition.PetriTransition_create(id, name, previous.Handle, next.Handle, c);
        }

        ~Transition() {
            Interop.Transition.PetriTransition_destroy(Handle);
        }

        /**
         * Checks whether the Transition can be crossed
         * @param actionResult The result of the Action 'previous'. This is useful when the Transition's test uses this value.
         * @return The result of the test, true meaning that the Transition can be crossed to enable the action 'next'
         */
        public bool IsFulfilled(ActionResult_t actionResult)
        {
            return Interop.Transition.PetriTransition_isFulfilled(Handle, actionResult);
        }

        /**
         * Returns the condition associated to the Transition
         * @return The condition associated to the Transition
         */
        public void SetCondition(TransitionCallable condition)
        {
            var c = WrapForNative.Wrap(condition, Name);
            _callback = c;
            Interop.Transition.PetriTransition_setCondition(Handle, c);
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
                Interop.Transition.PetriTransition_setDelayBetweenEvaluation(Handle, (UInt64)(value * 1.0e6));
            }
        }

        // Ensures the callback's lifetime is the same as the instance's one to avoid unexpected GC during native code invocation.
        private ManagedCallback _callback;
    }
}

