// This source file has been generated automatically. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

public class Transition {
[DllImport("PetriRuntime")]
public static extern IntPtr PetriTransition_createEmpty(IntPtr previous, IntPtr next);

[DllImport("PetriRuntime")]
public static extern IntPtr PetriTransition_create(UInt64 id, [MarshalAs(UnmanagedType.LPTStr)] string name, IntPtr previous, IntPtr next, TransitionCallableDel cond);

[DllImport("PetriRuntime")]
public static extern void PetriTransition_destroy(IntPtr transition);

[DllImport("PetriRuntime")]
public static extern UInt64 PetriTransition_getID(IntPtr transition);

[DllImport("PetriRuntime")]
public static extern void PetriTransition_setID(IntPtr transition, UInt64 id);

[DllImport("PetriRuntime")]
public static extern bool PetriTransition_isFulfilled(IntPtr transition, Int32 actionResult);

[DllImport("PetriRuntime")]
public static extern void PetriTransition_setCondition(IntPtr transition, TransitionCallableDel test);

[DllImport("PetriRuntime")]
public static extern IntPtr PetriTransition_getName(IntPtr transition);

[DllImport("PetriRuntime")]
public static extern void PetriTransition_setName(IntPtr transition, [MarshalAs(UnmanagedType.LPTStr)] string name);

[DllImport("PetriRuntime")]
public static extern UInt64 PetriTransition_getDelayBetweenEvaluation(IntPtr transition);

[DllImport("PetriRuntime")]
public static extern void PetriTransition_setDelayBetweenEvaluation(IntPtr transition, UInt64 usDelay);

}
}

