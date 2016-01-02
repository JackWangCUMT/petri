﻿using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri.Cpp
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

            Assert.AreEqual("3", e.Arguments[0].MakeCpp());
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

            Assert.AreEqual("3", e.Arguments[0].MakeCpp());
        }

        [Test()]
        public void TestFreeFunction4()
        {
            // GIVEN a function invocation string
            var e = Expression.CreateFromString<FunctionInvocation>("f(1 ,  2  )");

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(2, e.Function.Parameters.Count);
            Assert.AreEqual(2, e.Arguments.Count);

            Assert.AreEqual("1", e.Arguments[0].MakeCpp());
            Assert.AreEqual("2", e.Arguments[1].MakeCpp());
        }

        [Test()]
        public void TestFreeFunction5()
        {
            // GIVEN a function invocation string
            var e = Expression.CreateFromString<FunctionInvocation>("f ()");

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(0, e.Function.Parameters.Count);
        }
    }
}

