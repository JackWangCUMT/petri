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
using System.Xml.Linq;
using System.Xml;
using System.Linq;

namespace Petri
{
	public class HeadlessDocument {
		public HeadlessDocument(string path) {
			Headers = new List<string>();
			CppActions = new List<Cpp.Function>();
			AllFunctionsList = new List<Cpp.Function>();
			CppConditions = new List<Cpp.Function>();

			CppMacros = new Dictionary<string, string>();

			Conflicting = new HashSet<Entity>();

			var timeout = new Cpp.Function(new Cpp.Type("Timeout", Cpp.Scope.EmptyScope), Cpp.Scope.EmptyScope, "Timeout", false);
			timeout.AddParam(new Cpp.Param(new Cpp.Type("std::chrono::duration<Rep, Period>", Cpp.Scope.EmptyScope), "timeout"));
			CppConditions.Add(timeout);

			Path = path;
		}

		public string Path {
			get;
			set;
		}
			
		public List<string> Headers {
			get;
			private set;
		}

		public virtual void AddHeader(string header) {
			AddHeaderNoUpdate(header);
			UpdateConflicts();
		}

		public void AddHeaderNoUpdate(string header) {
			if(header.Length == 0 || Headers.Contains(header))
				return;

			if(header.Length > 0) {
				string filename = header;

				// If path is relative, then make it absolute
				if(!System.IO.Path.IsPathRooted(header)) {
					filename = System.IO.Path.Combine(System.IO.Directory.GetParent(this.Path).FullName, filename);
				}

				var functions = Cpp.Parser.Parse(filename);
				foreach(var func in functions) {
					AllFunctionsList.Add(func);
				}

				Headers.Add(header);
			}
		}

		public Dictionary<string, string> CppMacros {
			get;
			private set;
		}

		public List<Cpp.Function> CppConditions {
			get;
			private set;
		}

		public List<Cpp.Function> AllFunctionsList {
			get;
			private set;
		}

		public IEnumerable<Cpp.Function> AllFunctions {
			get {
				Cpp.Function[] ff = new Cpp.Function[AllFunctionsList.Count + 3];
				ff[0] = Action.DoNothingFunction(this);
				ff[1] = Action.PrintFunction(this);
				ff[2] = Action.PauseFunction(this);
				for(int i = 0; i < AllFunctionsList.Count; ++i) {
					ff[i + 3] = AllFunctionsList[i];
				}

				return ff;
			}
		}

		public List<Cpp.Function> CppActions {
			get;
			private set;
		}

		public void DispatchFunctions() {
			CppActions.Clear();
			CppConditions.Clear();
			Cpp.Type e = Settings.Enum.Type, b = new Cpp.Type("bool", Cpp.Scope.EmptyScope);

			foreach(Cpp.Function f in AllFunctions) {
				if(f.ReturnType.Equals(e)) {
					CppActions.Add(f);
				}
				else if(f.ReturnType.Equals(b)) {
					CppConditions.Add(f);
				}
			}
		}

		public RootPetriNet PetriNet {
			get;
			set;
		}

		public string GetRelativeToDoc(string path) {
			if(!System.IO.Path.IsPathRooted(path)) {
				path = System.IO.Path.GetFullPath(path);
			}

			string parent = System.IO.Directory.GetParent(Path).FullName;

			return Configuration.GetRelativePath(path, parent);
		}

		protected virtual Tuple<int, int> GetWindowSize() {
			return Tuple.Create(_wW, _wH);
		}

		protected virtual void SetWindowSize(int w, int h) {
			_wW = w;
			_wH = h;
		}

		protected virtual Tuple<int, int> GetWindowPosition() {
			return Tuple.Create(_wX, _wY);
		}

		protected virtual void SetWindowPosition(int x, int y) {
			_wX = x;
			_wY = y;
		}

		public virtual void Save() {
			string tempFileName = "";
			if(Path == "") {
				throw new Exception(Configuration.GetLocalized("Empty path!"));
			}

			var doc = new XDocument();
			var root = new XElement("Document");

			root.Add(Settings.GetXml());

			var winConf = new XElement("Window");
			{
				var size = GetWindowSize();
				var position = GetWindowPosition();
				winConf.SetAttributeValue("X", position.Item1.ToString());
				winConf.SetAttributeValue("Y", position.Item2.ToString());
				winConf.SetAttributeValue("W", size.Item1.ToString());
				winConf.SetAttributeValue("H", size.Item2.ToString());
			}

			var headers = new XElement("Headers");
			foreach(var h in Headers) {
				var hh = new XElement("Header");
				hh.SetAttributeValue("File", h);
				headers.Add(hh);
			}
			var macros = new XElement("Macros");
			foreach(var m in CppMacros) {
				var mm = new XElement("Macro");
				mm.SetAttributeValue("Name", m.Key);
				mm.SetAttributeValue("Value", m.Value);
				macros.Add(mm);
			}
			doc.Add(root);
			root.Add(winConf);
			root.Add(headers);
			root.Add(macros);
			root.Add(PetriNet.GetXml());

			// Write to a temporary file to avoid corrupting the existing document on error
			tempFileName = System.IO.Path.GetTempFileName();
			XmlWriterSettings xsettings = new XmlWriterSettings();
			xsettings.Indent = true;
			xsettings.Encoding = new System.Text.UTF8Encoding(false);
			XmlWriter writer = XmlWriter.Create(tempFileName, xsettings);

			doc.Save(writer);
			writer.Flush();
			writer.Close();

			if(System.IO.File.Exists(this.Path))
				System.IO.File.Delete(this.Path);
			System.IO.File.Move(tempFileName, this.Path);
			tempFileName = "";

			Settings.Modified = false;
		}

