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
	public class CppPetriGen : PetriGen
	{
		public CppPetriGen(HeadlessDocument doc) : base(doc, new CFamilyCodeGen(Language.Cpp)) {
			_headerGen = new CFamilyCodeGen(Language.Cpp);	
		}

		public override string Extension {
			get {
				return "cpp";
			}
		}

		public override void Write() {
			base.Write();

			if(_generateHeader) {
				System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + ".h"), _headerGen.Value);
			}
		}

		protected override void Begin() {
			CodeGen += "#include <cstdint>";
			CodeGen += "#include \"Runtime/PetriDebug.h\"";
			CodeGen += "#include \"Runtime/PetriUtils.h\"";
			CodeGen += "#include \"Runtime/Action.h\"";
			CodeGen += "#include \"Runtime/Atomic.h\"";
			foreach(var s in Document.Headers) {
				var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, s);
				var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath));
				CodeGen += "#include \"" + Configuration.GetRelativePath(p1, p2) + "\"";
			}

			CodeGen.AddLine();

			CodeGen += "#include \"" + Document.Settings.Name + ".h\"";

			CodeGen += "#define EXPORT extern \"C\"";
			CodeGen += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"\n";

			CodeGen += "\nusing namespace Petri;\n";

			var variables = Document.PetriNet.Variables;
			var cppVar = from v in variables
				select v.Expression;

			CodeGen += Document.GenerateVarEnum();

			CodeGen += "namespace {";
			CodeGen += "void fill(PetriNet &petriNet) {";

			foreach(var e in cppVar) {
				CodeGen += "petriNet.addVariable(static_cast<std::uint_fast32_t>(Petri_Var_Enum::" + e + "));";
			}
		}

		protected override void GenerateCodeFor(Entity entity, IDManager lastID) {
			if(entity is InnerPetriNet) {
				GenerateInnerPetriNet((InnerPetriNet)entity, lastID);
			}
			else if(entity is PetriNet) {
				base.GenerateCodeFor(entity, lastID);
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
				throw new Exception("Should not get there…");
			}
		}
			
		protected override void End() {
			CodeGen += "}"; // fill()
			CodeGen += "}"; // namespace

			CodeGen.Format();

			string toHash = CodeGen.Value;

			System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
			Hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-", "");

			CodeGen += "";

			CodeGen += "EXPORT void *" + Document.Settings.Name + "_create() {";
			CodeGen += "auto petriNet = std::make_unique<PetriNet>(PETRI_PREFIX);";
			CodeGen += "fill(*petriNet);";
			CodeGen += "return petriNet.release();";
			CodeGen += "}"; // create()

			CodeGen += "";

			CodeGen += "EXPORT void *" + Document.Settings.Name + "_createDebug() {";
			CodeGen += "auto petriNet = std::make_unique<PetriDebug>(PETRI_PREFIX);";
			CodeGen += "fill(*petriNet);";
			CodeGen += "return petriNet.release();";
			CodeGen += "}"; // create()

			CodeGen += "";

			CodeGen += "EXPORT char const *" + Document.Settings.Name + "_getHash() {";
			CodeGen += "return \"" + Hash + "\";";
			CodeGen += "}";

			CodeGen += "";

			CodeGen += "EXPORT char const *" + Document.Settings.Name + "_getAPIDate() {";
			CodeGen += "return __TIMESTAMP__;";
			CodeGen += "}";

			_headerGen += "#ifndef PETRI_" + Document.Settings.Name + "_H";
			_headerGen += "#define PETRI_" + Document.Settings.Name + "_H\n";

			_headerGen += "#define PETRI_CLASS_NAME " + Document.Settings.Name;
			_headerGen += "#define PETRI_PREFIX \"" + Document.CppPrefix + "\"";
			_headerGen += "#define PETRI_ENUM " + Document.Settings.Enum.Name;
			_headerGen += "#define PETRI_PORT " + Document.Settings.Port;

			_headerGen += "";

			_headerGen += "#include \"Runtime/PetriDynamicLib.h\"\n";

			_headerGen += "#undef PETRI_PORT";

			_headerGen += "#undef PETRI_ENUM";
			_headerGen += "#undef PETRI_PREFIX";
			_headerGen += "#undef PETRI_CLASS_NAME\n";

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

		private void GenerateAction(Action a, IDManager lastID) {
			var old = new Dictionary<Cpp.LiteralExpression, string>();
			string enumName = Document.Settings.Enum.Name;

			var litterals = a.Function.GetLiterals();
			foreach(Cpp.LiteralExpression le in litterals) {
				foreach(string e in Document.Settings.Enum.Members) {
					if(le.Expression == e) {
						old.Add(le, le.Expression);
						le.Expression = "static_cast<actionResult_t>(" + enumName + "::" + le.Expression + ")";
					}
					else if(le.Expression == "$Name") {
						old.Add(le, le.Expression);
						le.Expression = "\"" + a.Name + "\"";
					}
					else if(le.Expression == "$ID") {
						old.Add(le, le.Expression);
						le.Expression = a.ID.ToString();
					}
				}
			}

			var cpp = "static_cast<actionResult_t>(" + a.Function.MakeCpp() + ")";

			var cppVar = new HashSet<Cpp.VariableExpression>();
			a.GetVariables(cppVar);

			string action;

			if(cppVar.Count == 0) {
				action = "make_action_callable([&petriNet]() { return " + cpp + "; })";
			}
			else {
				var cppLockLock = from v in cppVar
					select "_petri_lock_" + v.Expression;

				action = "make_action_callable([&petriNet]() {\n";
				foreach(var v in cppVar) {
					action += "auto _petri_lock_" + v.Expression + " = petriNet.getVariable(static_cast<std::uint_fast32_t>(Petri_Var_Enum::" + v.Expression + ")).getLock();\n";
				}

				if(cppVar.Count > 1)
					action += "std::lock(" + String.Join(", ", cppLockLock) + ");\n";
				else {
					action += String.Join(", ", cppLockLock) + ".lock();\n";
				}

				action += "return " + cpp + ";\n";
				action += "})";
			}

			CodeGen += "auto &" + a.CppName + " = " + "petriNet.addAction("
				+ "Action(" + a.ID.ToString() + ", \"" + a.Parent.Name + "_" + a.Name + "\", " + action + ", " + a.RequiredTokens.ToString() + "), " + ((a.Active && (a.Parent is RootPetriNet)) ? "true" : "false") + ");";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}
		}

		private void GenerateExitPoint(ExitPoint e, IDManager lastID) {
			CodeGen += "auto &" + e.CppName + " = petriNet.addAction(" +
				"Action(" + e.ID.ToString() + ", \"" + e.Parent.Name + "_" + e.Name + "\", make_action_callable([](){ return actionResult_t(); }), "  + e.RequiredTokens.ToString()
				+ "), false);";
		}

		private void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID) {
			string name = i.EntryPointName;

			// Adding an entry point
			CodeGen += "auto &" + name + " = petriNet.addAction("
				+ "Action(" + i.EntryPointID + ", \"" + i.Name + "_Entry\", make_action_callable([](){ return actionResult_t(); }), " + i.RequiredTokens.ToString() + "), " + (i.Active ? "true" : "false") + ");";

			base.GenerateCodeFor((PetriNet)i, lastID);

			// Adding a transition from the entry point to all of the initially active states
			foreach(State s in i.States) {
				if(s.Active) {
					var newID = lastID.Consume();
					string tName = name + "_" + newID.ToString();

					CodeGen += name + ".addTransition(" + newID.ToString() + ", \"" + tName + "\", " + s.CppName + ", make_transition_callable([](actionResult_t){ return true; }));";
				}
			}
		}

		private void GenerateTransition(Transition t) {
			var old = new Dictionary<Cpp.LiteralExpression, string>();
			string enumName = Document.Settings.Enum.Name;

			foreach(Cpp.LiteralExpression le in t.Condition.GetLiterals()) {
				foreach(string e in Document.Settings.Enum.Members) {
					if(le.Expression == e) {
						old.Add(le, le.Expression);
						le.Expression = "static_cast<actionResult_t>(" + enumName + "::" + le.Expression + ")";
					}
					else if(le.Expression == "$Res") {
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

			var cppVar = new HashSet<Cpp.VariableExpression>();
			t.GetVariables(cppVar);

			if(cppVar.Count > 0) {
				string lockString = "";
				var cppLockLock = from v in cppVar
					select "_petri_lock_" + v.Expression;

				foreach(var v in cppVar) {
					lockString += "auto _petri_lock_" + v.Expression + " = petriNet.getVariable(static_cast<std::uint_fast32_t>(Petri_Var_Enum::" + v.Expression + ")).getLock();\n";
				}

				if(cppVar.Count > 1)
					lockString += "std::lock(" + String.Join(", ", cppLockLock) + ");\n";
				else {
					lockString += String.Join(", ", cppLockLock) + ".lock();\n";
				}

				cpp = "\n" + lockString + cpp + "\n";
			}


			cpp = "[&petriNet](actionResult_t _PETRI_PRIVATE_GET_ACTION_RESULT_) -> bool { " + cpp + " }";

			CodeGen += bName + ".addTransition(" + t.ID.ToString() + ", \"" + t.Name + "\", " + aName + ", make_transition_callable(" + cpp + "));";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}
		}

		private CodeGen _headerGen;
		private bool _generateHeader = true;
	}
}

