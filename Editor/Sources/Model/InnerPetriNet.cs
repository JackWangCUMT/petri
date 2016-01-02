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
        public InnerPetriNet(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
        {
            _exitPoint = new ExitPoint(doc, this, new Cairo.PointD(300, 100));
            this.AddState(this.ExitPoint);
            this.EntryPointID = Document.LastEntityID++;
        }

        public InnerPetriNet(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor)
        {
            EntryPointID = UInt64.Parse(descriptor.Attribute("EntryPointID").Value);

            foreach(var s in this.States) {
                if(s.GetType() == typeof(ExitPoint)) {
                    _exitPoint = s as ExitPoint;
                    break;
                }
            }

            if(_exitPoint == null)
                throw new Exception(Configuration.GetLocalized("No Exit node found in the saved Petri net!"));
        }

        public override void Serialize(XElement elem)
        {
            elem.SetAttributeValue("EntryPointID", this.EntryPointID);
            base.Serialize(elem);
        }

        public ExitPoint ExitPoint {
            get {
                return _exitPoint;
            }
        }

        public UInt64 EntryPointID {
            get;
            set;
        }

        public string EntryPointName {
            get {
                return this.CppName + "_Entry";
            }
        }

        ExitPoint _exitPoint;
    }
}

