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

namespace Petri.Editor
{
    public abstract class PetriGen
    {
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

        protected PetriGen(HeadlessDocument doc, Language language, CodeGen generator)
        {
            CodeGen = generator;
            Document = doc;
            Language = language;
        }

        public Language Language {
            get;
            private set;
        }

        protected HeadlessDocument Document {
            get;
            private set;
        }

        protected CodeGen CodeGen {
            get;
            set;
        }

        protected abstract void Begin();

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
                throw new Exception("Imbossibru!");
            }
        }

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

        protected abstract void GenerateAction(Action a, IDManager lastID);

        protected abstract void GenerateExitPoint(ExitPoint e, IDManager lastID);

        protected abstract void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID);

        protected abstract void GenerateTransition(Transition t);


        protected abstract void End();

        protected void Format()
        {
            CodeGen.Format();
        }

        public string GetHash()
        {
            Begin();
            GenerateCodeFor(Document.PetriNet, new IDManager(Document.LastEntityID + 1));
            End();

            return Hash;
        }

        public virtual void WritePetriNet()
        {
            Begin();
            GenerateCodeFor(Document.PetriNet, new IDManager(Document.LastEntityID + 1));
            End();

            System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + "." + PetriGen.SourceExtensionFromLanguage(Language)), CodeGen.Value);
        }

        public abstract void WriteExpressionEvaluator(Cpp.Expression expression, string path);

        protected string PathToFile(string filename)
        {
            return System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath), filename);
        }


        protected string Hash {
            get;
            set;
        }
    }
}

