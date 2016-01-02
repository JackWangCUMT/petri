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

namespace Petri.Test
{
    [TestFixture()]
    public class TestRuntime
    {
        [Test()]
        public void TestRuntime1()
        {
            // GIVEN a petri net created with a custom name
            string name = "Test12345";
            PetriNet pn = new PetriNet(name);

            // WHEN we read the name of the petri net
            string actual = pn.Name;

            // THEN we get an equivalent value
            Assert.AreNotSame(name, actual);
            Assert.AreEqual(name, actual);
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
        public void TestRuntime2()
        {
            Petri.Runtime.PetriNet pn = new Petri.Runtime.PetriNet("Test");

            Petri.Runtime.Action a1 = new Petri.Runtime.Action(1, "action1", Action1, 1);
            Petri.Runtime.Action a2 = new Petri.Runtime.Action(2, "action2", Action2, 1);
            Petri.Runtime.Action a3 = new Petri.Runtime.Action(3, "action3", Action3, 1);

            // TODO: allow AddTransition after AddAction
            pn.AddAction(a1, true);
            pn.AddAction(a2, false);
            pn.AddAction(a3, false);

            Petri.Runtime.Transition t1 = new Petri.Runtime.Transition(4, "transition1", a1, a2, Transition1);
            Petri.Runtime.Transition t2 = new Petri.Runtime.Transition(5, "transition2", a2, a1, Transition2);
            Petri.Runtime.Transition t3 = new Petri.Runtime.Transition(6, "transition3", a1, a3, Transition3);
            a1.AddTransition(t1);
            a2.AddTransition(t2);
            a1.AddTransition(t3);

            counter = 2;

            string stdout, stderr;
            Utility.InvokeAndRedirectOutput(() => {
                pn.Run();
                pn.Join();
            }, out stdout, out stderr);

            Assert.AreEqual("Action1!\nAction2!\nAction1!\nAction3!\n", stdout);
            Assert.IsEmpty(stderr);
        }

        static volatile int counter;
    }
}
