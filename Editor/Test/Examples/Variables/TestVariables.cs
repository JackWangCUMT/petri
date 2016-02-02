using System;
using NUnit.Framework;

namespace Petri.Test
{
    [TestFixture()]
    public class TestVariables
    {
        [Test()]
        public void TestCppExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-kcg", "../../../Examples/CppVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(0, result);
        }

        [Test()]
        public void TestCppExampleExecution()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-r", "../../../Examples/CppVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual(0, result);
        }

        [Test()]
        public void TestCExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-kcg", "../../../Examples/CVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(0, result);
        }

        [Test()]
        public void TestCExampleExecution()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-r", "../../../Examples/CVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual(0, result);
        }

        [Test()]
        public void TestCSharpExampleCompilation()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-kcg", "../../../Examples/CSharpVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(0, result);
        }

        [Test()]
        public void TestCSharpExampleExecution()
        {
            // GIVEN launch arguments requesting code generation and compilation of the Cpp.petri example
            string[] args = { "-r", "../../../Examples/CSharpVar.petri" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Utility.InvokeCompiler(args, out stdout, out stderr);

            // THEN no error is returned.
            Assert.AreEqual("", stderr);
            Assert.AreEqual(0, result);
        }
    }
}

