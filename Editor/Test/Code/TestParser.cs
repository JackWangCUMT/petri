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
using Petri.Editor.Code;

namespace Petri.Test.Code
{
    [TestFixture()]
    public class TestParser
    {
        static void AssertIdentifier(string id) {
            var match = System.Text.RegularExpressions.Regex.Match(id, Parser.GetNamePattern(true));

            Assert.True(match.Success);
            Assert.AreEqual(id, match.Value);
        }

        [Test(), Repeat(20)]
        public void TestRandomIdentifier()
        {
            // GIVEN a random identifier
            string identifier = CodeUtility.RandomIdentifier();

            // WHEN we check if it is actually an identifier
            // THEN the identifier is matched and its value is kept as a whole
            AssertIdentifier(identifier);
        }

        [Test()]
        public void TestIdentifier1()
        {
            // GIVEN a given identifier
            string identifier = "a";

            // WHEN we check if it is actually an identifier
            // THEN the identifier is matched and its value is kept as a whole
            AssertIdentifier(identifier);
        }

        [Test()]
        public void TestIdentifier2()
        {
            // GIVEN a given identifier
            string identifier = "_a";

            // WHEN we check if it is actually an identifier
            // THEN the identifier is matched and its value is kept as a whole
            AssertIdentifier(identifier);
        }

        [Test()]
        public void TestIdentifier3()
        {
            // GIVEN a given identifier
            string identifier = "a3";

            // WHEN we check if it is actually an identifier
            // THEN the identifier is matched and its value is kept as a whole
            AssertIdentifier(identifier);
        }

        [Test()]
        public void TestIdentifier4()
        {
            // GIVEN a given identifier
            string identifier = "_a_A4246_bfuzokze_";

            // WHEN we check if it is actually an identifier
            // THEN the identifier is matched and its value is kept as a whole
            AssertIdentifier(identifier);
        }

        [Test()]
        public void TestNotIdentifier1()
        {
            // GIVEN a given string that is not an identifier
            string identifier = "1";

            // WHEN we check if it is actually an identifier
            var match = System.Text.RegularExpressions.Regex.Match(identifier, Parser.GetNamePattern(true));

            // THEN it is recognized as not being an identifier
            Assert.False(match.Success);
        }

        [Test()]
        public void TestNotIdentifier2()
        {
            // GIVEN a given string that is not an identifier
            string identifier = "1a";

            // WHEN we check if it is actually an identifier
            var match = System.Text.RegularExpressions.Regex.Match(identifier, Parser.GetNamePattern(true));

            // THEN it is recognized as not being an identifier
            Assert.False(match.Success);
        }

        [Test()]
        public void TestNotIdentifier3()
        {
            // GIVEN a given string that is not an identifier
            string identifier = "a:";

            // WHEN we check if it is actually an identifier
            var match = System.Text.RegularExpressions.Regex.Match(identifier, Parser.GetNamePattern(true));

            // THEN it is recognized as not being an identifier
            Assert.False(match.Success);
        }

        [Test()]
        public void TestNotIdentifier4()
        {
            // GIVEN a given string that is not an identifier
            string identifier = "a a";

            // WHEN we check if it is actually an identifier
            var match = System.Text.RegularExpressions.Regex.Match(identifier, Parser.GetNamePattern(true));

            // THEN it is recognized as not being an identifier
            Assert.False(match.Success);
        }
    }
}

