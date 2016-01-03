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
using System.Linq;

namespace Petri.Editor
{
    namespace Cpp
    {
        public enum CVQualifier
        {
            None = 0,
            Const = 1,
            Volatile = 2}

        ;

        public class Type : IEquatable<Type>, IEquatable<string>
        {
            public static Type UnknownType {
                get {
                    return _unknownType ?? (_unknownType = new Type("UnknownType", Scope.MakeFromNamespace("Petri")));
                }
            }

            public Type(string name, Scope enclosing = null)
            {
                String s = name.Clone() as string;
                s.Replace("*", " * ");
                s = s.TrimSpaces();

                s = s.TrimStart(new char[]{ ' ' }).TrimEnd(new char[]{ ' ' });

                if(s.EndsWith("&")) {
                    _isReference = true;
                    s = s.Substring(0, s.Length - 1);
                }

                CVQualifier nameQualifier = CVQualifier.None;

                // Parse right-associative cv-qualifiers, as in const int
                if(s.StartsWith("const volatile ") || s.StartsWith("volatile const ")) {
                    s = s.Substring("const volatile ".Length);
                    nameQualifier = CVQualifier.Const | CVQualifier.Volatile;
                }
                else if(s.StartsWith("const ")) {
                    s = s.Substring("const ".Length);
                    nameQualifier = CVQualifier.Const;
                }
                else if(s.StartsWith("volatile ")) {
                    s = s.Substring("volatile ".Length);
                    nameQualifier = CVQualifier.Volatile;
                }

                var q = new List<CVQualifier>();

                s = this.ParseLeftAssociativeQualifiers(s, q).TrimEnd(new char[]{ ' ' });

                if(s.EndsWith(" const volatile") || s.EndsWith(" volatile const")) {
                    s = s.Substring(0, s.Length - " const volatile".Length);
                    nameQualifier = CVQualifier.Const | CVQualifier.Volatile;
                }
                else if(s.EndsWith(" const")) {
                    s = s.Substring(0, s.Length - " const".Length);
                    nameQualifier |= CVQualifier.Const;
                }
                else if(s.EndsWith(" volatile")) {
                    s = s.Substring(0, s.Length - " volatile".Length);
                    nameQualifier |= CVQualifier.Volatile;
                }

                q.Insert(0, nameQualifier);

                int index = s.IndexOf('<');
                if(index != -1) {
                    /*var t = Parser.SyntacticSplit(s.Substring(index + 1, s.Length - index - 2));
					foreach(var e in t) {
						template.Add(Expression.CreateFromString<Expression>(e, notnullplease, new List<Function>()));
					}*/
                    _template = s.Substring(index + 1, s.Length - index - 2);
                }
                else {
                    _template = "";
                    index = s.Length;
                }

                var tup = Parser.ExtractScope(s.Substring(0, index).Trim());
                _name = tup.Item2;
                Enclosing = Scope.MakeFromScopes(enclosing, tup.Item1);

                _cvQualifiers = q.ToArray();
            }

            public override string ToString()
            {
                string val = (Enclosing?.ToString() ?? "") + Name;
                if(Template.Length > 0) {
                    /*string v = "";
					foreach(var e in Template) {
						v += e.MakeUserReadable();
					}
					val += "<" + val + ">";*/
                    val += "<" + _template + ">";
                }

                for(int i = 0; i < _cvQualifiers.Length; ++i) {
                    if(i != 0)
                        val += " *";

                    if((_cvQualifiers[i] | CVQualifier.Volatile) == _cvQualifiers[i])
                        val += " volatile";
                    if((_cvQualifiers[i] | CVQualifier.Const) == _cvQualifiers[i])
                        val += " const";
                }

                if(this.IsReference)
                    val += " &";

                return val;
            }

            public string Name {
                get {
                    return _name;
                }
            }

            public Scope Enclosing {
                get;
                private set;
            }

            public /*List<Expression>*/string Template {
                get {
                    return _template;
                }
            }

