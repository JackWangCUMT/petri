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
        public void TestFixtureSetUp()
        {
            Console.WriteLine("TestFixtureSetUp");
        }

        [TestFixtureTearDown()]
        public void TestFixtureTearDown()
        {
            Console.WriteLine("TestFixtureTearDown");
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
        public void TestThatThrows1()
        {
            Assert.Throws(typeof(Exception), () => { throw new Exception("message");} );
        }

        [Test()]
        public void TestThatThrows2()
        {
            throw new Exception("message");
        }

        [Test()]
        public void TestThatDoesntCompile()
        {
            arg!
        }
    }
}

