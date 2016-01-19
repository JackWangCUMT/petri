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
using System.Collections.Generic;
using Petri.Editor.Cpp;
using System.Linq;

namespace Petri.Editor
{
    public class CSharpPetriGen : PetriGen
    {
        public CSharpPetriGen(HeadlessDocument doc) : base(doc,
                                                           Language.CSharp,
                                                           new CFamilyCodeGen(Language.CSharp))
        {
            _functionBodies = "";
        }

        public override void WritePetriNet()
        {
            base.WritePetriNet();
        }

        public override void WriteExpressionEvaluator(Expression expression, string path)
        {
            string cppExpr = expression.MakeCpp();

            CodeGen generator = new CFamilyCodeGen(Language.CSharp);
            /*foreach(var s in Document.Headers) {
                var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                s);
                var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                                           Document.Settings.SourceOutputPath));
                generator += "#include \"" + Configuration.GetRelativePath(p1, p2) + "\"";
            }*/

            /*generator += "#include \"Runtime/Cpp/Petri.h\"";
            generator += "#include \"Runtime/Cpp/Atomic.h\"";
            generator += "#include <string>";
            generator += "#include <sstream>";

            generator += "using namespace Petri;";

            generator += GenerateVarEnum();

            generator += "extern \"C\" char const *" + Document.CppPrefix + "_evaluate(void *petriPtr) {";
            generator += "auto &petriNet = *static_cast<PetriDebug *>(petriPtr);";
            generator += "static std::string result;";
            generator += "std::ostringstream oss;";
            generator += "oss << " + cppExpr + ";";
            generator += "result = oss.str();";
            generator += "return result.c_str();";
            generator += "}\n";*/

            System.IO.File.WriteAllText(path, generator.Value);
        }

        protected override void Begin()
        {
            CodeGen += "using System;";
            CodeGen += "using Petri.Runtime;";
            CodeGen += "using PNAction = Petri.Runtime.Action;";

            CodeGen.AddLine();

            CodeGen += GenerateVarEnum();

            CodeGen += "namespace Petri.Generated\n{";
            CodeGen += "public class " + CompilableClassName + " : Petri.Runtime.GeneratedDynamicLib\n{";

            CodeGen += "public " + CompilableClassName + "()";
            CodeGen += "{";
            CodeGen += "Lib = new DynamicLib(Create, CreateDebug, Hash, Name, Prefix, Port);";
            CodeGen += "}\n";

            CodeGen += "static void Populate(PetriNet petriNet) {";

            foreach(var e in Document.PetriNet.Variables) {
                CodeGen += "petriNet.AddVariable((UInt32)(" + e.Prefix + e.Expression + "));";
            }
        }

        protected override void End()
        {
            CodeGen += "}"; // Populate()

            CodeGen += _functionBodies;

            string toHash = CodeGen.Value;

            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
            Hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-",
                                                                                                              "");

            CodeGen += "";

            CodeGen += "static IntPtr Create() {";
            CodeGen += "var petriNet = new PetriNet(\"" + Document.Settings.Name + "\");";
            CodeGen += "Populate(petriNet);";
            CodeGen += "return petriNet.Release();";
            CodeGen += "}"; // create()

            CodeGen += "";

            CodeGen += "static IntPtr CreateDebug() {";
            CodeGen += "var petriNet = new PetriDebug(\"" + Document.Settings.Name + "\");";
            CodeGen += "Populate(petriNet);";
            CodeGen += "return petriNet.Release();";
            CodeGen += "}"; // create()

            CodeGen += "";

            CodeGen += "static string Hash() {";
            CodeGen += "return \"" + Hash + "\";";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "static string Prefix() {";
            CodeGen += "return \"" + Document.CppPrefix + "\";";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "static string Name() {";
            CodeGen += "return \"" + Document.CppPrefix + "\";";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "static UInt16 Port() {";
            CodeGen += "return " + Document.Settings.Port + ";";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "}"; // class
            CodeGen += "}"; // namespace

            CodeGen.Format();
        }

