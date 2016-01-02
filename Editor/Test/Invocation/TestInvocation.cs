using NUnit.Framework;
using System;
using Petri;
using System.IO;

namespace TestPetri
{
    [TestFixture()]
    public class TestInvocation
    {
        int Invoke(string[] args, out string stdout, out string stderr)
        {
            return Utility.InvokeAndRedirectOutput(() => {
                return Petri.MainClass.Main(args);
            }, out stdout, out stderr);
        }

        [Test()]
        public void TestInvalidArgument()
        {
            // GIVEN launch arguments containing an invalid argument
            string[] args = { "fkziovfjiojer", "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreNotEqual(0, result);
            Assert.AreEqual("", stdout);
            Assert.IsTrue(stderr.EndsWith("\n" + MainClass.HelpString + "\n"));
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned and the expected string is output
            Assert.AreEqual(0, result);
            Assert.AreEqual(MainClass.HelpString + "\n", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestHelp2()
        {
            // GIVEN launch arguments requesting help
            string[] args = { "-h" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned and the expected string is output
            Assert.AreEqual(0, result);
            Assert.AreEqual(MainClass.HelpString + "\n", stdout);
            Assert.AreEqual("", stderr);
        }

        [Test()]
        public void TestGenerationWithoutDocument()
        {
            // GIVEN launch arguments requesting the code generation of a not provided document
            string[] args = { "--generate" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(MainClass.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(MainClass.MissingPetriDocument + "\n" + MainClass.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestCompilationWithoutDocument()
        {
            // GIVEN launch arguments requesting the compilation of a not provided document
            string[] args = { "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(MainClass.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(MainClass.MissingPetriDocument + "\n" + MainClass.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestGenerationAndCompilationWithoutDocument()
        {
            // GIVEN launch arguments requesting the code generation and compilation of a not provided document
            string[] args = { "--generate", "--compile" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN an error is returned and the expected error string is output
            Assert.AreEqual(MainClass.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(MainClass.MissingPetriDocument + "\n" + MainClass.HelpString + "\n", stderr);
        }

        [Test()]
        public void TestGenerationWithInvalidDocument()
        {
            // GIVEN launch arguments requesting the code generation with an invalid document path
            string[] args = { "--generate", "/" };
            string stdout, stderr;

            // WHEN the invocation is made
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.UnexpectedError, result);
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.UnexpectedError, result);
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.UnexpectedError, result);
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.UnexpectedError, result);
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.UnexpectedError, result);
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
            int result = Invoke(args, out stdout, out stderr);

            // THEN no error is returned on the argument parsing phase
            Assert.AreEqual(MainClass.ArgumentError, result);
            Assert.AreEqual("", stdout);
            Assert.AreEqual(MainClass.WrongArchitecture + "\n" + MainClass.HelpString + "\n", stderr);
        }
    }
}