		public void Load() {
			var document = XDocument.Load(Path);

			var elem = document.FirstNode as XElement;

			Settings = DocumentSettings.CreateSettings(elem.Element("Settings"));

			var winConf = elem.Element("Window");
			SetWindowPosition(int.Parse(winConf.Attribute("X").Value), int.Parse(winConf.Attribute("Y").Value));
			SetWindowSize(int.Parse(winConf.Attribute("W").Value), int.Parse(winConf.Attribute("H").Value));

			Headers.Clear();
			AllFunctionsList.Clear();

			CppMacros.Clear();

			var node = elem.Element("Headers");
			if(node != null) {
				foreach(var e in node.Elements()) {
					this.AddHeaderNoUpdate(e.Attribute("File").Value);
				}
			}

			node = elem.Element("Macros");
			if(node != null) {
				foreach(var e in node.Elements()) {
					CppMacros.Add(e.Attribute("Name").Value, e.Attribute("Value").Value);
				}
			}

			DispatchFunctions();

			PetriNet = new RootPetriNet(this, elem.Element("PetriNet"));
			PetriNet.Canonize();
		}

		public string CppPrefix {
			get {
				return Settings.Name;
			}
		}

		public void SaveCppDontAsk() {
			if(this.Settings.SourceOutputPath.Length == 0) {
				throw new Exception(Configuration.GetLocalized("No source output path defined. Please open the Petri net with the graphical editor and generate the C++ code once."));
			}
			else if(Conflicts(PetriNet)) {
				throw new Exception(Configuration.GetLocalized("The Petri net has conflicting states. Please open it with the graphical editor and solve the conflicts."));
			}

			var cppGen = PetriNet.GenerateCpp();
			cppGen.Item1.AddHeader("\"" + Settings.Name + ".h\"");
			cppGen.Item1.Write(System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Path).FullName, Settings.SourceOutputPath), Settings.Name) + ".cpp");

			var generator = new Cpp.Generator();

			generator += "#ifndef PETRI_" + Settings.Name + "_H";
			generator += "#define PETRI_" + Settings.Name + "_H\n";

			generator += "#define PETRI_CLASS_NAME " + Settings.Name;
			generator += "#define PETRI_PREFIX \"" + CppPrefix + "\"";
			generator += "#define PETRI_ENUM " + Settings.Enum.Name;
			generator += "#define PETRI_PORT " + Settings.Port;


			generator += "";

			generator += "#include \"Runtime/PetriDynamicLib.h\"\n";

			generator += "#undef PETRI_PORT";

			generator += "#undef PETRI_ENUM";
			generator += "#undef PETRI_PREFIX";
			generator += "#undef PETRI_CLASS_NAME\n";

			generator += "#endif"; // ifndef header guard

			string path = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Path).FullName, Settings.SourceOutputPath), Settings.Name) + ".h";
			bool generate = true;
			string headerCode = generator.Value;
			if(System.IO.File.Exists(path)) {
				string existing = System.IO.File.ReadAllText(path);
				if(existing.Length > 1 && existing.Substring(0, existing.Length - 1) == headerCode) {
					generate = false;
				}
			}

			if(generate) {
				System.IO.File.WriteAllText(path, generator.Value);
			}
		}

		public virtual bool Compile(bool wait) {
			var c = new CppCompiler(this);
			var o = c.CompileSource(Settings.SourcePath, Settings.LibPath);
			if(o != "") {
				Console.WriteLine(Configuration.GetLocalized("Compilation failed.") + "\n" + Configuration.GetLocalized("Invocation du compilateur :") + "\n" + Settings.Compiler + " " + Settings.CompilerArguments(Settings.SourcePath, Settings.LibPath) + "\n\n" + Configuration.GetLocalized("Erreurs :") + "\n" + o);
				return false;
			}

			return true;
		}

		public UInt64 LastEntityID {
			get;
			set;
		}

		public Entity EntityFromID(UInt64 id) {
			return PetriNet.EntityFromID(id);
		}

		public void ResetID() {
			this.LastEntityID = 0;
		}

		public DocumentSettings Settings {
			get;
			protected set;
		}

		public string GetHash() {
			return PetriNet.GetHash();
		}

		public HashSet<Entity> Conflicting {
			get;
			private set;
		}

		public bool Conflicts(Entity e) {
			if(Conflicting.Contains(e)) {
				return true;
			}
			else if(e is PetriNet) {
				foreach(var ee in Conflicting) {
					if(((PetriNet)e).EntityFromID(ee.ID) != null) {
						return true;
					}
				}
			}

			return false;
		}

		public virtual void UpdateConflicts() {
			Conflicting.Clear();
			PetriNet.UpdateConflicts();
		}

		public string GenerateVarEnum() {
			var variables = PetriNet.Variables;
			var cppVar = from v in variables
			             select v.Expression;
			if(variables.Count > 0) {
				return "enum class Petri_Var_Enum : std::uint_fast32_t {" + String.Join(", ", cppVar) + "};";
			}

			return "";
		}

		int _wX, _wY, _wW, _wH;
	}
}

