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
            Assert.AreEqual("", stdout);
            Assert.AreEqual(0, result);
        }
    }
}

