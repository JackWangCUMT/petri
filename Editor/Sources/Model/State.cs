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

namespace Petri.Editor
{
    public abstract class State : Entity
    {
        public State(HeadlessDocument doc, PetriNet parent, bool active, int requiredTokens, Cairo.PointD pos) : base(doc, parent)
        {
            this.TransitionsBefore = new List<Transition>();
            this.TransitionsAfter = new List<Transition>();

            this.Active = active;
            this.RequiredTokens = requiredTokens;
            this.Position = pos;
            this.Name = this.ID.ToString();
        }

        public State(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor)
        {
            this.TransitionsBefore = new List<Transition>();
            this.TransitionsAfter = new List<Transition>();

            this.Active = XmlConvert.ToBoolean(descriptor.Attribute("Active").Value);
            this.RequiredTokens = XmlConvert.ToInt32(descriptor.Attribute("RequiredTokens").Value);
            this.Radius = XmlConvert.ToDouble(descriptor.Attribute("Radius").Value);
        }

        protected override void Serialize(XElement element)
        {
            base.Serialize(element);
            element.SetAttributeValue("Active", this.Active);
            element.SetAttributeValue("RequiredTokens", this.RequiredTokens);
            element.SetAttributeValue("Radius", this.Radius);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.State"/> is active when the petri net starts its execution.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public virtual bool Active {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the required tokens that must be brought by transitions to activate the state.
        /// </summary>
        /// <value>The required tokens.</value>
        public virtual int RequiredTokens {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of transitions that lead to <c>this</c>.
        /// </summary>
        /// <value>The transitions before.</value>
        public List<Transition> TransitionsBefore {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of transitions that go from <c>this</c> to another state.
        /// </summary>
        /// <value>The transitions after.</value>
        public List<Transition> TransitionsAfter {
            get;
            private set;
        }

        /// <summary>
        /// Adds a transition before.
        /// <see cref="State.TransitionBefore"/>
        /// </summary>
        /// <param name="transition">The transition.</param>
        public void AddTransitionBefore(Transition transition)
        {
            TransitionsBefore.Add(transition);
        }

        /// <summary>
        /// Adds a transition after.
        /// <see cref="State.TransitionAfter"/>
        /// </summary>
        /// <param name="transition">The transition.</param>
        public void AddTransitionAfter(Transition transition)
        {
            TransitionsAfter.Add(transition);
        }

        /// <summary>
        /// Removes a transition before.
        /// <see cref="State.TransitionBefore"/>
        /// </summary>
        /// <param name="transition">The transition to remove.</param>
        public void RemoveTransitionBefore(Transition t)
        {
            TransitionsBefore.Remove(t);
        }

        /// <summary>
        /// Removes a transition after.
        /// <see cref="State.TransitionAfter"/>
        /// </summary>
        /// <param name="transition">The transition to remove.</param>
        public void RemoveTransitionAfter(Transition t)
        {
            TransitionsAfter.Remove(t);
        }

        /// <summary>
        /// Gets or sets the position as in <c>Entity.Position</c>, except that the transitions attached to this instance are given a chance to gracefully update their position.
        /// </summary>
        /// <value>The position.</value>
        public override Cairo.PointD Position {
            get {
                return base.Position;
            }
            set {
                base.Position = value;

                // Prevent execution during State construction
                if(TransitionsBefore != null) {
                    foreach(Transition t in TransitionsBefore) {
                        t.UpdatePosition();
                    }
                    foreach(Transition t in TransitionsAfter) {
                        t.UpdatePosition();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the radius of the circle representing the state.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius {
            get;
            set;
        }

        public override string CodeIdentifier {
            get {
                return "state_" + this.ID.ToString();
            }
        }

        /// <summary>
        /// Checks whether the parameter is enclosed into this instance's shape.
        /// </summary>
        /// <returns><c>true</c>, if in <c>point</c> is contained by this instance, <c>false</c> otherwise.</returns>
        /// <param name="p">P.</param>
        public virtual bool PointInState(Cairo.PointD p)
        {
            if(Math.Pow(p.X - this.Position.X, 2) + Math.Pow(p.Y - this.Position.Y, 2) < Math.Pow(this.Radius, 2)) {
                return true;
            }

            return false;
        }

        public virtual void UpdateConflicts()
        {
			
        }
    }
}

