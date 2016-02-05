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
    public class DebugServer : CInterop
    {
        /**
         * Returns the DebugServer API's version
         * @return The current version of the API.
         */
        public static string Version {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.DebugServer.PetriDebugServer_getVersion());
            }
        }

        /**
         * Creates the DebugServer and binds it to the provided dynamic library.
         * @param petri The dynamic lib from which the debug server operates.
         */
        public DebugServer(DynamicLib lib)
        {
            Handle = Interop.DebugServer.PetriDebugServer_create(lib.Handle);
        }

        /**
         * Destroys the debug server. If the server is running, a deleted or out of scope
         * DebugServer
         * object will wait for the connected client to end the debug session to continue the
         * program exectution.
         */
        protected override void Clean()
        {
            Interop.DebugServer.PetriDebugServer_destroy(Handle);
        }

        /**
         * Starts the debug server by listening on the debug port of the bound dynamic library,
         * making it ready to receive a debugger connection.
         */
        public void Start()
        {
            Interop.DebugServer.PetriDebugServer_start(Handle);
        }

        /**
         * Stops the debug server. After that, the debugging port is unbound.
         */
        public void Stop()
        {
            Interop.DebugServer.PetriDebugServer_stop(Handle);
        }

        /**
         * Checks whether the debug server is running or not.
         * @return true if the server is running, false otherwise.
         */
        public bool Running()
        {
            return Interop.DebugServer.PetriDebugServer_isRunning(Handle);
        }

        public PetriDebug CurrentPetriNet {
            get {
                IntPtr handle = Interop.DebugServer.PetriDebugServer_currentPetriNet(Handle);
                if(handle != IntPtr.Zero) {
                    var pn = new PetriDebug(handle);
                    pn.Release();
                    return pn;
                }

                return null;
            }
        }
    }
}

