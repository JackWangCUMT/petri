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
using System.Runtime.InteropServices;

namespace Petri.Runtime
{
    [return: MarshalAs(UnmanagedType.LPTStr)] public delegate string StringCallableDel();
    public delegate IntPtr PtrCallableDel();
    public delegate UInt16 UInt16CallableDel();

    public delegate Int32 ActionCallableDel();
    public delegate Int32 ParametrizedActionCallableDel(PetriNet petriNet);
    public delegate bool TransitionCallableDel(Int32 result);

    public class WrapForNative
    {
        public static ActionCallableDel Wrap(ActionCallableDel callable, string actionName)
        {
            return () => {
                try {
                    return callable();
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return default(Int32);
                }
            };
        }

        public static ParametrizedActionCallableDel Wrap(ParametrizedActionCallableDel callable,
                                                         string actionName)
        {
            return (PetriNet pn) => {
                try {
                    return callable(pn);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return default(Int32);
                }
            };
        }

        public static TransitionCallableDel Wrap(TransitionCallableDel callable,
                                                 string transitionName)
        {
            return (Int32 result) => {
                try {
                    return callable(result);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The condition testing of the condition {0} failed with the exception \"{1}\"",
                                            transitionName,
                                            e.Message);
                    return default(bool);
                }
            };
        }
    }
}

