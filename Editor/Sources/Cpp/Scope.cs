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

namespace Petri
{
    namespace Cpp
    {
        public class Scope : IEquatable<Scope>
        {
            public static Scope MakeFromNamespace(string ns, Scope enclosing = null)
            {
                Scope s = new Scope();
                s.Class = null;
                s.Namespace = ns;
                s.Enclosing = enclosing; 

                return s;
            }

            public static Scope MakeFromClass(Type classType)
            {
                Scope s = new Scope();
                s.Class = classType;
                s.Namespace = null;
                s.Enclosing = classType.Enclosing; 

                return s;
            }

            public static Scope MakeFromScopes(Scope enclosing, Scope inner)
            {
                if(inner != null) {
                    Scope i = inner;
                    while(true) {
                        if(i.Enclosing != null) {
                            i = i.Enclosing;
                        }
                        else {
                            i.Enclosing = enclosing;
                            break;
                        }
                    }

                    return inner;
                }
                else {
                    return enclosing;
                }
            }

            public bool IsClass {
                get {
                    return Class != null;
                }
            }

            public bool IsNamespace {
                get {
                    return Namespace != null;
                }
            }

            public Type Class {
                get;
                private set;
            }

            public string Namespace {
                get;
                private set;
            }

            public override string ToString()
            {
                string enclosing = "";
                if(Enclosing != null)
                    enclosing = Enclosing.ToString();

                if(IsNamespace)
                    return enclosing + (Namespace.Length > 0 ? Namespace + "::" : "");
                else
                    return enclosing + Class.ToString() + "::";
            }

            public Scope Enclosing {
                get;
                private set;
            }

            public bool Equals(Scope s)
            {
                return ToString() == s.ToString();
            }
        }
    }
}

