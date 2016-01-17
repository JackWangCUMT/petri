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

namespace Petri.Editor
{
    namespace Cpp
    {
        public class Param
        {
            public Param(Type type, string name)
            {
                this.Type = type;
                this.Name = name.Trim();
            }

            public Type Type { get; private set; }

            public string Name { get; private set; }
        }

        public class Function
        {
            // With 'enclosing', you can create a static method, or a namespace-enclosed function
            public Function(Type returnType, Scope enclosing, string name, bool template)
            {
                this.ReturnType = returnType;
                this.Name = name.Trim();

                Parameters = new List<Param>();
                this.Enclosing = enclosing;

                Template = template;
            }

            public Type ReturnType {
                get;
                private set;
            }

            public string Name {
                get;
                private set;
            }

            public bool Template {
                get;
                private set;
            }

            public string TemplateArguments {
                get;
                set;
            }

            public string QualifiedName {
                get {
                    string qn = "";
                    if(Enclosing != null)
                        qn = Enclosing.ToString();
					
                    return qn + Name;
                }
            }

            public Scope Enclosing {
                get;
                private set;
            }

            public string Header {
                get;
                set;
            }

            public List<Param> Parameters {
                get;
                private set;
            }

            public void AddParam(Param p)
            {
                Parameters.Add(p);
            }

            public virtual string Signature {
                get {
                    string s = this.QualifiedName + "(";
                    foreach(var p in Parameters) {
                        s += p.Type + " " + p.Name + ", ";
                    }
                    if(s.EndsWith(", ")) {
                        s = s.Substring(0, s.Length - 2);
                    }
                    s += ")";

                    return s;
                }
            }

            public string Prototype {
                get {
                    return ReturnType.ToString() + " " + Signature;
                }
            }

            public override string ToString()
            {
                return Prototype;
            }

            public static string ParameterPattern {
                get {
                    return @"\((?<parameters>([^)]*\))*)";
                }
            }

            public static string StaticPattern {
                get {
                    return "((?<static>(static))?)";
                }
            }

            public static string InlinePattern {
                get {
                    return "((inline)?)";
                }
            }

            public static string StaticInlinePattern {
                get {
                    return "(((" + StaticPattern + " ?" + InlinePattern + ")|(" + InlinePattern + " ?" + StaticPattern + "))?)";
                }
            }

            public static string VisibilityPattern {
                get {
                    return "((?<visibility>(public|protected|private|internal))?)";
                }
            }

            public static string QualifiersPattern {
                get {
                    return "(?<attributes>(const|volatile|override|final|nothrow|= ?(default|0|delete)| )*)";
                }
            }

            public static string FunctionPattern {
                get {
                    return @"^" + VisibilityPattern + " ?" + Function.StaticInlinePattern + " ?" + Type.RegexPattern + @" ?" + Parser.NamePattern + " ?" + Function.ParameterPattern + " ?" + Function.QualifiersPattern;// + Parser.DeclarationEndPattern;
                }
            }

            public static System.Text.RegularExpressions.Regex Regex {
                get {
                    return new System.Text.RegularExpressions.Regex(FunctionPattern);
                }
            }
        }

        public class Method : Function
        {
            public Method(Type classType,
                          Type returnType,
                          string name,
                          string qualifiers,
                          bool template) : base(returnType,
                                                Scope.MakeFromClass(classType),
                                                name,
                                                template)
            {
                Qualifiers = qualifiers;
                Class = classType;
            }

            public string Qualifiers {
                get;
                private set;
            }

            public Type Class {
                get;
                private set;
            }

            public override string Signature {
                get {
                    string sig = base.Signature;
                    return sig + (this.Qualifiers.Length > 0 ? " " + this.Qualifiers : "");
                }
            }
        }
    }
}

