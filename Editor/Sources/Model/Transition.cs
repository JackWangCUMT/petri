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
        public Transition(HeadlessDocument doc, PetriNet parent, State before, State after) : base(doc,
                                                                                                   parent)
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

        public Transition(HeadlessDocument doc,
                          PetriNet parent,
                          XElement descriptor,
                          IDictionary<UInt64, State> statesTable) : base(doc,
                                                                         parent,
                                                                         descriptor)
        {
            this.Before = statesTable[UInt64.Parse(descriptor.Attribute("BeforeID").Value)];
            this.After = statesTable[UInt64.Parse(descriptor.Attribute("AfterID").Value)];

            TrySetCondition(descriptor.Attribute("Condition").Value);

            this.Width = double.Parse(descriptor.Attribute("W").Value);
            this.Height = double.Parse(descriptor.Attribute("H").Value);

            this.Shift = new Cairo.PointD(XmlConvert.ToDouble(descriptor.Attribute("ShiftX").Value),
                                          XmlConvert.ToDouble(descriptor.Attribute("ShiftY").Value));
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

        public override XElement GetXML()
        {
            var elem = new XElement("Transition");
            this.Serialize(elem);
            return elem;
        }

        protected override void Serialize(XElement elem)
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

        /// <summary>
        /// Gets the global direction of the transition, i.e. the direction of the vector going from one end of the transtition to the other.
        /// If the transition comes from and goes from the same state, then a not null vector is returned.
        /// </summary>
        /// <value>The direction.</value>
        public PointD Direction {
            get {
                if(Before == After) {
                    var dir = new PointD(Before.Position.X - Position.X,
                                         Before.Position.Y - Position.Y);
                    return dir;
                }

                return new PointD(After.Position.X - Before.Position.X,
                                  After.Position.Y - Before.Position.Y);
            }
        }

        /// <summary>
        /// Updates the position of the transition's shape so that it gracefully follows the position of the states at its ends.
        /// </summary>
        public void UpdatePosition()
        {
            if(Before != After) {
                UpdatePrivate();
            }
        }

        /// <summary>
        /// Internal position update.
        /// </summary>
        void UpdatePrivate()
        {
            double norm = PetriView.Norm(Direction);
            PointD center = new PointD((Before.Position.X + After.Position.X) / 2,
                                       (Before.Position.Y + After.Position.Y) / 2);
            double amplitude = ((ShiftAmplitude > 1e-3) ? ShiftAmplitude : 1);
            var pos = new PointD(center.X + Shift.X * norm / amplitude,
                                 center.Y + Shift.Y * norm / amplitude);
            Position = pos;
        }

        /// <summary>
        /// Gets or sets the state before, i.e. NOT the one pointed by the transition's arrow.
        /// </summary>
        /// <value>The state before.</value>
        public State Before {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the state after, i.e. the one pointed by the transition's arrow.
        /// </summary>
        /// <value>The state after.</value>
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

                // Prevents access during construction, where the Direction call would choke on a null member.
                if(After != null) {
                    ShiftAmplitude = PetriView.Norm(Direction);
                    PointD center = new PointD((Before.Position.X + After.Position.X) / 2,
                                               (Before.Position.Y + After.Position.Y) / 2);
                    Shift = new PointD(value.X - center.X, value.Y - center.Y);
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the transition's shape.
        /// </summary>
        /// <value>The width.</value>
        public double Width {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the height of the transition's shape.
        /// </summary>
        /// <value>The height.</value>
        public double Height {
            get;
            set;
        }

        /// <summary>
        /// The shift of the transition's shape orthogonal to its Direction.
        /// </summary>
        /// <value>The shift.</value>
        public PointD Shift {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the shift amplitude, which allows a nice resizing of the arrows' curve when one of the transition's end is moveD.
        /// </summary>
        /// <value>The shift amplitude.</value>
        public double ShiftAmplitude {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the expression that is evaluated when the question "Can the transition be crossed?" is asked.
        /// </summary>
        /// <value>The condition.</value>
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

        /// <summary>
        /// Adds the VariableExpressions contained in the condition to the set passed as an argument.
        /// </summary>
        /// <param name="result">Result.</param>
        public void GetVariables(HashSet<VariableExpression> result)
        {
            var l = Condition.GetLiterals();
            foreach(var ll in l) {
                if(ll is VariableExpression) {
                    result.Add(ll as VariableExpression);
                }
            }
        }
    }
}

