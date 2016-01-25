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
using System.Collections;
using System.Xml.Linq;

namespace Petri.Editor
{
    public abstract class PetriNet : State
    {
        public PetriNet(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc,
                                                                                                     parent,
                                                                                                     active,
                                                                                                     0,
                                                                                                     pos)
        {
            Comments = new List<Comment>();
            States = new List<State>();
            Transitions = new List<Transition>();

            this.Radius = 30;
            this.Size = new Cairo.PointD(0, 0);
        }

        public PetriNet(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc,
                                                                                           parent,
                                                                                           descriptor)
        {
            Comments = new List<Comment>();
            States = new List<State>();
            Transitions = new List<Transition>();

            // Used to map XML's IDs of Transitions to actual States, after loading them.
            var statesTable = new Dictionary<UInt64, State>();

            this.Name = descriptor.Attribute("Name").Value;

            foreach(var e in descriptor.Element("Comments").Elements()) {
                var c = Entity.EntityFromXml(Document, e, this, null) as Comment;
                Comments.Add(c);
            }

            foreach(var e in descriptor.Element("States").Elements()) {
                var s = Entity.EntityFromXml(Document, e, this, null) as State;
                statesTable.Add(s.ID, s);
            }

            foreach(var e in descriptor.Element("Transitions").Elements("Transition")) {
                var t = new Transition(doc, this, e, statesTable);
                this.AddTransition(t);
                t.Before.AddTransitionAfter(t);
                t.After.AddTransitionBefore(t);
            }

            foreach(var s in statesTable.Values) {
                this.AddState(s);
            }
        }

        protected override void Serialize(XElement elem)
        {
            base.Serialize(elem);
            var comments = new XElement("Comments");
            foreach(var c in this.Comments) {
                comments.Add(c.GetXML());
            }
            var states = new XElement("States");
            foreach(var s in this.States) {
                states.Add(s.GetXML());
            }
            var transitions = new XElement("Transitions");
            foreach(var t in this.Transitions) {
                transitions.Add(t.GetXML());
            }

            elem.Add(comments);
            elem.Add(states);
            elem.Add(transitions);
        }

        public override XElement GetXML()
        {
            var elem = new XElement("PetriNet");
            this.Serialize(elem);
            return elem;
        }

        public override bool UsesFunction(Code.Function f)
        {
            foreach(var t in Transitions)
                if(t.UsesFunction(f))
                    return true;
            foreach(var s in States)
                if(s.UsesFunction(f))
                    return true;

            return false;
        }

        /// <summary>
        /// Adds a Comment entity to the instance.
        /// </summary>
        /// <param name="comment">The comment.</param>
        public void AddComment(Comment comment)
        {
            Comments.Add(comment);
        }

        /// <summary>
        /// Adds a State entity to the instance.
        /// </summary>
        /// <param name="state">The state.</param>
        public void AddState(State state)
        {
            States.Add(state);
        }

        /// <summary>
        /// Adds a Transition entity to the instance.
        /// </summary>
        /// <param name="transition">The transition.</param>
        public void AddTransition(Transition transition)
        {
            Transitions.Add(transition);
        }

        /// <summary>
        /// The size of the petri net's content.
        /// The Radius property represents the external view of a petri net, and the size property represents the size of the drawing area for the entities inside the petri net.
        /// </summary>
        /// <value>The size.</value>
        public Cairo.PointD Size {
            get;
            set;
        }

