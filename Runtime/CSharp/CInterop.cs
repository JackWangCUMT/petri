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
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Petri.Generated")]

namespace Petri.Runtime
{
    public class CInterop : MarshalByRefObject
    {
        internal CInterop()
        {
        }

        internal IntPtr Handle {
            get;
            set;
        }

        /// <summary>
        /// Release this instance. Call at your own risk, as accessing one of the object's methods afterwards will probably segfault.
        /// The destructor however will be invoked without risk.
        /// </summary>
        public IntPtr Release()
        {
            var h = Handle;
            Handle = IntPtr.Zero;
            return h;
        }
    }
}

