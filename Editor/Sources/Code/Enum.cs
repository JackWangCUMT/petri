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
using System.Text.RegularExpressions;

namespace Petri.Editor.Code
{
    public class Enum : IEquatable<Enum>
    {
        public Enum(Language language, String name, IEnumerable<string> members)
        {
            Language = language;
            Name = name;
            Members = members.ToArray();
        }

        public Enum(Language language, string commaSeparatedList)
        {
            Language = language;

            commaSeparatedList = commaSeparatedList.Replace(" ", "");
            commaSeparatedList = commaSeparatedList.Replace("\t", "");
            var lst = commaSeparatedList.Split(new char[]{ ',' }, StringSplitOptions.None);
            Regex name = new Regex(Code.Parser.NamePattern);

            bool ok = lst.Length >= 2;
            if(ok) {
                foreach(var v in lst) {
                    Match nameMatch = name.Match(v);
                    if(!nameMatch.Success || nameMatch.Value != v) {
                        ok = false;
                        break;
                    }
                }
            }

            if(!ok) {
                throw new Exception(Configuration.GetLocalized("Invalid comma separated-stored enum"));
            }

            Name = lst[0];
            Members = lst.Skip(1).ToArray();
        }

        public override string ToString()
        {
            return Name + "," + String.Join(",", Members);
        }

        public string Name {
            get;
            private set;
        }

        public Language Language {
            get;
            private set;
        }

        public Type Type {
            get {
                return new Type(Language, Name);
            }
        }

        public string[] Members {
            get;
            private set;
        }

        public bool Equals(Enum e) {
            if(Name != e.Name || Members.Length != e.Members.Length) {
                return false;
            }

            for(int i = 0; i < Members.Length; ++i) {
                if(Members[i] != e.Members[i]) {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object e)
        {
            if(e is Enum) {
                return Equals((Enum)e);
            }

            return false;
        }
    }
}