            public IEnumerable<CVQualifier> CVQualifiers {
                get {
                    return _cvQualifiers;
                }
            }

            public bool IsReference {
                get {
                    return _isReference;
                }
            }

            public bool Equals(Type type)
            {
                if(Name != type.Name || Template != type.Template || IsReference != type.IsReference)
                    return false;

                if(_cvQualifiers.Length != type._cvQualifiers.Length)
                    return false;

                for(int i = 0; i < _cvQualifiers.Length; ++i) {
                    if(_cvQualifiers[i] != type._cvQualifiers[i])
                        return false;
                }

                bool enclosingNull = Enclosing == null || type.Enclosing == null;
                if(enclosingNull && Enclosing != type.Enclosing) {
                    return false;
                }
                if(!enclosingNull && !Enclosing.Equals(type.Enclosing)) {
                    return false;
                }
                    

                return true;
            }

            public bool Equals(string type)
            {
                return this.Equals(new Type(type, Enclosing));
            }

            // Is var1 = var2 a valid expression, where var1's type is 'type' and var2's type is 'this' ?
            public bool ConvertibleTo(Type type)
            {
                if(this.Name != type.Name || this.Template != this.Template || this._cvQualifiers.Length != type._cvQualifiers.Length)
                    return false;

                int cvCount = this._cvQualifiers.Length;

                int lastTest = (!type.IsReference) ? this._cvQualifiers.Length - 1 : this._cvQualifiers.Length;
                for(int i = 0; i < lastTest; ++i) {
                    if(!ConvertibleTo(this._cvQualifiers[i], type._cvQualifiers[i]))
                        return false;
                }

                return true;
            }

            // Is var1 = var2 a valid expression, where var1's type is 'this' and var2's type is 'type' ?
            public bool AssignableFrom(Type type)
            {
                return type.ConvertibleTo(this);
            }

            public static bool ConvertibleTo(CVQualifier sourceCV, CVQualifier destCV)
            {
                if((sourceCV | CVQualifier.Const) == sourceCV && (destCV | CVQualifier.Const) != destCV)
                    return false;
                if((sourceCV | CVQualifier.Volatile) == sourceCV && (destCV | CVQualifier.Volatile) != destCV)
                    return false;

                return true;
            }

            public static string RegexPattern {
                get {
                    // Matches const and volatile qualifiers as well as pointer and reference specifiers
                    string constVolatilePattern = @"(((const|volatile))*[ ]*[&\* ?]*)*";

                    // Matches the cv-qualifiers, the potential virtual, the type name, its template specialization, again cv-qualifiers (may be found before or after the type…)
                    string typePattern = @"[ ]*(?<type>(" + constVolatilePattern + @"(virtual[ ]+)?[:a-zA-Z_][:a-zA-Z0-9_]*" + Parser.TemplatePattern + constVolatilePattern + @"))";
                    return typePattern;
                }
            }

            string ParseLeftAssociativeQualifiers(string s, List<CVQualifier> qualifiers)
            {
                s = s.TrimEnd(new char[]{ ' ' });

                if(s.EndsWith(" * const volatile") || s.EndsWith(" * volatile const")) {
                    s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * const volatile".Length),
                                                       qualifiers);
                    qualifiers.Add(CVQualifier.Const | CVQualifier.Volatile);
                }
                else if(s.EndsWith(" * const")) {
                    s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * const".Length), qualifiers);
                    qualifiers.Add(CVQualifier.Const);
                }
                else if(s.EndsWith(" * volatile")) {
                    s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * volatile".Length), qualifiers);
                    qualifiers.Add(CVQualifier.Volatile);
                }
                else if(s.EndsWith(" *")) {
                    s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " *".Length), qualifiers);
                    qualifiers.Add(CVQualifier.None);
                }

                return s;
            }

            CVQualifier[] _cvQualifiers;
            bool _isReference;
            string _name;
            //List<Expression> _template;
            string _template;
            static Type _unknownType;
        }
    }
}

