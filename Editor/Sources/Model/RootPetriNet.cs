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

        public RootPetriNet(HeadlessDocument doc, XElement descriptor) : base(doc, null, descriptor)
        {
        }

        public override bool Active {
            get {
                return true;
            }
            set {
                base.Active = true;
            }
        }

        public override int RequiredTokens {
            get {
                return 0;
            }
            set {
            }
        }

        public override string Name {
            get {
                return "Root";
            }
            set {
                base.Name = "Root";
            }
        }

        public override HeadlessDocument Document {
            get;
            set;
        }
		
        // Use this to scale down the IDs of entities to 0...N, with N = number of entities
        public void Canonize()
        {
            var entities = this.BuildEntitiesList();
            entities.Add(this);

            entities.Sort(delegate(Entity o1, Entity o2) {
                return o1.ID.CompareTo(o2.ID);
            });

            Document.LastEntityID = 0;
            foreach(Entity o in entities) {
                o.ID = Document.LastEntityID++;
                if(o is InnerPetriNet) {
                    ((InnerPetriNet)o).EntryPointID = Document.LastEntityID++;
                }
            }
        }

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

