/*
 * Copyright (c) 2016 Rémi Saurel
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

using NUnit.Framework;
using Petri;
using Petri.Editor;
using Petri.Editor.GUI;
using System.Collections.Generic;
using System.Linq;

namespace Petri.Test.Editor.EditorController
{
    [TestFixture()]
    public class TestCloneEntities
    {
        HeadlessDocument _document;
        Dictionary<string, Action> _actions;
        Dictionary<string, Transition> _transitions;
        Dictionary<string, Comment> _comments;

        [TestFixtureSetUp()]
        public void FixtureSetUp()
        {
            _document = new HeadlessDocument("", DocumentSettings.GetDefaultSettings(_document));
            _document.PetriNet = new RootPetriNet(_document);

            _actions = new Dictionary<string, Action>();
            _transitions = new Dictionary<string, Transition>();
            _comments = new Dictionary<string, Comment>();

            var a1 = new Action(_document, _document.PetriNet, false, new Cairo.PointD(0, 0));
            a1.Name = "A1";
            var a2 = new Action(_document, _document.PetriNet, false, new Cairo.PointD(0, 0));
            a2.Name = "A2";
            var a3 = new Action(_document, _document.PetriNet, false, new Cairo.PointD(0, 0));
            a3.Name = "A3";

            var t1 = new Transition(_document, _document.PetriNet, a1, a2);
            t1.Name = "T1";
            var t2 = new Transition(_document, _document.PetriNet, a2, a3);
            t2.Name = "T2";
            var t3 = new Transition(_document, _document.PetriNet, a3, a1);
            t3.Name = "T3";
            var t4 = new Transition(_document, _document.PetriNet, a2, a1);
            t4.Name = "T4";
            var t5 = new Transition(_document, _document.PetriNet, a1, a1);
            t5.Name = "T5";

            var c1 = new Comment(_document, _document.PetriNet, new Cairo.PointD(0, 0));
            c1.Name = "C1";
            var c2 = new Comment(_document, _document.PetriNet, new Cairo.PointD(0, 0));
            c2.Name = "C2";

            new AddStateAction(a1).Apply();
            new AddStateAction(a2).Apply();
            new AddStateAction(a3).Apply();

            new AddTransitionAction(t1, false).Apply();
            new AddTransitionAction(t2, false).Apply();
            new AddTransitionAction(t3, false).Apply();
            new AddTransitionAction(t4, false).Apply();
            new AddTransitionAction(t5, false).Apply();

            new AddCommentAction(c1).Apply();
            new AddCommentAction(c2).Apply();

            foreach(State s in _document.PetriNet.States) {
                _actions.Add(s.Name, (Action)s);
            }

            foreach(Transition t in _document.PetriNet.Transitions) {
                _transitions.Add(t.Name, t);
            }

            foreach(Comment c in _document.PetriNet.Comments) {
                _comments.Add(c.Name, c);
            }
        }

        List<Entity> AllEntities()
        {
            var entities = new List<Entity>();
            entities.AddRange(_document.PetriNet.States);
            entities.AddRange(_document.PetriNet.Transitions);
            entities.AddRange(_document.PetriNet.Comments);

            return entities;
        }

        [Test()]
        public void TestSetup()
        {
            Assert.Greater(_document.PetriNet.States.Count, 0);
            Assert.Greater(_document.PetriNet.Transitions.Count, 0);
            Assert.Greater(_document.PetriNet.Comments.Count, 0);
        }

        [Test()]
        public void TestCloneEntitiesCount()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedStates = from c in cloned
                                        where c is Action
                                        select (Action)c; 

            var clonedTransitions = from c in cloned
                                             where c is Transition
                                             select (Transition)c; 

            var clonedComments = from c in cloned
                                          where c is Comment
                                          select (Comment)c; 

            Assert.AreEqual(allEntities.Count, cloned.Count);
            Assert.AreEqual(_document.PetriNet.States.Count, clonedStates.Count());
            Assert.AreEqual(_document.PetriNet.Transitions.Count, clonedTransitions.Count());
            Assert.AreEqual(_document.PetriNet.Comments.Count, clonedComments.Count());
        }

        [Test()]
        public void TestCloneEntitiesActions()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedStates = from c in cloned
                                        where c is Action
                                        select (Action)c;

            var actions = new Dictionary<string, Action>();

            foreach(Action a in clonedStates) {
                actions.Add(a.Name, a);
            }
                
            Assert.AreEqual(_actions.Count, actions.Count);
            foreach(var a in _actions) {
                Assert.That(actions.ContainsKey(a.Key));
            }
            foreach(var a in actions) {
                Assert.That(_actions.ContainsKey(a.Key));
            }
        }

        [Test()]
        public void TestCloneEntitiesTransitions()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedTransitions = from c in cloned
                                             where c is Transition
                                             select (Transition)c; 

            var transitions = new Dictionary<string, Transition>();

            foreach(Transition a in clonedTransitions) {
                transitions.Add(a.Name, a);
            }

            Assert.AreEqual(_transitions.Count, transitions.Count);
            foreach(var a in _transitions) {
                Assert.That(transitions.ContainsKey(a.Key));
            }
            foreach(var a in transitions) {
                Assert.That(_transitions.ContainsKey(a.Key));
            }
        }

        [Test()]
        public void TestCloneEntitiesComments()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedComments = from c in cloned
                                          where c is Comment
                                          select (Comment)c; 

            var comments = new Dictionary<string, Comment>();

            foreach(Comment a in clonedComments) {
                comments.Add(a.Name, a);
            }

            Assert.AreEqual(_comments.Count, comments.Count);
            foreach(var a in _comments) {
                Assert.That(comments.ContainsKey(a.Key));
            }
            foreach(var a in comments) {
                Assert.That(_comments.ContainsKey(a.Key));
            }
        }

        [Test()]
        public void TestCloneEntitiesTransitionsBefore()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedStates = from c in cloned
                                        where c is Action
                                        select (Action)c;

            var clonedTransitions = new List<Transition>(from c in cloned
                                                                  where c is Transition
                                                                  select (Transition)c); 

            foreach(var c in clonedStates) {
                var original = _actions[c.Name];
                Assert.AreEqual(original.TransitionsBefore.Count, c.TransitionsBefore.Count);

                foreach(var t in original.TransitionsBefore) {
                    bool found = false;
                    foreach(var t2 in c.TransitionsBefore) {
                        if(t.Name == t2.Name) {
                            Assert.Contains(t2, clonedTransitions);
                            found = true;
                            break;
                        }
                    }
                    if(!found) {
                        Assert.Fail("Missing transition in TransitionsBefore!");
                    }
                }
            }
        }

        [Test()]
        public void TestCloneEntitiesTransitionsAfter()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedStates = from c in cloned
                                        where c is Action
                                        select (Action)c;

            var clonedTransitions = new List<Transition>(from c in cloned
                                                                  where c is Transition
                                                                  select (Transition)c); 

            foreach(var c in clonedStates) {
                var original = _actions[c.Name];
                Assert.AreEqual(original.TransitionsAfter.Count, c.TransitionsAfter.Count);

                foreach(var t in original.TransitionsAfter) {
                    bool found = false;
                    foreach(var t2 in c.TransitionsAfter) {
                        Assert.Contains(t2, clonedTransitions);
                        if(t.Name == t2.Name) {
                            found = true;
                            break;
                        }
                    }
                    if(!found) {
                        Assert.Fail("Missing transition in TransitionsAfter!");
                    }
                }
            }
        }

        [Test()]
        public void TestCloneEntitiesTransitionsEnds()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.GUI.Editor.EditorController.CloneEntities(allEntities,
                                                                                _document.PetriNet);

            var clonedTransitions = new List<Transition>(from c in cloned
                                                                  where c is Transition
                                                                  select (Transition)c); 

            foreach(var c in clonedTransitions) {
                var original = _transitions[c.Name];

                Assert.AreEqual(original.Before.Name, c.Before.Name);
                Assert.AreNotEqual(original.Before, c.Before);

                Assert.AreEqual(original.After.Name, c.After.Name);
                Assert.AreNotEqual(original.After, c.After);
            }
        }
    }
}

