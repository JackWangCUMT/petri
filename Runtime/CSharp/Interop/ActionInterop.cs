// This source file has been generated automatically. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

public class Action {
[DllImport("PetriRuntime")]
public static extern IntPtr PetriAction_createEmpty();

[DllImport("PetriRuntime")]
public static extern IntPtr PetriAction_create(UInt64 id, [MarshalAs(UnmanagedType.LPTStr)] string name, ActionCallableDel action, UInt64 requiredTokens);

[DllImport("PetriRuntime")]
public static extern IntPtr PetriAction_createWithParam(UInt64 id, [MarshalAs(UnmanagedType.LPTStr)] string name, ParametrizedActionCallableDel action, UInt64 requiredTokens);

[DllImport("PetriRuntime")]
public static extern void PetriAction_destroy(IntPtr action);

[DllImport("PetriRuntime")]
public static extern UInt64 PetriAction_getID(IntPtr action);

[DllImport("PetriRuntime")]
public static extern void PetriAction_setID(IntPtr action, UInt64 id);

[DllImport("PetriRuntime")]
public static extern void PetriAction_addTransition(IntPtr action, IntPtr transition);

[DllImport("PetriRuntime")]
public static extern void PetriAction_createAndAddTransition(IntPtr action, UInt64 id, [MarshalAs(UnmanagedType.LPTStr)] string name, IntPtr next, TransitionCallableDel cond);

[DllImport("PetriRuntime")]
public static extern void PetriAction_setAction(IntPtr action, ActionCallableDel a);

[DllImport("PetriRuntime")]
public static extern void PetriAction_setActionParam(IntPtr action, ParametrizedActionCallableDel a);

[DllImport("PetriRuntime")]
public static extern UInt64 PetriAction_getRequiredTokens(IntPtr action);

[DllImport("PetriRuntime")]
public static extern void PetriAction_setRequiredTokens(IntPtr action, UInt64 requiredTokens);

[DllImport("PetriRuntime")]
public static extern UInt64 PetriAction_getCurrentTokens(IntPtr action);

[DllImport("PetriRuntime")]
public static extern IntPtr PetriAction_getName(IntPtr action);

[DllImport("PetriRuntime")]
public static extern void PetriAction_setName(IntPtr action, [MarshalAs(UnmanagedType.LPTStr)] string name);

}
}

