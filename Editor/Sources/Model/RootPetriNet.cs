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
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Petri.Editor
{
    public class RootPetriNet : PetriNet
    {
        public RootPetriNet(HeadlessDocument doc) : base(doc, null, true, new Cairo.PointD(0, 0))
        {
        }

        public RootPetriNet(HeadlessDocument doc, XElement descriptor) : base(doc,
                                                                              null,
                                                                              descriptor)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.RootPetriNet"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public override bool Active {
            get {
                return true;
            }
            set {
                base.Active = true;
            }
        }

        /// <summary>
        /// Gets or sets the required tokens that must be brought by transitions to activate the state.
        /// </summary>
        /// <value>The required tokens.</value>
        public override int RequiredTokens {
            get {
                return 0;
            }
            set{ }
        }

        /// <summary>
        /// The name is forced to "Root" here.
        /// </summary>
        /// <value>The name.</value>
        public override string Name {
            get {
                return "Root";
            }
            set {
                base.Name = "Root";
            }
        }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>The document.</value>
        public override HeadlessDocument Document {
            get;
            set;
        }

        /// <summary>
        /// Use this to scale down the IDs of entities to 0...N, with N = number of entities
        /// </summary>
        /// <returns><c>true</c> if this instance canonize ; otherwise, <c>false</c>.</returns>
        public void Canonize()
        {
            var entities = this.BuildEntitiesList();
            entities.Add(this);

            entities.Sort(delegate(Entity o1, Entity o2) {
                return o1.ID.CompareTo(o2.ID);
            });

            Document.IDManager = new IDManager(0);
            foreach(Entity o in entities) {
                o.ID = Document.IDManager.Consume();
                if(o is InnerPetriNet) {
                    ((InnerPetriNet)o).EntryPointID = Document.IDManager.Consume();
                }
            }
        }

        /// <summary>
        /// Recursively gets the variables contained in the expressions of child entities. 
        /// </summary>
        /// <value>The variables.</value>
        public HashSet<Code.VariableExpression> Variables {
            get {
                var res = new HashSet<Code.VariableExpression>();
                var list = BuildEntitiesList();
                foreach(Entity e in list) {
                    if(e is Action) {
                        ((Action)e).GetVariables(res);
                    }
                    if(e is Transition) {
                        ((Transition)e).GetVariables(res);
                    }
                }

                return res;
            }
        }
    }
}

