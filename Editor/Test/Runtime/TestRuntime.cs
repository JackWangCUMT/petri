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
using Petri.Runtime;
using System.IO;
using Random = System.Random;
using UInt64 = System.UInt64;

namespace Petri.Test
{
    [TestFixture()]
    public class TestRuntime
    {
        Random _random = new Random();

        [Test()]
        public void TestRuntimePetriNetProperties()
        {
            var name = CodeUtility.RandomLiteral(CodeUtility.LiteralType.String);

            // GIVEN a petri net created with a custom name
            PetriNet pn = new PetriNet(name);

            // WHEN we read the properties of the action
            // THEN we get an equivalent value
            Assert.AreNotSame(name, pn.Name);
            Assert.AreEqual(name, pn.Name);
        }

        public static System.Int32 Action1()
        {
            System.Console.WriteLine("Action1!");
            return 0;
        }

        public static System.Int32 Action2()
        {
            System.Console.WriteLine("Action2!");
            return 0;
        }

        public static System.Int32 Action3()
        {
            System.Console.WriteLine("Action3!");
            return 0;
        }

        public static bool Transition1(System.Int32 result)
        {
            --counter;
            return counter > 0;
        }

        public static bool Transition3(System.Int32 result)
        {
            return counter == 0;
        }

        public static bool Transition2(System.Int32 result)
        {
            return true;
        }

        [Test()]
        public void TestRuntime1()
        {
            PetriNet pn = new PetriNet("Test");

            Action a1 = new Action(1, "action1", Action1, 1);
            Action a2 = new Action(2, "action2", Action2, 1);
            Action a3 = new Action(3, "action3", Action3, 1);

            a1.AddTransition(4, "transition1", a2, Transition1);
            a2.AddTransition(5, "transition2", a1, Transition2);
            a1.AddTransition(6, "transition3", a3, Transition3);

            pn.AddAction(a1, true);
            pn.AddAction(a2, false);
            pn.AddAction(a3, false);

            counter = 2;

            string stdout, stderr;
            CompilerUtility.InvokeAndRedirectOutput(() => {
                pn.Run();
                pn.Join();
            }, out stdout, out stderr);

            Assert.AreEqual("Action1!\nAction2!\nAction1!\nAction3!\n", stdout);
            Assert.IsEmpty(stderr);
        }

        [Test(), Repeat(10)]
        public void TestRuntimeActionProperties()
        {
            var name = CodeUtility.RandomLiteral(CodeUtility.LiteralType.String);
            var id = (UInt64)_random.Next();
            var requiredTokens = (UInt64)_random.Next();

            // GIVEN an action created with a custom name, id and required tokens count
            Action a = new Action(id, name, Action1, requiredTokens);

            // WHEN we read the properties of the action
            // THEN we get an equivalent value
            Assert.AreNotSame(name, a.Name);
            Assert.AreEqual(name, a.Name);
            Assert.AreEqual(id, a.ID);
            Assert.AreEqual(requiredTokens, a.RequiredTokens);
            Assert.AreEqual(0, a.CurrentTokens);
        }

        [Test()]
        public void TestRuntimeTransitionProperties()
        {
            var name = CodeUtility.RandomLiteral(CodeUtility.LiteralType.String);
            var id = (UInt64)_random.Next();
            // GIVEN a transition created with a custom name and id
            Action a = new Action(3, "", Action1, 1);
            Transition t = a.AddTransition(id, name, a, Transition1);

            // WHEN we read the name of the transition
            // THEN we get an equivalent value
            Assert.AreNotSame(name, t.Name);
            Assert.AreEqual(name, t.Name);
            Assert.AreEqual(id, t.ID);
        }


        static volatile int counter;
    }
}
