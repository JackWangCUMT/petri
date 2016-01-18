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
using System.Collections.Generic;

namespace Petri.Test.Cpp
{
    [TestFixture()]
    public class TestFunctions
    {
        [Test()]
        public void TestFreeFunction1()
        {
            // GIVEN a function invocation string
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<FunctionInvocation>("f()", Language.CSharp);

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
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<FunctionInvocation>("f(3)", Language.CSharp);

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
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<FunctionInvocation>("f(   3 )", Language.CSharp);

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
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<FunctionInvocation>("f(1 ,  2  )", Language.CSharp);

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
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<FunctionInvocation>("f ()", Language.CSharp);

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(0, e.Function.Parameters.Count);
        }

        [Test()]
        public void TestFreeFunction6()
        {
            // GIVEN a function invocation string
            // WHEN an invocation is created from the string
            var e = Expression.CreateFromString<UnaryExpression>("!f()", Language.CSharp);

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.

            Assert.IsInstanceOf<UnaryExpression>(e);
            Assert.AreEqual(Operator.Name.LogicalNot, e.Operator);

            FunctionInvocation invocation = e.Expression as FunctionInvocation;
            Assert.AreEqual("f", invocation.Function.Name);
            Assert.AreEqual(0, invocation.Function.Parameters.Count);
        }

        [Test()]
        public void TestFreeFunction7()
        {

            // GIVEN a function list
            var functions = new List<Editor.Cpp.Function>();
            var f = new Editor.Cpp.Function(new Editor.Cpp.Type(Language.CSharp, "void"), null, "f", false);
            functions.Add(f);

            // AND a function invocation string
            // WHEN an invocation is created from the string and uses the functions list
            var e = Expression.CreateFromString<FunctionInvocation>("f()", Language.CSharp, functions);

            // THEN the function is created with the right number of parameters, and the number of arguments passed to it is recognized.
            Assert.IsInstanceOf<FunctionInvocation>(e);
            Assert.AreEqual("f", e.Function.Name);
            Assert.AreEqual(0, e.Function.Parameters.Count);
            Assert.AreEqual(0, e.Arguments.Count);
        }
    }
}

