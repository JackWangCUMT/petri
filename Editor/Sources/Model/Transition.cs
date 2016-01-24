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
using Cairo;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Petri.Editor.Code;

namespace Petri.Editor
{
    public class Transition : Entity
    {
        public Transition(HeadlessDocument doc, PetriNet s, State before, State after) : base(doc, s)
        {
            this.Before = before;
            this.After = after;

            this.Name = ID.ToString();

            this.Width = 50;
            this.Height = 30;

            if(Before == After) {
                this.Shift = new PointD(40, 40);
            }
            else {
                this.Shift = new PointD(0, 0);
            }

            base.Position = new PointD(0, 0);
            this.ShiftAmplitude = PetriView.Norm(Direction);

            this.Condition = Expression.CreateFromStringAndEntity<Expression>("true", this);

            UpdatePrivate();
        }

        public Transition(HeadlessDocument doc, PetriNet parent, XElement descriptor, IDictionary<UInt64, State> statesTable) : base(doc, parent, descriptor)
        {
            this.Before = statesTable[UInt64.Parse(descriptor.Attribute("BeforeID").Value)];
            this.After = statesTable[UInt64.Parse(descriptor.Attribute("AfterID").Value)];

            TrySetCondition(descriptor.Attribute("Condition").Value);

            this.Width = double.Parse(descriptor.Attribute("W").Value);
            this.Height = double.Parse(descriptor.Attribute("H").Value);

            this.Shift = new Cairo.PointD(XmlConvert.ToDouble(descriptor.Attribute("ShiftX").Value), XmlConvert.ToDouble(descriptor.Attribute("ShiftY").Value));
            this.ShiftAmplitude = XmlConvert.ToDouble(descriptor.Attribute("ShiftAmplitude").Value);

            this.Position = this.Position;
        }
        
        private void TrySetCondition(string s)
        {
            try {
                Condition = Expression.CreateFromStringAndEntity<Expression>(s, this);
            }
            catch(Exception) {
                Document.Conflicting.Add(this);
                Condition = LiteralExpression.CreateFromString(s, Document.Settings.Language);
            }
        }

        public void UpdateConflicts()
        {
            this.TrySetCondition(Condition.MakeUserReadable());
        }

        public override XElement GetXml()
        {
            var elem = new XElement("Transition");
            this.Serialize(elem);
            return elem;
        }

        public override void Serialize(XElement elem)
        {
            base.Serialize(elem);
            elem.SetAttributeValue("BeforeID", this.Before.ID);
            elem.SetAttributeValue("AfterID", this.After.ID);

            elem.SetAttributeValue("Condition", this.Condition.MakeUserReadable());

            elem.SetAttributeValue("W", this.Width);
            elem.SetAttributeValue("H", this.Height);

            elem.SetAttributeValue("ShiftX", this.Shift.X);
            elem.SetAttributeValue("ShiftY", this.Shift.Y);
            elem.SetAttributeValue("ShiftAmplitude", this.ShiftAmplitude);
        }

        public override bool UsesFunction(Function f)
        {
            return Condition.UsesFunction(f);
        }

        public PointD Direction {
            get {
                if(Before == After) {
                    var dir = new PointD(Before.Position.X - Position.X, Before.Position.Y - Position.Y);
                    return dir;
                }

                return new PointD(After.Position.X - Before.Position.X, After.Position.Y - Before.Position.Y);
            }
        }

        public void UpdatePosition()
        {
            if(Before != After) {
                UpdatePrivate();
            }
        }

        private void UpdatePrivate()
        {
            double norm = PetriView.Norm(Direction);
            PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
            double amplitude = ((ShiftAmplitude > 1e-3) ? ShiftAmplitude : 1);
            var pos = new PointD(center.X + Shift.X * norm / amplitude, center.Y + Shift.Y * norm / amplitude);
            Position = pos;
        }

        public State Before {
            get;
            set;
        }

        public State After {
            get;
            set;
        }

        public override PointD Position {
            get {
                return base.Position;
            }
            set {
                base.Position = value;

                // Prevents access during construction
                if(After != null) {
                    ShiftAmplitude = PetriView.Norm(Direction);
                    PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
                    Shift = new PointD(value.X - center.X, value.Y - center.Y);
                }
            }
        }

        public double Width {
            get;
            set;
        }

        public double Height {
            get;
            set;
        }

        public PointD Shift {
            get;
            set;
        }

        public double ShiftAmplitude {
            get;
            set;
        }

        public Expression Condition {
            get;
            set;
        }

        public override string CodeIdentifier {
            get {
                return "transition_" + this.ID.ToString();
            }
        }

        public override bool StickToGrid {
            get {
                return false;
            }
        }

        public void GetVariables(HashSet<VariableExpression> res)
        {				
            var l = Condition.GetLiterals();
            foreach(var ll in l) {
                if(ll is VariableExpression) {
                    res.Add(ll as VariableExpression);
                }
            }
        }
    }
}

