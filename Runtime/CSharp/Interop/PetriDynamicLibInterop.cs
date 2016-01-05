// This source file has been generated automatically from ../../C/PetriDynamicLib.h by C2CS.sh. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

    public class PetriDynamicLib {
        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_create([MarshalAs(UnmanagedType.LPTStr)] string name, [MarshalAs(UnmanagedType.LPTStr)] string prefix, UInt16 port);

        [DllImport("PetriRuntime")]
        public static extern void PetriDynamicLib_destroy(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_createPetriNet(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_createDebugPetriNet(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_getHash(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_getName(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern UInt16 PetriDynamicLib_getPort(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern bool PetriDynamicLib_load(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_getPath(IntPtr lib);

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDynamicLib_getPrefix(IntPtr lib);
    }
}

