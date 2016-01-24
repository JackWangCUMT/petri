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

namespace Petri.Test
{
    [TestFixture()]
    public class TestInvocation
    {
        [Test()]
        public void TestInvalidArgument()
        {
            // GIVEN launch arguments containing an invalid argument
            string[] args = { "fkziovfjiojer", "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreNotEqual(0, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.EndsWith("\n" + Editor.Application.HelpString + "\n"));
            Assert.IsTrue(stderr.Contains("Invalid argument"));
            Assert.IsTrue(stderr.Contains(args[0]));
        }

        [Test()]
        public void TestHelp1()
        {
            // GIVEN launch arguments requesting help
            string[] args = { "--help" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned and the expected string is output
            Assert.AreEqual(0, result);
            Assert.AreEqual(Editor.Application.HelpString + "\n", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestHelp2()
        {
            // GIVEN launch arguments requesting help
            string[] args = { "-h" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned and the expected string is output
            Assert.AreEqual(0, result);
            Assert.AreEqual(Editor.Application.HelpString + "\n", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestGenerationWithoutDocument()
        {
            // GIVEN launch arguments requesting the code generation of a not provided document
            string[] args = { "--generate" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(Editor.Application.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(Editor.Application.MissingPetriDocument + "\n" + Editor.Application.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestCompilationWithoutDocument()
        {
            // GIVEN launch arguments requesting the compilation of a not provided document
            string[] args = { "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(Editor.Application.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(Editor.Application.MissingPetriDocument + "\n" + Editor.Application.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestGenerationAndCompilationWithoutDocument()
        {
            // GIVEN launch arguments requesting the code generation and compilation of a not provided document
            string[] args = { "--generate", "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(Editor.Application.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(Editor.Application.MissingPetriDocument + "\n" + Editor.Application.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestGenerationWithInvalidDocument()
        {
            // GIVEN launch arguments requesting the code generation with an invalid document path
            string[] args = { "--generate", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.UnexpectedError, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.StartsWith("An exception occurred: "));
        }

        [Test()]
        public void TestCompilationWithInvalidDocument()
        {
            // GIVEN launch arguments requesting the code generation with an invalid document path
            string[] args = { "--compile", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.UnexpectedError, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.StartsWith("An exception occurred: "));
        }

        [Test()]
        public void TestGenerationAndCompilationWithInvalidDocument()
        {
            // GIVEN launch arguments requesting the code generation with an invalid document path
            string[] args = { "--generate", "--compile", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.UnexpectedError, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.StartsWith("An exception occurred: "));
        }

        [Test()]
        public void TestArchitectureWithInvalidDocument1()
        {
            // GIVEN launch arguments requesting the code generation with a 32 bit architecture and an invalid document path
            string[] args = { "--generate", "--arch", "32", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.UnexpectedError, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.StartsWith("An exception occurred: "));
        }

        [Test()]
        public void TestArchitectureWithInvalidDocument2()
        {
            // GIVEN launch arguments requesting the code generation with a 64 bit architecture and an invalid document path
            string[] args = { "--generate", "--arch", "64", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.UnexpectedError, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.StartsWith("An exception occurred: "));
        }

        [Test()]
        public void TestInvalidArchitectureWithInvalidDocument()
        {
            // GIVEN launch arguments requesting the code generation with an invalid architecture and an invalid document path
            string[] args = { "--generate", "--arch", "blob", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(Editor.Application.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(Editor.Application.WrongArchitecture + "\n" + Editor.Application.HelpString + "\n", stderr);
        }
    }
}