        protected override void GenerateAction(Action a, IDManager lastID)
        {
            var old = new Dictionary<LiteralExpression, string>();
            string enumName = Document.Settings.Enum.Name;

            var litterals = a.Function.GetLiterals();
            foreach(LiteralExpression le in litterals) {
                if(le.Expression == "$Name") {
                    old.Add(le, le.Expression);
                    le.Expression = "\"" + a.Name + "\"";
                }
                else if(le.Expression == "$ID") {
                    old.Add(le, le.Expression);
                    le.Expression = a.ID.ToString();
                }
                else {
                    foreach(string e in Document.Settings.Enum.Members) {
                        if(le.Expression == e) {
                            old.Add(le, le.Expression);
                            // TODO: use language specific enum scope
                            le.Expression = "(Int32)(" + enumName + "." + le.Expression + ")";
                        }
                    }
                }
            }

            var cpp = "(Int32)(" + a.Function.MakeCpp() + ")";

            var cppVar = new HashSet<VariableExpression>();
            a.GetVariables(cppVar);

            _functionBodies += "static Int32 " + a.CppName + "_invocation(PetriNet petriNet) {\nreturn " + cpp + ";\n}\n";

            string action = a.CppName + "_invocation";

            CodeGen += "var " + a.CppName + " = " + "new PNAction(" + a.ID.ToString() + ", \"" + a.Parent.Name + "_" + a.Name + "\", " + action + ", " + a.RequiredTokens.ToString() + ");";
            CodeGen += "petriNet.AddAction(" + a.CppName + ", " + ((a.Active && (a.Parent is RootPetriNet)) ? "true" : "false") + ");";
            foreach(var v in cppVar) {
                CodeGen += a.CppName + ".AddVariable(" + "(UInt32)(" + v.Prefix + v.Expression + "));";
            }

            foreach(var tup in old) {
                tup.Key.Expression = tup.Value;
            }
        }

        protected override void GenerateExitPoint(ExitPoint e, IDManager lastID)
        {
            CodeGen += "var " + e.CppName + " = new PNAction(" + e.ID.ToString() + ", \"" + e.Parent.Name + "_" + e.Name + "\", () => { return default(Int32); }, " + e.RequiredTokens.ToString() + ");";
            CodeGen += "petriNet.AddAction(" + e.CppName + ", false);";
        }

        protected override void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID)
        {
            string name = i.EntryPointName;

            // Adding an entry point
            CodeGen += "var " + name + " = new PNAction(" + i.EntryPointID + ", \"" + i.Name + "_Entry\", () => { return default(Int32); }, " + i.RequiredTokens.ToString() + ");";
            CodeGen += "petriNet.AddAction(" + name + ", " + (i.Active ? "true" : "false") + ");";
            // Adding a transition from the entry point to all of the initially active states
            foreach(State s in i.States) {
                if(s.Active) {
                    var newID = lastID.Consume();
                    string tName = name + "_" + newID.ToString();

                    CodeGen += name + ".AddTransition(" + newID.ToString() + ", \"" + tName + "\", " + s.CppName + ", (Int32 result) => { return true; });";
                }
            }
        }

        protected override void GenerateTransition(Transition t)
        {
            var old = new Dictionary<LiteralExpression, string>();
            string enumName = Document.Settings.Enum.Name;

            foreach(LiteralExpression le in t.Condition.GetLiterals()) {
                if(le.Expression == "$Res") {
                    old.Add(le, le.Expression);
                    le.Expression = "_PETRI_PRIVATE_GET_ACTION_RESULT_";
                }
                else if(le.Expression == "$Name") {
                    old.Add(le, le.Expression);
                    le.Expression = "\"" + t.Name + "\"";
                }
                else if(le.Expression == "$ID") {
                    old.Add(le, le.Expression);
                    le.Expression = t.ID.ToString();
                }
                else {
                    foreach(string e in Document.Settings.Enum.Members) {
                        if(le.Expression == e) {
                            old.Add(le, le.Expression);
                            le.Expression = "(Int32)(" + enumName + "." + le.Expression + ")";
                        }
                    }
                }
            }

            string bName = t.Before.CppName;
            string aName = t.After.CppName;

            var b = t.Before as InnerPetriNet;
            if(b != null) {
                bName = b.ExitPoint.CppName;
            }

            var a = t.After as InnerPetriNet;
            if(a != null) {
                aName = a.EntryPointName;
            }

            string cpp = "return " + t.Condition.MakeCpp() + ";";

            var cppVar = new HashSet<VariableExpression>();
            t.GetVariables(cppVar);

            _functionBodies += "static bool " + t.CppName + "_invocation(Int32 _PETRI_PRIVATE_GET_ACTION_RESULT_) {\n" + cpp + "\n}\n";

            cpp = t.CppName + "_invocation";

            CodeGen += bName + ".AddTransition(" + t.ID.ToString() + ", \"" + t.Name + "\", " + aName + ", " + cpp + ");";
            foreach(var v in cppVar) {
                CodeGen += t.CppName + ".AddVariable(" + "(UInt32)(" + v.Prefix + v.Expression + "));";
            }

            foreach(var tup in old) {
                tup.Key.Expression = tup.Value;
            }
        }

        protected string GenerateVarEnum()
        {
            var variables = Document.PetriNet.Variables;
            var cppVar = from v in variables
                                  select v.Expression;
            if(variables.Count > 0) {
                return "enum " + VariableExpression.EnumName + " {" + String.Join(", ", cppVar) + "};\n";
            }

            return "";
        }

        private string _functionBodies;
    }
}

