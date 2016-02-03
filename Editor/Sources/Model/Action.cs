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
using Petri.Editor.Code;

namespace Petri.Editor
{
    public sealed class Action : State
    {
        public Action(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc,
                                                                                                   parent,
                                                                                                   active,
                                                                                                   0,
                                                                                                   pos)
        {
            this.Position = pos;
            this.Radius = 20;

            this.Active = active;

            this.Function = new FunctionInvocation(doc.Settings.Language,
                                                   RuntimeFunctions.DoNothingFunction(doc));
        }

        public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc,
                                                                                         parent,
                                                                                         descriptor)
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
                    if(!f.Function.ReturnType.Equals(Code.Type.UnknownType(Document.Settings.Language)) && !f.Function.ReturnType.Equals(Document.Settings.Enum.Type)) {
                        Document.AddConflicting(this,
                                                Configuration.GetLocalized("Incorrect return type for the function: {0} expected, {1} found.",
                                                                           Document.Settings.Enum.Name,
                                                                           f.Function.ReturnType.ToString()));
                    }
                    Function = f;
                }
                else {
                    Function = new WrapperFunctionInvocation(Document.Settings.Language,
                                                             Document.Settings.Enum.Type,
                                                             exp);
                }
            }
            catch(Exception e) {
                Document.AddConflicting(this, e.Message);
                Function = new ConflictFunctionInvocation(Document.Settings.Language, s);
            }
        }

        public override XElement GetXML()
        {
            var elem = new XElement("Action");
            this.Serialize(elem);
            return elem;
        }

        protected override void Serialize(XElement element)
        {
            base.Serialize(element);
            element.SetAttributeValue("Function", this.Function.MakeUserReadable());
        }

        /// <summary>
        /// Gets the action's invocation that pretty prints the petri net action at runtime.
        /// </summary>
        /// <returns>The action.</returns>
        public FunctionInvocation PrintAction()
        {
            return new Code.FunctionInvocation(Document.Settings.Language,
                                               RuntimeFunctions.PrintFunction(Document),
                                               LiteralExpression.CreateFromString("$Name",
                                                                                  Document.Settings.Language),
                                               LiteralExpression.CreateFromString("$ID",
                                                                                  Document.Settings.Language));
        }

        /// <summary>
        /// Gets or sets the function invocation that will be run when the action becomes active.
        /// </summary>
        /// <value>The function.</value>
        public FunctionInvocation Function {
            get;
            set;
        }

        public override bool UsesFunction(Function f)
        {
            return Function.UsesFunction(f);
        }

        /// <summary>
        /// Adds the VariableExpressions contained in the condition to the set passed as an argument.
        /// </summary>
        /// <param name="result">Result.</param>
        public void GetVariables(HashSet<VariableExpression> res)
        {				
            var l = Function.GetVariables();
            foreach(var ll in l) {
                res.Add(ll);
            }
        }
    }
}

