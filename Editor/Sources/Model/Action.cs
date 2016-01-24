﻿/*
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
using Petri.Editor.Code;

namespace Petri.Editor
{
    public sealed class Action : NonRootState
    {
        public Action(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
        {
            this.Position = pos;
            this.Radius = 20;

            this.Active = active;

            this.Function = new FunctionInvocation(doc.Settings.Language, DoNothingFunction(doc));
        }

        public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor)
        {
            TrySetFunction(descriptor.Attribute("Function").Value);
        }

        private void TrySetFunction(string s)
        {
            Expression exp;
            try {
                exp = Expression.CreateFromStringAndEntity<Expression>(s, this);
                if(exp is FunctionInvocation) {
                    var f = (FunctionInvocation)exp;
                    if(!f.Function.ReturnType.Equals(Document.Settings.Enum.Type)) {
                        Document.Conflicting.Add(this);
                    }
                    Function = f;
                }
                else {
                    Function = new WrapperFunctionInvocation(Document.Settings.Language, Document.Settings.Enum.Type, exp);
                }
            }
            catch(Exception) {
                Document.Conflicting.Add(this);
                Function = new ConflictFunctionInvocation(Document.Settings.Language, s);
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

        public FunctionInvocation PrintAction()
        {
            return new Code.FunctionInvocation(Document.Settings.Language, PrintFunction(Document), LiteralExpression.CreateFromString("$Name", Document.Settings.Language), LiteralExpression.CreateFromString("$ID", Document.Settings.Language));
        }

        public static Function PrintFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;

            if(lang == Language.Cpp) {
                var f = new Code.Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Utility"), "printAction", false);
                f.AddParam(new Param(new Code.Type(lang, "std::string const &"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "std::uint64_t"), "id"));

                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type, null, "PetriUtility_printAction", false);
                f.AddParam(new Param(new Code.Type(lang, "char const *"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "uint64_t"), "id"));

                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Petri.Runtime.Utility"), "PrintAction", false);
                f.AddParam(new Param(new Code.Type(lang, "string"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "UInt64"), "id"));

                return f;
            }

            throw new Exception("Action.PrintFunction: Should not get there !");
        }

        public static Function DoNothingFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;
            if(lang == Language.Cpp) {
                var f = new Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Utility"), "doNothing", false);
                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type, null, "PetriUtility_doNothing", false);
                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Petri.Runtime.Utility"), "DoNothing", false);
                return f;
            }

            throw new Exception("Action.DoNothingFunction: Should not get there !");
        }

        public static Function PauseFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;

            if(lang == Language.Cpp) {
                var f = new Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Utility"), "pause", false);
                f.AddParam(new Param(new Code.Type(lang, "std::chrono::nanoseconds"), "delay"));
                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type, null, "PetriUtility_pause", false);
                f.AddParam(new Param(new Code.Type(lang, "time_t"), "delay"));
                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type, Scope.MakeFromNamespace(lang, "Petri.Runtime.Utility"), "Pause", false);
                f.AddParam(new Param(new Code.Type(lang, "double"), "delay"));
                return f;
            }

            throw new Exception("Action.PauseFunction: Should not get there !");
        }

        public FunctionInvocation Function {
            get {
                return _function;
            }
            set {
                _function = value;
            }
        }

        public override bool UsesFunction(Function f)
        {
            return Function.UsesFunction(f);
        }

        public void GetVariables(HashSet<VariableExpression> res)
        {				
            var l = Function.GetLiterals();
            foreach(var ll in l) {
                if(ll is VariableExpression) {
                    res.Add(ll as VariableExpression);
                }
            }
        }

        FunctionInvocation _function;
    }
}

