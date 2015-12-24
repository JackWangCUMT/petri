/*
 * Copyright (c) 2015 Rémi Saurel
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
using Regex = System.Text.RegularExpressions.Regex;
using System.Linq;

namespace Petri
{
    namespace Cpp
    {
        public class Duration
        {
            public Duration(string s)
            {
                this.Value = s;
            }

            public string Value {
                get {
                    return _value;
                }
                set {
                    Regex regex = new Regex(@"^[0-9]*(\.[0-9]+)?(ns|us|ms|s)");
                    var match = regex.Match(value);
                    if(!match.Success) {
                        throw new ArgumentException(Configuration.GetLocalized("Invalid timeout duration."));
                    }
                    else {
                        this._value = value;
                    }

                }
            }

            private string _value;
        }
    }
}

