// This source file has been generated automatically. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

public class PetriNet {
[DllImport("PetriRuntime")]
public static extern IntPtr PetriNet_create([MarshalAs(UnmanagedType.LPTStr)] string name);

[DllImport("PetriRuntime")]
public static extern IntPtr PetriNet_createDebug([MarshalAs(UnmanagedType.LPTStr)] string name);

[DllImport("PetriRuntime")]
public static extern void PetriNet_destroy(IntPtr pn);

[DllImport("PetriRuntime")]
public static extern void PetriNet_addAction(IntPtr pn, IntPtr action, bool active);

[DllImport("PetriRuntime")]
public static extern bool PetriNet_isRunning(IntPtr pn);

[DllImport("PetriRuntime")]
public static extern void PetriNet_run(IntPtr pn);

[DllImport("PetriRuntime")]
public static extern void PetriNet_stop(IntPtr pn);

[DllImport("PetriRuntime")]
public static extern void PetriNet_join(IntPtr pn);

[DllImport("PetriRuntime")]
public static extern void PetriNet_addVariable(IntPtr pn, UInt32 id);

[DllImport("PetriRuntime")]
public static extern Int64 PetriNet_getVariable(IntPtr pn, UInt32 id);

[DllImport("PetriRuntime")]
public static extern void PetriNet_lockVariable(IntPtr pn, UInt32 id);

[DllImport("PetriRuntime")]
public static extern void PetriNet_unlockVariable(IntPtr pn, UInt32 id);

}
}

