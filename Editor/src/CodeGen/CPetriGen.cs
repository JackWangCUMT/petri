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
using System.Collections.Generic;
using Petri.Cpp;
using System.Linq;

namespace Petri
{
	public class CPetriGen : PetriGen
	{
		public CPetriGen(HeadlessDocument doc) : base(doc, Language.C, new CFamilyCodeGen(Language.Cpp)) {
			_headerGen = new CFamilyCodeGen(Language.C);
			_functionBodies = "";
			_functionPrototypes = "";
			_prototypesIndex = 0;
		}

		public override void WritePetriNet() {
			base.WritePetriNet();

			if(_generateHeader) {
				System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + ".h"), _headerGen.Value);
			}
		}

		public override void WriteExpressionEvaluator(Cpp.Expression expression, string path) {
			
		}

		protected override void Begin() {
			CodeGen += "#include <stdint.h>";
			CodeGen += "#include <stdbool.h>";
			CodeGen += "#include <math.h>";
			CodeGen += "#include <time.h>";
			CodeGen += "#include \"Runtime/C/PetriUtils.h\"";
			CodeGen += "#include \"Runtime/C/Action.h\"";
			foreach(var s in Document.Headers) {
				var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, s);
				var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath));
				CodeGen += "#include \"" + Configuration.GetRelativePath(p1, p2) + "\"";
			}

			CodeGen.AddLine();

			CodeGen += "#include \"" + Document.Settings.Name + ".h\"";
			CodeGen += "";
			CodeGen += "#define EXPORT extern";
			CodeGen += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"\n";

			CodeGen += GenerateVarEnum();

			_prototypesIndex = CodeGen.Value.Length;

			CodeGen += "static void fill(PetriNet *petriNet) {";

			foreach(var e in Document.PetriNet.Variables) {
				CodeGen += "PetriNet_addVariable(petriNet, (uint_fast32_t)" + e.Prefix + e.Expression + ");";
			}
		}

		protected override void End() {
			CodeGen.Value = CodeGen.Value.Substring(0, _prototypesIndex) + _functionPrototypes + "\n" + CodeGen.Value.Substring(_prototypesIndex);

			CodeGen += "}\n"; // fill()

			CodeGen += _functionBodies;

			string toHash = CodeGen.Value;

			System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
			Hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-", "");

			CodeGen += "EXPORT void *" + Document.Settings.Name + "_create() {";
			CodeGen += "PetriNet *petriNet = PetriNet_create(PETRI_PREFIX);";
			CodeGen += "fill(petriNet);";
			CodeGen += "return petriNet;";
			CodeGen += "}"; // create()

			CodeGen += "";

			CodeGen += "EXPORT void *" + Document.Settings.Name + "_createDebug() {";
			CodeGen += "PetriNet *petriNet = PetriNet_createDebug(PETRI_PREFIX);";
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

			CodeGen.Format();

			_headerGen += "#ifndef PETRI_" + Document.Settings.Name + "_H";
			_headerGen += "#define PETRI_" + Document.Settings.Name + "_H";

			_headerGen += "";
			_headerGen += "#include \"Runtime/C/PetriDynamicLib.h\"";
			_headerGen += "";

			_headerGen += "#ifdef __cplusplus";
			_headerGen += "extern \"C\" {";
			_headerGen += "#endif";
			_headerGen += "";
			_headerGen += "inline PetriDynamicLib *" + Document.Settings.Name + "_createLib() {";
			_headerGen += "\treturn PetriDynamicLib_create(\"" + Document.CppPrefix + "\", \"" + Document.CppPrefix + "\", "
				+ Document.Settings.Port + ");";
			_headerGen += "}";
			_headerGen += "";
			_headerGen += "#ifdef __cplusplus";
			_headerGen += "}";
			_headerGen += "#endif";
			_headerGen += "";
			_headerGen += "#endif"; // ifndef header guard

			_headerGen.Format();

			string path = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath), Document.Settings.Name) + ".h";
			string headerCode = _headerGen.Value;
			if(System.IO.File.Exists(path)) {
				string existing = System.IO.File.ReadAllText(path);
				if(existing.Length > 1 && existing.Substring(0, existing.Length - 1) == headerCode) {
					_generateHeader = false;
				}
			}
		}

		protected override void GenerateAction(Action a, IDManager lastID) {
			var old = new Dictionary<Cpp.LiteralExpression, string>();
			string enumName = Document.Settings.Enum.Name;

			var litterals = a.Function.GetLiterals();
			foreach(Cpp.LiteralExpression le in litterals) {
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

			var cppVar = new HashSet<Cpp.VariableExpression>();
			a.GetVariables(cppVar);

			_functionPrototypes += "static Petri_actionResult_t " + a.CppName + "_invocation(PetriNet *);\n";
			_functionBodies += "static Petri_actionResult_t " + a.CppName + "_invocation(PetriNet *petriNet) {\n";

			string lockString = "", unlockString = "";

			foreach(var v in cppVar) {
				lockString += "PetriNet_lockVariable(petriNet, (uint_fast32_t)(" + v.Prefix + v.Expression + "));\n";
				unlockString += "PetriNet_unlockVariable(petriNet, (uint_fast32_t)(" + v.Prefix + v.Expression + "));\n";
			}

			_functionBodies += lockString;
			if(a.Function.NeedsReturn) {
				_functionBodies += a.Function.MakeCpp() + "\n";
			}
			else {
				_functionBodies += Document.Settings.Enum.Name + " result = (Petri_actionResult_t)(" + a.Function.MakeCpp() + ")" + ";\n";
			}

			_functionBodies += unlockString;

			if(a.Function.NeedsReturn) {
				_functionBodies += "return PetriUtility_returnDefault();\n";
			}
			else {
				_functionBodies += "return result;\n";
			}

			_functionBodies += "}\n\n";

			CodeGen += "PetriAction *" + a.CppName + " = PetriAction_create(" + a.ID.ToString() + ", \""
				+ a.Parent.Name + "_" + a.Name + "\", &" + a.CppName + "_invocation, " + a.RequiredTokens.ToString() + ");";
			CodeGen += "PetriNet_addAction(petriNet, " + a.CppName + ", " + ((a.Active && (a.Parent is RootPetriNet)) ? "true" : "false") + ");";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}
		}

		protected override void GenerateExitPoint(ExitPoint e, IDManager lastID) {
			CodeGen += "PetriAction *" + e.CppName + " = PetriAction_create(" + e.ID.ToString() + ", \""
				+ e.Parent.Name + "_" + e.Name + "\", &PetriUtility_returnDefault, " + e.RequiredTokens.ToString() + ");";
			CodeGen += "PetriNet_addAction(petriNet, " + e.CppName + ", false);";
		}

		protected override void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID) {
			string name = i.EntryPointName;

			// Adding an entry point
			CodeGen += "PetriAction *" + name + " = PetriAction_create(" + i.EntryPointID + ", \"" + i.Name + "_Entry\", &PetriUtility_returnDefault, " + i.RequiredTokens.ToString() + ");";
			CodeGen += "PetriNet_addAction(petriNet, " + name + ", " + (i.Active ? "true" : "false") + ");";

			// Adding a transition from the entry point to all of the initially active states
			foreach(State s in i.States) {
				if(s.Active) {
					var newID = lastID.Consume();
					string tName = name + "_" + newID.ToString();

					CodeGen += "PetriAction_createAndAddTransition(" + name + ", " + newID.ToString()
						+ ", \"" + tName + "\", " + s.CppName + ", &PetriUtility_returnTrue);";
				}
			}
		}

		protected override void GenerateTransition(Transition t) {
			var old = new Dictionary<Cpp.LiteralExpression, string>();
			string enumName = Document.Settings.Enum.Name;

			foreach(Cpp.LiteralExpression le in t.Condition.GetLiterals()) {
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
							le.Expression = "(Petri_actionResult_t)(" + enumName + "_" + le.Expression + ")";
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

			var cppVar = new HashSet<Cpp.VariableExpression>();
			t.GetVariables(cppVar);

			_functionPrototypes += "static bool " + t.CppName + "_invocation(Petri_actionResult_t);\n";
			_functionBodies += "static bool " + t.CppName + "_invocation(Petri_actionResult_t _PETRI_PRIVATE_GET_ACTION_RESULT_) {\n";
			string lockString = "", unlockString = "";

			foreach(var v in cppVar) {
				lockString += "PetriNet_lockVariable(petriNet, (uint_fast32_t)(" + v.Prefix + v.Expression + "));\n";
				unlockString += "PetriNet_unlockVariable(petriNet, (uint_fast32_t)(" + v.Prefix + v.Expression + "));\n";
			}

			_functionBodies += lockString;
			if(t.Condition.NeedsReturn) {
				_functionBodies += t.Condition.MakeCpp();
			}
			else {
				_functionBodies += "bool result = " + t.Condition.MakeCpp() + ";\n";
			}

			_functionBodies += unlockString + "\n";

			if(t.Condition.NeedsReturn) {
				_functionBodies += "return true;\n";
			}
			else {
				_functionBodies += "return result;\n";
			}

			_functionBodies += "}\n\n";

			CodeGen += "PetriAction_createAndAddTransition(" + bName + ", " + t.ID.ToString() + ", \"" + t.Name + "\", " + aName + ", "
				+ "&" + t.CppName + "_invocation" + ");";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}
		}

		protected string GenerateVarEnum() {
			var variables = Document.PetriNet.Variables;
			var cppVar = from v in variables
				select v.Prefix + v.Expression;
			if(variables.Count > 0) {
				return "enum " + Cpp.VariableExpression.EnumName + " {" + String.Join(", ", cppVar) + "};\n";
			}

			return "";
		}

		private CodeGen _headerGen;
		private string _functionBodies;
		private string _functionPrototypes;
		private bool _generateHeader = true;
		private int _prototypesIndex;
	}
}