        /// <summary>
        /// Gets the comment that is located at the given position, or null if none exist.
        /// </summary>
        /// <returns>The comment at the given position.</returns>
        /// <param name="position">Position.</param>
        public Comment CommentAtPosition(Cairo.PointD position)
        {
            // TODO: come back with a better collision detection algorithm :p
            for(int i = Comments.Count - 1; i >= 0; --i) {
                var c = Comments[i];
                if(Math.Abs(c.Position.X - position.X) <= c.Size.X / 2 && Math.Abs(c.Position.Y - position.Y) < c.Size.Y / 2) {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the state that is located at the given position, or null if none exist.
        /// </summary>
        /// <returns>The syaye at the given position.</returns>
        /// <param name="position">Position.</param>
        public State StateAtPosition(Cairo.PointD position)
        {
            // TODO: come back with a better collision detection algorithm :p
            for(int i = States.Count - 1; i >= 0; --i) {
                var s = States[i];
                if(s.PointInState(position)) {
                    return s;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the transition that is located at the given position, or null if none exist.
        /// </summary>
        /// <returns>The transition at the given position.</returns>
        /// <param name="position">Position.</param>
        public Transition TransitionAtPosition(Cairo.PointD position)
        {
            // TODO: come back with a better collision detection algorithm :p
            for(int i = Transitions.Count - 1; i >= 0; --i) {
                var t = Transitions[i];
                if(Math.Abs(t.Position.X - position.X) <= t.Width / 2 && Math.Abs(t.Position.Y - position.Y) < t.Height / 2) {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes the comment from this instance.
        /// </summary>
        /// <param name="comment">Comment.</param>
        public void RemoveComment(Comment comment)
        {
            Comments.Remove(comment);
        }

        /// <summary>
        /// Removes the state from this instance.
        /// </summary>
        /// <param name="state">State.</param>
        public void RemoveState(State state)
        {
            States.Remove(state);
        }

        /// <summary>
        /// Removes the transition from this instance.
        /// </summary>
        /// <param name="transition">Transition.</param>
        public void RemoveTransition(Transition transition)
        {
            transition.Before.RemoveTransitionAfter(transition);
            transition.After.RemoveTransitionBefore(transition);

            Transitions.Remove(transition);
        }

        /// <summary>
        /// Gets the comments directly contained in this instance, and not recursively from this instance's inner petri nets.
        /// </summary>
        /// <value>The transitions.</value>
        public List<Comment> Comments {
            get;
            private set;
        }

        /// <summary>
        /// Gets the states directly contained in this instance, and not recursively from this instance's inner petri nets.
        /// </summary>
        /// <value>The transitions.</value>
        public List<State> States {
            get;
            private set;
        }

        /// <summary>
        /// Gets the transitions directly contained in this instance, and not recursively from this instance's inner petri nets.
        /// </summary>
        /// <value>The transitions.</value>
        public List<Transition> Transitions {
            get;
            private set;
        }

        /// <summary>
        /// Return the entity which ID is equal to the parameter, or null if none is found.
        /// </summary>
        /// <returns>The entity of ID <paramref name="id"/>, or <c>null</c> if none exist.</returns>
        /// <param name="id">Identifier.</param>
        public Entity EntityFromID(UInt64 id)
        {
            foreach(var s in States) {
                if(s.ID == id)
                    return s;
                if(s is PetriNet) {
                    Entity e = (s as PetriNet).EntityFromID(id);
                    if(e != null)
                        return e;
                }
                if(s is InnerPetriNet && (s as InnerPetriNet).EntryPointID == id) {
                    return s;
                }
            }
            foreach(var t in Transitions) {
                if(t.ID == id)
                    return t;
            }
            foreach(var c in Comments) {
                if(c.ID == id)
                    return c;
            }

            return null;
        }

        /// <summary>
        /// Recursively gets all of the Action/PetriNet/Transitions/Comments contained in the instance.
        /// Does not include <c>this</c>.
        /// </summary>
        /// <returns>The entities list.</returns>
        public List<Entity> BuildEntitiesList()
        {
            var l = new List<Entity>();
            l.AddRange(this.States);

            for(int i = 0; i < this.States.Count; ++i) {
                var s = l[i] as PetriNet;
                if(s != null) {
                    l.AddRange(s.BuildEntitiesList());
                }
            }

            l.AddRange(this.Transitions);
            l.AddRange(this.Comments);

            return l;
        }
    }
}

