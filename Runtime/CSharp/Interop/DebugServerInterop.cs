// This source file has been generated automatically from ../../C/DebugServer.h by C2CS.sh. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

    public class DebugServer {
        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDebugServer_getVersion();

        [DllImport("PetriRuntime")]
        public static extern IntPtr PetriDebugServer_create(IntPtr petri);

        [DllImport("PetriRuntime")]
        public static extern void PetriDebugServer_destroy(IntPtr server);

        [DllImport("PetriRuntime")]
        public static extern void PetriDebugServer_start(IntPtr server);

        [DllImport("PetriRuntime")]
        public static extern void PetriDebugServer_stop(IntPtr server);

        [DllImport("PetriRuntime")]
        public static extern bool PetriDebugServer_isRunning(IntPtr server);
    }
}

