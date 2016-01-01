using NUnit.Framework;
using System;
using Petri.Runtime;
using System.IO;

namespace TestPetri
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
    }
}
