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
		public CppPetriGen(HeadlessDocument doc) : base(doc, Language.Cpp, new CFamilyCodeGen(Language.Cpp)) {
			_headerGen = new CFamilyCodeGen(Language.Cpp);	
		}

		public override void WritePetriNet() {
			base.WritePetriNet();

			if(_generateHeader) {
				System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + ".h"), _headerGen.Value);
			}
		}

		public override void WriteExpressionEvaluator(Cpp.Expression expression, string path) {
			string cppExpr = expression.MakeCpp();

			CodeGen generator = new CFamilyCodeGen(Language.Cpp);
			foreach(string header in Document.Headers) {
				foreach(var s in Document.Headers) {
					var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, s);
					var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath));
					generator += "#include \"" + Configuration.GetRelativePath(p1, p2) + "\"";
				}
			}

			generator += "#include \"Runtime/Petri.h\"";
			generator += "#include \"Runtime/Atomic.h\"";
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
			generator += "}\n";

			System.IO.File.WriteAllText(path, generator.Value);
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
			CodeGen += "";

			CodeGen += "#define EXPORT extern \"C\"";
			CodeGen += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"\n";

			CodeGen += "\nusing namespace Petri;\n";

			CodeGen += GenerateVarEnum();

			CodeGen += "namespace {";
			CodeGen += "void fill(PetriNet &petriNet) {";

			foreach(var e in Document.PetriNet.Variables) {
				CodeGen += "petriNet.addVariable(static_cast<std::uint_fast32_t>(" + e.Prefix + e.Expression + "));";
			}
		}
					
		protected override void End() {
			CodeGen += "}"; // fill()
			CodeGen += "}"; // namespace

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

			CodeGen.Format();

			_headerGen += "#ifndef PETRI_" + Document.Settings.Name + "_H";
			_headerGen += "#define PETRI_" + Document.Settings.Name + "_H\n";

			_headerGen += "";
			_headerGen += "#include \"Runtime/PetriDynamicLib.h\"";
			_headerGen += "";
			_headerGen += "class " + Document.Settings.Name + " : public Petri::PetriDynamicLib {";
			_headerGen += "public:";
			_headerGen += "\t/**";
			_headerGen += "\t * Creates the dynamic library wrapper. It still needs to be loaded to make it possible to create the PetriNet objects.";
			_headerGen += "\t */";
			_headerGen += "\t" + Document.Settings.Name + "() = default;";
			_headerGen += "\t" + Document.Settings.Name + "(" + Document.Settings.Name + " const &pn) = delete;";
			_headerGen += "\t" + Document.Settings.Name + " &operator=(" + Document.Settings.Name + " const &pn) = delete;";
			_headerGen += "";
			_headerGen += "\t" + Document.Settings.Name + "(" + Document.Settings.Name + " &&pn) = default;";
			_headerGen += "\t" + Document.Settings.Name + " &operator=(" + Document.Settings.Name + " &&pn) = default;";
			_headerGen += "\tvirtual ~" + Document.Settings.Name + "() = default;";
			_headerGen += "";
			_headerGen += "\t/**";
			_headerGen += "\t * Returns the name of the Petri net.";
			_headerGen += "\t * @return The name of the Petri net";
			_headerGen += "\t */";
			_headerGen += "\tvirtual std::string name() const override {";
			_headerGen += "\t\treturn \"" + Document.CppPrefix + "\";";
			_headerGen += "\t}";
			_headerGen += "";
			_headerGen += "\t/**";
			_headerGen += "\t * Returns the TCP port on which a DebugSession initialized with this wrapper will listen to debugger connection.";
			_headerGen += "\t * @return The TCP port which will be used by DebugSession";
			_headerGen += "\t */";
			_headerGen += "\tvirtual uint16_t port() const override {";
			_headerGen += "\t\treturn " + Document.Settings.Port + ";";
			_headerGen += "\t}";
			_headerGen += "";
			_headerGen += "\tvirtual char const *prefix() const override {";
			_headerGen += "\t\treturn \"" + Document.CppPrefix + "\";";
			_headerGen += "\t}";
			_headerGen += "};";

			_headerGen += "#endif"; // ifndef header guard

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
							le.Expression = "static_cast<actionResult_t>(" + enumName + "::" + le.Expression + ")";
						}
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
					action += "auto _petri_lock_" + v.Expression + " = petriNet.getVariable(static_cast<std::uint_fast32_t>(" + v.Prefix + v.Expression + ")).getLock();\n";
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

		protected override void GenerateExitPoint(ExitPoint e, IDManager lastID) {
			CodeGen += "auto &" + e.CppName + " = petriNet.addAction(" +
				"Action(" + e.ID.ToString() + ", \"" + e.Parent.Name + "_" + e.Name + "\", make_action_callable([](){ return actionResult_t(); }), "  + e.RequiredTokens.ToString()
				+ "), false);";
		}

		protected override void GenerateInnerPetriNet(InnerPetriNet i, IDManager lastID) {
			string name = i.EntryPointName;

			// Adding an entry point
			CodeGen += "auto &" + name + " = petriNet.addAction("
				+ "Action(" + i.EntryPointID + ", \"" + i.Name + "_Entry\", make_action_callable([](){ return actionResult_t(); }), " + i.RequiredTokens.ToString() + "), " + (i.Active ? "true" : "false") + ");";

			// Adding a transition from the entry point to all of the initially active states
			foreach(State s in i.States) {
				if(s.Active) {
					var newID = lastID.Consume();
					string tName = name + "_" + newID.ToString();

					CodeGen += name + ".addTransition(" + newID.ToString() + ", \"" + tName + "\", " + s.CppName + ", make_transition_callable([](actionResult_t){ return true; }));";
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
							le.Expression = "static_cast<actionResult_t>(" + enumName + "::" + le.Expression + ")";
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

			var cppVar = new HashSet<Cpp.VariableExpression>();
			t.GetVariables(cppVar);

			if(cppVar.Count > 0) {
				string lockString = "";
				var cppLockLock = from v in cppVar
					select "_petri_lock_" + v.Expression;

				foreach(var v in cppVar) {
					lockString += "auto _petri_lock_" + v.Expression + " = petriNet.getVariable(static_cast<std::uint_fast32_t>(" + v.Prefix + v.Expression + ")).getLock();\n";
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

		protected string GenerateVarEnum() {
			var variables = Document.PetriNet.Variables;
			var cppVar = from v in variables
				select v.Expression;
			if(variables.Count > 0) {
				return "enum class " + Cpp.VariableExpression.EnumName + "  : std::uint_fast32_t {" + String.Join(", ", cppVar) + "};\n";
			}

			return "";
		}

		private CodeGen _headerGen;
		private bool _generateHeader = true;
	}
}

