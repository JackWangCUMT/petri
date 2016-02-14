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
using System.Collections.Generic;
using System.Linq;

namespace Petri.Test.Editor.EditorController
{
    [TestFixture()]
    public class TestCloneEntities
    {
        HeadlessDocument _document;

        [TestFixtureSetUp()]
        public void FixtureSetUp()
        {
            _document = new HeadlessDocument("", DocumentSettings.GetDefaultSettings(_document));
            _document.PetriNet = new RootPetriNet(_document);

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
        public void TestCloneEntitiesCount()
        {
            var allEntities = AllEntities();
            var cloned = Petri.Editor.EditorController.CloneEntities(allEntities,
                                                                     _document.PetriNet);

            var clonedStates = from c in cloned
                                        where c is State
                                        select (State)c; 

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
    }
}

