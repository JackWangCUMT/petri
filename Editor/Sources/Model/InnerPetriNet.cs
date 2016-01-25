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
using System.Xml;
using System.Xml.Linq;

namespace Petri.Editor
{
    public sealed class InnerPetriNet : PetriNet
    {
        public InnerPetriNet(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc,
                                                                                                          parent,
                                                                                                          active,
                                                                                                          pos)
        {
            ExitPoint = new ExitPoint(doc, this, new Cairo.PointD(300, 100));
            this.AddState(this.ExitPoint);
            this.EntryPointID = Document.IDManager.Consume();
        }

        public InnerPetriNet(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc,
                                                                                                parent,
                                                                                                descriptor)
        {
            EntryPointID = UInt64.Parse(descriptor.Attribute("EntryPointID").Value);

            foreach(var s in this.States) {
                if(s.GetType() == typeof(ExitPoint)) {
                    ExitPoint = s as ExitPoint;
                    break;
                }
            }

            if(ExitPoint == null)
                throw new Exception(Configuration.GetLocalized("No Exit node found in the saved Petri net!"));
        }

        protected override void Serialize(XElement elem)
        {
            elem.SetAttributeValue("EntryPointID", this.EntryPointID);
            base.Serialize(elem);
        }

        /// <summary>
        /// Gets the exit point of the inner petri net, which is supposed to be a convenient state to synchronize every other state upon the inner petri net's end of execution.
        /// </summary>
        /// <value>The exit point.</value>
        public ExitPoint ExitPoint {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the entry point ID. It represents the virtual EntryPoint entity and is used for code generation.
        /// </summary>
        /// <value>The entry point ID.</value>
        public UInt64 EntryPointID {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the entry point name. It represents the virtual EntryPoint entity and is used for code generation.
        /// </summary>
        /// <value>The entry point name.</value>
        public string EntryPointName {
            get {
                return this.CodeIdentifier + "_Entry";
            }
        }
    }
}

