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
using Petri;
using Petri.Editor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Petri.Test.Editor.EditorController
{
    [TestFixture()]
    public class TestCloneSettings
    {
        HeadlessDocument _document;

        [TestFixtureSetUp()]
        public void FixtureSetUp()
        {
            _document = new HeadlessDocument("", DocumentSettings.GetDefaultSettings(_document));
        }

        static void AssertEquivalent(DocumentSettings s1, DocumentSettings s2)
        {
            Assert.NotNull(s1);
            Assert.NotNull(s2);

            Assert.True(TestUtility.InstancePropertiesEqual(s1,
                                                            s2,
                                                            "IncludePaths",
                                                            "LibPaths",
                                                            "Libs",
                                                            "CompilerFlags"));
            CollectionAssert.AreEquivalent(s1.IncludePaths, s2.IncludePaths);
            CollectionAssert.AreEquivalent(s1.LibPaths, s2.LibPaths);
            CollectionAssert.AreEquivalent(s1.Libs, s2.Libs);
            CollectionAssert.AreEquivalent(s1.CompilerFlags, s2.CompilerFlags);
        }

        [Test()]
        public void TestDefaultSettings()
        {
            // GIVEN a fresh document
            // WHEN we get the default settings for this document
            var defaultSettings = DocumentSettings.GetDefaultSettings(_document);

            // THEN the settings of the document are the same as the default settings.
            AssertEquivalent(_document.Settings, defaultSettings);
        }

        DocumentSettings GetRandomSettings()
        {
            var settings = DocumentSettings.GetDefaultSettings(_document);

            var random = new Random();
            settings.Compiler = CodeUtility.RandomIdentifier();
            int flagsCount = random.Next(10);
            for(int i = 0; i < flagsCount; ++i) {
                settings.CompilerFlags.Add("-" + CodeUtility.RandomIdentifier());
            }

            settings.Language = CodeUtility.RandomLanguage();

            bool defaultEnum = random.Next(2) != 0;
            if(defaultEnum) {
                settings.Enum = settings.DefaultEnum;
            }
            else {
                int enumCount = random.Next(1, 10) + 1;
                var enumValues = string.Join(",",
                                             Enumerable.Repeat((object)null, enumCount).Select(o => CodeUtility.RandomIdentifier()));

                settings.Enum = new Petri.Editor.Code.Enum(settings.Language, enumValues);
            }

            settings.Hostname = "localhost";
            settings.Port = (UInt16)random.Next(65536);

            settings.Name = CodeUtility.RandomIdentifier();

            settings.RunInEditor = random.Next(2) != 0;

            settings.RelativeSourceOutputPath = TestUtility.RandomPath();
            settings.RelativeLibOutputPath = TestUtility.RandomPath();

            int includePathsCount = random.Next(10);
            for(int i = 0; i < includePathsCount; ++i) {
                settings.IncludePaths.Add(Tuple.Create(TestUtility.RandomPath(), random.Next(2) == 0));
            }

            int libPathsCount = random.Next(10);
            for(int i = 0; i < libPathsCount; ++i) {
                settings.LibPaths.Add(Tuple.Create(TestUtility.RandomPath(), random.Next(2) == 0));
            }

            int libsCount = random.Next(10);
            for(int i = 0; i < libsCount; ++i) {
                settings.Libs.Add(CodeUtility.RandomIdentifier());
            }
                
            return settings;
        }

        [Test(), Repeat(10)]
        public void TestClone()
        {
            // GIVEN a fresh document and settings for this document with random values
            var randomSettings = GetRandomSettings();

            // WHEN we clone the document's settings
            var cloned = randomSettings.Clone();

            // THEN the cloned settings are the same as the document's.
            AssertEquivalent(randomSettings, cloned);
        }
    }
}

