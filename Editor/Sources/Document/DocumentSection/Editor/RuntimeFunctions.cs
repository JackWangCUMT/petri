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

using System;
using Petri.Editor.Code;

namespace Petri.Editor
{
    public class RuntimeFunctions
    {
        /// <summary>
        /// Gets the function that pretty prints the petri net action at runtime.
        /// </summary>
        /// <returns>The print function.</returns>
        public static Function PrintFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;

            if(lang == Language.Cpp) {
                var f = new Code.Function(doc.Settings.Enum.Type,
                                          Scope.MakeFromNamespace(lang, "Utility"),
                                          "printAction",
                                          false);
                f.AddParam(new Param(new Code.Type(lang, "std::string const &"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "std::uint64_t"), "id"));

                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type,
                                     null,
                                     "PetriUtility_printAction",
                                     false);
                f.AddParam(new Param(new Code.Type(lang, "char const *"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "uint64_t"), "id"));

                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type,
                                     Scope.MakeFromNamespace(lang,
                                                             "Petri.Runtime.Utility"),
                                     "PrintAction",
                                     false);
                f.AddParam(new Param(new Code.Type(lang, "string"), "name"));
                f.AddParam(new Param(new Code.Type(lang, "UInt64"), "id"));

                return f;
            }

            throw new Exception("Action.PrintFunction: Should not get there !");
        }

        /// <summary>
        /// Gets the function that does nothing at runtime.
        /// </summary>
        /// <returns>The do nothing function.</returns>
        public static Function DoNothingFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;
            if(lang == Language.Cpp) {
                var f = new Function(doc.Settings.Enum.Type,
                                     Scope.MakeFromNamespace(lang,
                                                             "Utility"),
                                     "doNothing",
                                     false);
                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type, null, "PetriUtility_doNothing", false);
                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type,
                                     Scope.MakeFromNamespace(lang,
                                                             "Petri.Runtime.Utility"),
                                     "DoNothing",
                                     false);
                return f;
            }

            throw new Exception("Action.DoNothingFunction: Should not get there !");
        }

        /// <summary>
        /// Gets the function that make the calling thread to pause for the requested amount of time.
        /// </summary>
        /// <returns>The pause function.</returns>
        public static Function PauseFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;

            if(lang == Language.Cpp) {
                var f = new Function(doc.Settings.Enum.Type,
                                     Scope.MakeFromNamespace(lang, "Utility"),
                                     "pause", false);
                f.AddParam(new Param(new Code.Type(lang, "std::chrono::nanoseconds"), "delay"));
                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(doc.Settings.Enum.Type, null, "PetriUtility_pause", false);
                f.AddParam(new Param(new Code.Type(lang, "uint64_t"), "delayMicroSeconds"));
                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(doc.Settings.Enum.Type,
                                     Scope.MakeFromNamespace(lang, "Petri.Runtime.Utility"),
                                     "Pause", false);
                f.AddParam(new Param(new Code.Type(lang, "double"), "delayInSeconds"));
                return f;
            }

            throw new Exception("Action.PauseFunction: Should not get there !");
        }

        /// <summary>
        /// Gets the function that does nothing at runtime.
        /// </summary>
        /// <returns>The do nothing function.</returns>
        public static Function RandomFunction(HeadlessDocument doc)
        {
            var lang = doc.Settings.Language;
            if(lang == Language.Cpp) {
                var f = new Function(new Code.Type(lang, "int64_t"),
                                     Scope.MakeFromNamespace(lang,
                                                             "Utility"),
                                     "random",
                                     false);
                f.AddParam(new Param(new Code.Type(lang, "int64_t"), "lowerBound"));
                f.AddParam(new Param(new Code.Type(lang, "int64_t"), "uppererBound"));
                return f;
            }
            else if(lang == Language.C) {
                var f = new Function(new Code.Type(lang, "int64_t"), null, "random", false);
                f.AddParam(new Param(new Code.Type(lang, "int64_t"), "lowerBound"));
                f.AddParam(new Param(new Code.Type(lang, "int64_t"), "uppererBound"));
                return f;
            }
            else if(lang == Language.CSharp) {
                var f = new Function(new Code.Type(lang, "Int64"),
                                     Scope.MakeFromNamespace(lang,
                                                             "Petri.Runtime.Utility"),
                                     "random",
                                     false);
                f.AddParam(new Param(new Code.Type(lang, "Int64"), "lowerBound"));
                f.AddParam(new Param(new Code.Type(lang, "Int64"), "uppererBound"));
                return f;
            }

            throw new Exception("Action.DoNothingFunction: Should not get there !");
        }
    }
}

