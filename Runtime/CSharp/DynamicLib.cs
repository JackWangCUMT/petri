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
    public class DynamicLib : CInterop
    {
        /**
         * Creates the dynamic library wrapper. It still needs to be loaded to make it possible to
         * create the PetriNet objects.
         */
        public DynamicLib(PtrCallableDel create,
                        PtrCallableDel createDebug,
                        StringCallableDel hash,
                        StringCallableDel name,
                        StringCallableDel prefix,
                        UInt16CallableDel port)
        {
            Handle = Interop.PetriDynamicLib.PetriDynamicLib_createWithPtr(create,
                                                                           createDebug,
                                                                           hash,
                                                                           name,
                                                                           port,
                                                                           prefix);
        }

        ~DynamicLib()
        {
            Interop.PetriDynamicLib.PetriDynamicLib_destroy(Handle);
        }

        /**
         * Creates the PetriNet object according to the code contained in the dynamic library.
         * @return The PetriNet object wrapped in a std::unique_ptr
         */
        public PetriNet Create()
        {
            return new PetriNet(Interop.PetriDynamicLib.PetriDynamicLib_createPetriNet(Handle));
        }

        /**
         * Creates the PetriDebug object according to the code contained in the dynamic library.
         * @return The PetriDebug object wrapped in a std::unique_ptr
         */
        public PetriDebug CreateDebug()
        {
            return new PetriDebug(Interop.PetriDynamicLib.PetriDynamicLib_createDebugPetriNet(Handle));
        }

        /**
         * Returns the SHA1 hash of the dynamic library. It uniquely identifies the code of the
         * PetriNet,
         * so that a different or modified petri net has a different hash print
         * @return The dynamic library hash
         */
        public string Hash {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.PetriDynamicLib.PetriDynamicLib_getHash(Handle));
            }
        }

        /**
         * Returns the name of the Petri net.
         * @return The name of the Petri net
         */
        public string Name {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.PetriDynamicLib.PetriDynamicLib_getName(Handle));
            }
        }

        /**
         * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to
         * debugger connection.
         * @return The TCP port which will be used by DebugSession
         */
        public UInt16 Port {
            get {
                return Interop.PetriDynamicLib.PetriDynamicLib_getPort(Handle);
            }
        }

        public string Prefix {
            get {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(Interop.PetriDynamicLib.PetriDynamicLib_getPrefix(Handle));
            }
        }
    };
}

