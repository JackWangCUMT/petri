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
using System;
using Petri.Editor.Cpp;

namespace Petri.Test.Cpp
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

