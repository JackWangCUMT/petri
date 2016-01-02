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
using System.IO;

namespace Petri.Test
{
    public class Utility
    {
        public static TResult InvokeAndRedirectOutput<TResult>(System.Func<TResult> function,
                                                               out string stdout,
                                                               out string stderr)
        {
            TResult result;

            var previousOut = Console.Out;
            var previousErr = Console.Error;

            using(StringWriter sout = new StringWriter(), serr = new StringWriter()) {
                Console.SetOut(sout);
                Console.SetError(serr);

                result = function();

                stdout = sout.ToString();
                stderr = serr.ToString();
            }

            Console.SetOut(previousOut);
            Console.SetError(previousErr);

            return result;
        }

        public static void InvokeAndRedirectOutput(System.Action function, out string stdout, out string stderr)
        {
            InvokeAndRedirectOutput(() => {
                function();
                return 0;
            }, out stdout, out stderr);
        }
    }
}

