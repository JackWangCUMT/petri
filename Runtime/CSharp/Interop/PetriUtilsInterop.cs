// This source file has been generated automatically from ../../C/PetriUtils.h by C2CS.sh. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

    public class PetriUtils {
        [DllImport("PetriRuntime")]
        public static extern Int32 PetriUtility_pause(UInt64 usdelay);

        [DllImport("PetriRuntime")]
        public static extern Int32 PetriUtility_printAction([MarshalAs(UnmanagedType.LPTStr)] string name, UInt64 id);

        [DllImport("PetriRuntime")]
        public static extern Int32 PetriUtility_doNothing();

        [DllImport("PetriRuntime")]
        public static extern Int32 PetriUtility_returnDefault();

        [DllImport("PetriRuntime")]
        public static extern Int64 PetriUtility_random(Int64 lowerBound, Int64 upperBound);

        [DllImport("PetriRuntime")]
        public static extern bool PetriUtility_returnTrue(Int32 res);

        [DllImport("PetriRuntime")]
        public static extern IntPtr Petri_loadPetriDynamicLib([MarshalAs(UnmanagedType.LPTStr)] string path, [MarshalAs(UnmanagedType.LPTStr)] string prefix);
    }
}

