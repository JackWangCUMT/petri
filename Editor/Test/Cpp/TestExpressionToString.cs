using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri.Cpp
{
    [TestFixture()]
    public class TestExpressionToString
    {
        [Test()]
        public void TestAdditionMakeCpp()
        {
            // GIVEN a simple addition expression
            // WHEN we create an expression from it and convert it to a C++ string
            var e = Expression.CreateFromString("3+4");

            // THEN the string representations of the expression is as expected
            Assert.AreEqual("3 + 4", e.MakeCpp());
            Assert.AreEqual("3 + 4", e.MakeUserReadable());
        }

        [Test()]
        public void TestMultiplicationToCpp()
        {
            // GIVEN a composition of an addition and multiplication
            // WHEN we create an expression from it and convert it to a C++ string
            var e = Expression.CreateFromString("3+4*5");

            // THEN the string representations of the expression is as expected
            Assert.AreEqual("3 + 4 * 5", e.MakeCpp());
            Assert.AreEqual("3 + 4 * 5", e.MakeUserReadable());
        }

        [Test()]
        public void TestFunctionInvocationToString1()
        {
            // GIVEN a function invocation string
            string invocation = "f()";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString<FunctionInvocation>(invocation);

            // THEN the string representations of the expression is as expected
            Assert.AreEqual(invocation, e.MakeCpp());
            Assert.AreEqual(invocation, e.MakeUserReadable());
        }

        [Test()]
        public void TestFunctionInvocationToString2()
        {
            // GIVEN a function invocation string
            string invocation = "f(a)";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString<FunctionInvocation>(invocation);

            // THEN the string representations of the expression is as expected
            Assert.AreEqual("f((Petri::UnknownType)(a))", e.MakeCpp());
            Assert.AreEqual(invocation, e.MakeUserReadable());
        }

        [Test()]
        public void TestFunctionInvocationToString3()
        {
            // GIVEN a function invocation string
            string invocation = "f( a             )";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString<FunctionInvocation>(invocation);

            // THEN the string representations of the expression is as expected
            Assert.AreEqual("f((Petri::UnknownType)(a))", e.MakeCpp());
            Assert.AreEqual("f(a)", e.MakeUserReadable());
        }

        [Test()]
        public void TestFunctionInvocationToString4()
        {
            // GIVEN a function invocation string
            string invocation = "f( a     ,b, c        )";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString<FunctionInvocation>(invocation);

            // THEN the string representations of the expression is as expected
            Assert.AreEqual("f((Petri::UnknownType)(a), (Petri::UnknownType)(b), (Petri::UnknownType)(c))", e.MakeCpp());
            Assert.AreEqual("f(a, b, c)", e.MakeUserReadable());
        }
    }
}

