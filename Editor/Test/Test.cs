using NUnit.Framework;
using System;

namespace TestPetri
{
    [TestFixture()]
    public class Test1
    {
        [SetUp()]
        public void SetUp()
        {
            Console.WriteLine("SetUp");
        }

        [TearDown()]
        public void TearDown()
        {
            Console.WriteLine("TearDown");
        }

        [TestFixtureSetUp()]
        public void FixtureSetUp()
        {
        }

        [TestFixtureTearDown()]
        public void FixtureTearDown()
        {
        }

        [Test()]
        public void TestThatFails()
        {
            Assert.AreEqual("expected", "actual");
        }

        [Test()]
        public void TestThatSucceeds()
        {
            Assert.AreEqual("expected", "expected");
        }

        [Test()]
        [ExpectedException(typeof(Exception))]
        public void TestThatThrows1()
        {
            throw new Exception("message");
        }

        [Test()]
        public void TestThatThrows2()
        {
            throw new Exception("message");
        }
    }
}

