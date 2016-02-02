// This source file has been generated automatically from ../../C/Transition.h by C2CS.sh. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

    public class Transition {
        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_destroy(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriTransition_getPrevious(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriTransition_getNext(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern UInt64 PetriTransition_getID(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_setID(IntPtr transition, UInt64 id);

        [DllImport("PetriRuntime")]
        public static extern bool PetriTransition_isFulfilled(IntPtr transition, IntPtr petriNet, Int32 actionResult);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_setCondition(IntPtr transition, TransitionCallableDel test);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_setConditionWithParam(IntPtr transition, ParametrizedTransitionCallableDel test);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriTransition_getName(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_setName(IntPtr transition, [MarshalAs(UnmanagedType.LPTStr)] string name);

        [DllImport("PetriRuntime")]
        public static extern UInt64 PetriTransition_getDelayBetweenEvaluation(IntPtr transition);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_setDelayBetweenEvaluation(IntPtr transition, UInt64 usDelay);

        [DllImport("PetriRuntime")]
        public static extern void PetriTransition_addVariable(IntPtr transition, UInt32 id);
    }
}

