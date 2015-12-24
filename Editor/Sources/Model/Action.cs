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
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace Petri
{
    public sealed class Action : NonRootState
    {
        public Action(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
        {
            this.Position = pos;
            this.Radius = 20;

            this.Active = active;

            this.Function = new Cpp.FunctionInvocation(doc.Settings.Language, DoNothingFunction(doc));
        }

        public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor)
        {
            TrySetFunction(descriptor.Attribute("Function").Value);
        }

        private void TrySetFunction(string s)
        {
            Cpp.Expression exp;
            try {
                exp = Cpp.Expression.CreateFromString<Cpp.Expression>(s, this);
                if(exp is Cpp.FunctionInvocation) {
                    var f = (Cpp.FunctionInvocation)exp;
                    if(!f.Function.ReturnType.Equals(Document.Settings.Enum.Type)) {
                        Document.Conflicting.Add(this);
                    }
                    Function = f;
                }
                else {
                    Function = new Cpp.WrapperFunctionInvocation(Document.Settings.Language, Document.Settings.Enum.Type, exp);
                }
            }
            catch(Exception) {
                Document.Conflicting.Add(this);
                Function = new Cpp.ConflictFunctionInvocation(s);
            }
        }

        public override void UpdateConflicts()
        {
            this.TrySetFunction(Function.MakeUserReadable());
        }

        public override XElement GetXml()
        {
            var elem = new XElement("Action");
            this.Serialize(elem);
            return elem;
        }

        public override void Serialize(XElement element)
        {
            base.Serialize(element);
            element.SetAttributeValue("Function", this.Function.MakeUserReadable());
        }

        public Cpp.FunctionInvocation PrintAction()
        {
            return new Petri.Cpp.FunctionInvocation(Document.Settings.Language, PrintFunction(Document), Cpp.LiteralExpression.CreateFromString("$Name", this), Cpp.LiteralExpression.CreateFromString("$ID", this));
        }

        public static Cpp.Function PrintFunction(HeadlessDocument doc)
        {
            if(doc.Settings.Language == Language.Cpp) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("Utility", Cpp.Scope.EmptyScope), "printAction", false);
                f.AddParam(new Cpp.Param(new Cpp.Type("std::string const &", Cpp.Scope.EmptyScope), "name"));
                f.AddParam(new Cpp.Param(new Cpp.Type("std::uint64_t", Cpp.Scope.EmptyScope), "id"));

                return f;
            }
            else if(doc.Settings.Language == Language.C) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.EmptyScope, "PetriUtility_printAction", false);
                f.AddParam(new Cpp.Param(new Cpp.Type("char const *", Cpp.Scope.EmptyScope), "name"));
                f.AddParam(new Cpp.Param(new Cpp.Type("uint64_t", Cpp.Scope.EmptyScope), "id"));

                return f;
            }

            throw new Exception("Should not get there !");
        }

        public static Cpp.Function DoNothingFunction(HeadlessDocument doc)
        {
            if(doc.Settings.Language == Language.Cpp) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("Utility", Cpp.Scope.EmptyScope), "doNothing", false);
                return f;
            }
            else if(doc.Settings.Language == Language.C) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.EmptyScope, "PetriUtility_doNothing", false);
                return f;
            }

            throw new Exception("Should not get there !");
        }

        public static Cpp.Function PauseFunction(HeadlessDocument doc)
        {
            if(doc.Settings.Language == Language.Cpp) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("Utility", Cpp.Scope.EmptyScope), "pause", false);
                f.AddParam(new Cpp.Param(new Cpp.Type("std::chrono::nanoseconds", Cpp.Scope.EmptyScope), "delay"));
                return f;
            }
            else if(doc.Settings.Language == Language.C) {
                var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.EmptyScope, "PetriUtility_pause", false);
                f.AddParam(new Cpp.Param(new Cpp.Type("time_t", Cpp.Scope.EmptyScope), "delay"));
                return f;
            }

            throw new Exception("Should not get there !");
        }

        public Cpp.FunctionInvocation Function {
            get {
                return _function;
            }
            set {
                _function = value;
            }
        }

        public override bool UsesFunction(Cpp.Function f)
        {
            return Function.UsesFunction(f);
        }

        public void GetVariables(HashSet<Cpp.VariableExpression> res)
        {				
            var l = Function.GetLiterals();
            foreach(var ll in l) {
                if(ll is Cpp.VariableExpression) {
                    res.Add(ll as Cpp.VariableExpression);
                }
            }
        }

        Cpp.FunctionInvocation _function;
    }
}

