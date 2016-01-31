/*
 * Copyright (c) 2015 Rémi Saurel
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
using System.Collections.Generic;

namespace Petri.Editor
{
    /// <summary>
    /// A simple struct that represents a code range by its first and last lines.
    /// </summary>
    public struct CodeRange
    {
        public CodeRange(int first, int last)
        {
            FirstLine = first;
            LastLine = last;
        }

        /// <summary>
        /// Gets or sets the first line of the range.
        /// </summary>
        /// <value>The first line.</value>
        public int FirstLine {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the last line of the range.
        /// </summary>
        /// <value>The last line.</value>
        public int LastLine {
            get;
            set;
        }
    }

    public abstract class PetriGen
    {
        /// <summary>
        /// Creates a PetriGen instance from the given language and document
        /// </summary>
        /// <returns>The code generator from language.</returns>
        /// <param name="language">Language.</param>
        /// <param name="document">Document.</param>
        public static PetriGen PetriGenFromLanguage(Language language, HeadlessDocument document)
        {
            if(language == Language.Cpp) {
                return new CppPetriGen(document);
            }
            else if(language == Language.C) {
                return new CPetriGen(document);
            }
            else if(language == Language.CSharp) {
                return new CSharpPetriGen(document);
            }

            throw new Exception("Unsupported language: " + language);
        }

        /// <summary>
        /// Gets a the canonical source file extension for the provided language
        /// </summary>
        /// <returns>The extension from the language.</returns>
        /// <param name="language">Language.</param>
        public static string SourceExtensionFromLanguage(Language language)
        {
            if(language == Language.Cpp) {
                return "cpp";
            }
            else if(language == Language.C) {
                return "c";
            }
            else if(language == Language.CSharp) {
                return "cs";
            }

            throw new Exception("Unsupported language: " + language);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.PetriGen"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="language">Language.</param>
        /// <param name="generator">Generator.</param>
        protected PetriGen(HeadlessDocument doc, Language language, CodeGen generator)
        {
            CodeGen = generator;
            Document = doc;
            Language = language;
            CodeRanges = new Dictionary<Entity, CodeRange>();
        }

        /// <summary>
        /// Gets the language the petri net code generator has been configured with.
        /// </summary>
        /// <value>The language.</value>
        public Language Language {
            get;
            private set;
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <value>The document.</value>
        protected HeadlessDocument Document {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the underlying code generator.
        /// </summary>
        /// <value>The code gen.</value>
        protected CodeGen CodeGen {
            get;
            set;
        }

        /// <summary>
        /// Gets the code ranges.
        /// </summary>
        /// <value>The code ranges.</value>
        public Dictionary<Entity, CodeRange> CodeRanges {
            get;
            private set;
        }

        /// <summary>
        /// Called before any entity generation and gives the opportunity to emit some setup code.
        /// </summary>
        protected abstract void Begin();

        /// <summary>
        /// Generates the code for the given entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="lastID">Last ID.</param>
        protected void GenerateCodeFor(Entity entity, IDManager lastID)
        {
            if(entity is InnerPetriNet) {
                GeneratePetriNet((PetriNet)entity, lastID);
                GenerateInnerPetriNet((InnerPetriNet)entity, lastID);
            }
            else if(entity is PetriNet) {
                GeneratePetriNet((PetriNet)entity, lastID);
            }
            else if(entity is Action) {
                GenerateAction((Action)entity, lastID);
            }
            else if(entity is ExitPoint) {
                GenerateExitPoint((ExitPoint)entity, lastID);
            }
            else if(entity is Transition) {
                GenerateTransition((Transition)entity);
            }
            else {
                throw new Exception("Entity type " + entity + " not handled!");
            }
        }

        /// <summary>
        /// Generates the code for a petri net.
        /// </summary>
        /// <param name="pn">Petri net.</param>
        /// <param name="lastID">Last ID.</param>
        protected void GeneratePetriNet(PetriNet pn, IDManager lastID)
        {
            foreach(State s in pn.States) {
                GenerateCodeFor(s, lastID);
            }

            CodeGen += "\n";

            foreach(Transition t in pn.Transitions) {
                GenerateCodeFor(t, lastID);
            }
        }

        /// <summary>
        /// Generates the code for an action.
        /// </summary>
        /// <param name="a">Action.</param>
        /// <param name="lastID">Last ID.</param>
        protected abstract void GenerateAction(Action a, IDManager lastID);

        /// <summary>
        /// Generates the code for an exit point.
        /// </summary>
        /// <param name="e">ExitPoint.</param>
        /// <param name="lastID">Last ID.</param>
        protected abstract void GenerateExitPoint(ExitPoint e, IDManager lastID);

        /// <summary>
        /// Generates the code for an inner petri net.
        /// </summary>
        /// <param name="i">Inner petri net.</param>
        /// <param name="lastID">Last ID.</param>
        protected abstract void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID);

        /// <summary>
        /// Generates the code for a transition.
        /// </summary>
        /// <param name="t">Transition.</param>
        /// <param name="lastID">Last ID.</param>
        protected abstract void GenerateTransition(Transition t);

        /// <summary>
        /// Finishes the code generation and compute the Hash value of the petri net.
        /// </summary>
        protected abstract void End();

        /// <summary>
        /// Format this generated code.
        /// </summary>
        protected void Format()
        {
            CodeGen.Format();
        }

        /// <summary>
        /// Gets the hash of the petri net by generating the code and discarding it.
        /// </summary>
        /// <returns>The hash.</returns>
        public string GetHash()
        {
            Begin();
            GenerateCodeFor(Document.PetriNet, new IDManager(Document.IDManager));
            End();

            return Hash;
        }

        /// <summary>
        /// Generates the code for the petri net of the document.
        /// </summary>
        public virtual void WritePetriNet()
        {
            Begin();
            GenerateCodeFor(Document.PetriNet, new IDManager(Document.IDManager));
            End();

            System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + "." + PetriGen.SourceExtensionFromLanguage(Language)),
                                        CodeGen.Value);
        }

        /// <summary>
        /// Writes the expression evaluator code corresponding to the given expression to the given path.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <param name="path">Path.</param>
        /// <param name="userData">Additional and optional user data that will be used to generate the code.</param>
        public abstract void WriteExpressionEvaluator(Expression expression, string path, params object[] userData);

        /// <summary>
        /// Gets the absolute path of a file from its path relative to the document.
        /// </summary>
        /// <returns>The path to the file.</returns>
        /// <param name="filename">Filename.</param>
        protected string PathToFile(string filename)
        {
            return System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                                 Document.Settings.RelativeSourceOutputPath),
                                          filename);
        }

        /// <summary>
        /// Gets or sets a value indicating this instance hash.
        /// </summary>
        protected string Hash {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of the class name.
        /// </summary>
        /// <value>The name of the class.</value>
        protected string ClassName {
            get {
                return Document.Settings.Name;
            }
        }
    }
}

