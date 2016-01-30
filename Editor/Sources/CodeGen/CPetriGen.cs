﻿/*
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
using System.Collections.Generic;
using Petri.Editor.Code;
using System.Linq;

namespace Petri.Editor
{
    public class CPetriGen : PetriGen
    {
        public CPetriGen(HeadlessDocument doc) : base(doc,
                                                      Language.C,
                                                      new CFamilyCodeGen(Language.C))
        {
            _headerGen = new CFamilyCodeGen(Language.C);
            _functionBodies = new CFamilyCodeGen(Language.C);
            _functionPrototypes = new CFamilyCodeGen(Language.C);
            _prototypesIndex = 0;
        }

        public override void WritePetriNet()
        {
            base.WritePetriNet();

            if(_generateHeader) {
                System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + ".h"),
                                            _headerGen.Value);
            }
        }

        public override void WriteExpressionEvaluator(Expression expression, string path)
        {
			// TODO: tbd
            throw new Exception("Not implemented yet!");
        }

        protected override void Begin()
        {
            CodeGen += "/*";
            CodeGen += " * Generated by the petri net editor - https://github.com/rems4e/petri";
            CodeGen += " * Version " + DebugClient.Version;
            CodeGen += " */";
            CodeGen.AddLine();

            CodeGen += "#include <stdint.h>";
            CodeGen += "#include <stdbool.h>";
            CodeGen += "#include <math.h>";
            CodeGen += "#include <time.h>";
            CodeGen += "#include \"Runtime/C/PetriUtils.h\"";
            CodeGen += "#include \"Runtime/C/Action.h\"";
            foreach(var s in Document.Headers) {
                var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                s);
                var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                                           Document.Settings.RelativeSourceOutputPath));
                CodeGen += "#include \"" + Configuration.GetRelativePath(p1, p2) + "\"";
            }

            CodeGen.AddLine();

            CodeGen += "#include \"" + Document.Settings.Name + ".h\"";
            CodeGen += "";
            CodeGen += "#define EXPORT extern";
            CodeGen += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"";

            CodeGen += GenerateVarEnum();

            _prototypesIndex = CodeGen.Value.Length;

            CodeGen += "static void fill(struct PetriNet *petriNet) {";

            foreach(var e in Document.PetriNet.Variables) {
                CodeGen += "PetriNet_addVariable(petriNet, (uint_fast32_t)" + e.Prefix + e.Expression + ");";
            }
        }

        protected override void End()
        {
            CodeGen.Value = CodeGen.Value.Substring(0, _prototypesIndex) + _functionPrototypes.Value + "\n" + CodeGen.Value.Substring(_prototypesIndex);

            CodeGen += "}\n"; // fill()

            int linesSoFar = CodeGen.LineCount;
            var keys = new List<Entity>(CodeRanges.Keys);
            foreach(var key in keys) {
                var range = CodeRanges[key];
                range.FirstLine += linesSoFar;
                range.LastLine += linesSoFar;
                CodeRanges[key] = range;
            }

            CodeGen += _functionBodies.Value;

            string toHash = CodeGen.Value;

            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
            Hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-",
                                                                                                              "");

            CodeGen += "EXPORT void *" + Document.Settings.Name + "_create() {";
            CodeGen += "struct PetriNet *petriNet = PetriNet_create(PETRI_PREFIX);";
            CodeGen += "fill(petriNet);";
            CodeGen += "return petriNet;";
            CodeGen += "}"; // create()

            CodeGen += "";

            CodeGen += "EXPORT void *" + Document.Settings.Name + "_createDebug() {";
            CodeGen += "struct PetriNet *petriNet = PetriNet_createDebug(PETRI_PREFIX);";
            CodeGen += "fill(petriNet);";
            CodeGen += "return petriNet;";
            CodeGen += "}"; // create()

            CodeGen += "";

            CodeGen += "EXPORT char const *" + Document.Settings.Name + "_getHash() {";
            CodeGen += "return \"" + Hash + "\";";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "EXPORT char const *" + Document.Settings.Name + "_getAPIDate() {";
            CodeGen += "return __TIMESTAMP__;";
            CodeGen += "}";

            CodeGen += "";

            CodeGen += "EXPORT struct PetriDynamicLib *" + Document.Settings.Name + "_createLibForEditor() {";
            CodeGen += "return PetriDynamicLib_create(\"" + Document.CodePrefix + "\", \"" + Document.CodePrefix + "\", " + Document.Settings.Port + ");";
            CodeGen += "}";

            CodeGen.Format();

            _headerGen += "/*";
            _headerGen += " * Generated by the petri net editor - https://github.com/rems4e/petri";
            _headerGen += " * Version " + DebugClient.Version;
            _headerGen += " */";
            _headerGen.AddLine();

            _headerGen += "#ifndef PETRI_GENERATED_" + Document.Settings.Name + "_H";
            _headerGen += "#define PETRI_GENERATED_" + Document.Settings.Name + "_H";

            _headerGen += "";
            _headerGen += "#include \"Runtime/C/PetriDynamicLib.h\"";
            _headerGen += "";

            _headerGen += "#ifdef __cplusplus";
            _headerGen += "extern \"C\" {";
            _headerGen += "#endif";
            _headerGen += "";
            _headerGen += "inline struct PetriDynamicLib *" + Document.Settings.Name + "_createLib() {";
            _headerGen += "return PetriDynamicLib_create(\"" + Document.CodePrefix + "\", \"" + Document.CodePrefix + "\", "
            + Document.Settings.Port + ");";
            _headerGen += "}";
            _headerGen += "";
            _headerGen += "#ifdef __cplusplus";
            _headerGen += "}";
            _headerGen += "#endif";
            _headerGen += "";
            _headerGen += "#endif"; // ifndef header guard

            _headerGen.Format();

            string path = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName,
                                                                        Document.Settings.RelativeSourceOutputPath),
                                                 Document.Settings.Name) + ".h";
            string headerCode = _headerGen.Value;
            if(System.IO.File.Exists(path)) {
                string existing = System.IO.File.ReadAllText(path);
                if(existing.Length > 1 && existing.Substring(0, existing.Length - 1) == headerCode) {
                    _generateHeader = false;
                }
            }
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
                            le.Expression = "(Petri_actionResult_t>)" + enumName + "_" + le.Expression;
                        }
                    }
                }
            }

            var cppVar = new HashSet<VariableExpression>();
            a.GetVariables(cppVar);

            _functionPrototypes += "static Petri_actionResult_t " + a.CodeIdentifier + "_invocation(struct PetriNet *);";

            CodeRange range = new CodeRange();
            range.FirstLine = _functionBodies.LineCount;

            _functionBodies += "static Petri_actionResult_t " + a.CodeIdentifier + "_invocation(struct PetriNet *petriNet) {";

            if(a.Function.NeedsReturn) {
                _functionBodies += a.Function.MakeCode();
            }
            else {
                _functionBodies += Document.Settings.Enum.Name + " result = (Petri_actionResult_t)(" + a.Function.MakeCode() + ")" + ";";
            }
			
            if(a.Function.NeedsReturn) {
                _functionBodies += "return PetriUtility_returnDefault();";
            }
            else {
                _functionBodies += "return result;";
            }

            _functionBodies += "}\n";

            range.LastLine = _functionBodies.LineCount;

            CodeRanges[a] = range;

            CodeGen += "struct PetriAction *" + a.CodeIdentifier + " = PetriAction_createWithParam(" + a.ID.ToString() + ", \""
            + a.Parent.Name + "_" + a.Name + "\", &" + a.CodeIdentifier + "_invocation, " + a.RequiredTokens.ToString() + ");";
            CodeGen += "PetriNet_addAction(petriNet, " + a.CodeIdentifier + ", " + ((a.Active && (a.Parent is RootPetriNet)) ? "true" : "false") + ");";

            foreach(var tup in old) {
                tup.Key.Expression = tup.Value;
            }
        }

        protected override void GenerateExitPoint(ExitPoint e, IDManager lastID)
        {
            CodeGen += "struct PetriAction *" + e.CodeIdentifier + " = PetriAction_create(" + e.ID.ToString() + ", \""
            + e.Parent.Name + "_" + e.Name + "\", &PetriUtility_returnDefault, " + e.RequiredTokens.ToString() + ");";
            CodeGen += "PetriNet_addAction(petriNet, " + e.CodeIdentifier + ", false);";
        }

        protected override void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID)
        {
            string name = i.EntryPointName;

            // Adding an entry point
            CodeGen += "struct PetriAction *" + name + " = PetriAction_create(" + i.EntryPointID + ", \"" + i.Name + "_Entry\", &PetriUtility_returnDefault, " + i.RequiredTokens.ToString() + ");";
            CodeGen += "PetriNet_addAction(petriNet, " + name + ", " + (i.Active ? "true" : "false") + ");";

            // Adding a transition from the entry point to all of the initially active states
            foreach(State s in i.States) {
                if(s.Active) {
                    var newID = lastID.Consume();
                    string tName = name + "_" + newID.ToString();

                    CodeGen += "PetriAction_addTransition(" + name + ", " + newID.ToString()
                    + ", \"" + tName + "\", " + s.CodeIdentifier + ", &PetriUtility_returnTrue);";
                }
            }
        }

        protected override void GenerateTransition(Transition t)
        {
            var old = new Dictionary<LiteralExpression, string>();
            string enumName = Document.Settings.Enum.Name;

            foreach(LiteralExpression le in t.Condition.GetLiterals()) {
                if(le.Expression == "$Res" || le.Expression == "$Result") {
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
                            le.Expression = "(Petri_actionResult_t)(" + enumName + "_" + le.Expression + ")";
                        }
                    }
                }
            }

            string bName = t.Before.CodeIdentifier;
            string aName = t.After.CodeIdentifier;

            var b = t.Before as InnerPetriNet;
            if(b != null) {
                bName = b.ExitPoint.CodeIdentifier;
            }

            var a = t.After as InnerPetriNet;
            if(a != null) {
                aName = a.EntryPointName;
            }

            var cppVar = new HashSet<VariableExpression>();
            t.GetVariables(cppVar);

            _functionPrototypes += "static bool " + t.CodeIdentifier + "_invocation(Petri_actionResult_t);";

            CodeRange range = new CodeRange();
            range.FirstLine = _functionBodies.LineCount;
            _functionBodies += "static bool " + t.CodeIdentifier + "_invocation(Petri_actionResult_t _PETRI_PRIVATE_GET_ACTION_RESULT_) {";

            if(t.Condition.NeedsReturn) {
                _functionBodies += t.Condition.MakeCode();
            }
            else {
                _functionBodies += "bool result = " + t.Condition.MakeCode() + ";";
            }

            if(t.Condition.NeedsReturn) {
                _functionBodies += "return true;";
            }
            else {
                _functionBodies += "return result;";
            }

            _functionBodies += "}\n";

            range.LastLine = _functionBodies.LineCount;

            CodeRanges[t] = range;

            CodeGen += "PetriAction_addTransition(" + bName + ", " + t.ID.ToString() + ", \"" + t.Name + "\", " + aName + ", "
            + "&" + t.CodeIdentifier + "_invocation" + ");";

            foreach(var tup in old) {
                tup.Key.Expression = tup.Value;
            }
        }

        protected string GenerateVarEnum()
        {
            var variables = Document.PetriNet.Variables;
            var cppVar = from v in variables
                                  select v.Prefix + v.Expression;
            if(variables.Count > 0) {
                return "enum " + VariableExpression.EnumName + " {" + String.Join(", ", cppVar) + "};\n";
            }

            return "";
        }

        private CodeGen _headerGen;
        private CodeGen _functionBodies;
        private CodeGen _functionPrototypes;
        private bool _generateHeader = true;
        private int _prototypesIndex;
    }
}

