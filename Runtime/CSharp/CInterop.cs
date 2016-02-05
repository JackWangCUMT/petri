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

namespace Petri.Runtime
{
    public abstract class CInterop : MarshalByRefObject
    {
        internal CInterop()
        {
            Owning = true;
        }

        internal IntPtr Handle {
            get;
            set;
        }

        ~CInterop() {
            if(Owning) {
                Clean();
            }
        }

        /// <summary>
        /// Release the native handle.
        /// </summary>
        protected abstract void Clean();

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Runtime.CInterop"/> is owning the native handle.
        /// </summary>
        /// <value><c>true</c> if owning; otherwise, <c>false</c>.</value>
        public bool Owning {
            get;
            protected set;
        }

        /// <summary>
        /// Release this instance. The instance can still be used afterwards, but the destructor will not clean the native handle upon call.
        /// </summary>
        public IntPtr Release()
        {
            Owning = false;
            return Handle;
        }
    }
}

