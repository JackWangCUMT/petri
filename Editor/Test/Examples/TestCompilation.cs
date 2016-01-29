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
using Petri;
using System.IO;

namespace Petri.Test.Examples
{
    [TestFixture()]
    public class TestCompilation
    {
        [Test()]
        public void TestCSharpExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the C#.petri example
            string[] args = { "-k", "-g", "-c", "../../../Examples/CSharp.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual(0, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestCppExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-k", "-g", "-c", "../../../Examples/Cpp.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual(0, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestCExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-k", "-g", "-c", "../../../Examples/C.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual(0, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual("", stderr);
        }
    }
}

