using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri
{
    [TestFixture()]
    public class TestFunctions
    {
        [Test()]
        public void TestFreeFunction1()
        {
            // GIVEN a function invocation string
            var e = Expression.CreateFromString<FunctionInvocation>("f()");

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(0, e.Function.Parameters.Count);
            Assert.AreEqual(0, e.Arguments.Count);
        }

        [Test()]
        public void TestFreeFunction2()
        {
            // GIVEN a function invocation string
            var e = Expression.CreateFromString<FunctionInvocation>("f(3)");

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(1, e.Function.Parameters.Count);
            Assert.AreEqual(1, e.Arguments.Count);
        }

        [Test()]
        public void TestFreeFunction3()
        {
            // GIVEN a function invocation string
            var e = Expression.CreateFromString<FunctionInvocation>("f(   3 )");

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(1, e.Function.Parameters.Count);
            Assert.AreEqual(1, e.Arguments.Count);
        }
    }
}

