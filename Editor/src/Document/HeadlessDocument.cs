using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;

namespace Petri
{
	public class HeadlessDocument {
		public HeadlessDocument(string path) {
			this.Headers = new List<string>();
			CppActions = new List<Cpp.Function>();
			AllFunctions = new List<Cpp.Function>();
			CppConditions = new List<Cpp.Function>();

			AllFunctions = new List<Cpp.Function>();
			CppActions = new List<Cpp.Function>();
			CppConditions = new List<Cpp.Function>();

			CppMacros = new Dictionary<string, string>();

			var timeout = new Cpp.Function(new Cpp.Type("Timeout", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "Timeout", false);
			timeout.AddParam(new Cpp.Param(new Cpp.Type("std::chrono::duration<Rep, Period>", Cpp.Scope.EmptyScope()), "timeout"));
			CppConditions.Add(timeout);

			CppActions.Insert(0, Action.DefaultFunction);
			CppActions.Insert(0, Action.DoNothingFunction);
			AllFunctions.Insert(0, Action.DefaultFunction);
			AllFunctions.Insert(0, Action.DoNothingFunction);

			this.Path = path;
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
					if(func.ReturnType.Equals("ResultatAction")) {
						CppActions.Add(func);
					}
					else if(func.ReturnType.Equals("bool")) {
						CppConditions.Add(func);
					}
					AllFunctions.Add(func);
				}

				Headers.Add(header);
			}
		}

		// Performs the removal if possible
		public virtual bool RemoveHeader(string header) {
			if(PetriNet.UsesHeader(header))
				return false;

			CppActions.RemoveAll(a => a.Header == header);
			CppConditions.RemoveAll(c => c.Header == header);
			AllFunctions.RemoveAll(s => s.Header == header);
			Headers.Remove(header);

			return true;
		}

		public Dictionary<string, string> CppMacros {
			get;
			private set;
		}

		public List<Cpp.Function> CppConditions {
			get;
			private set;
		}

		public List<Cpp.Function> AllFunctions {
			get;
			private set;
		}

		public List<Cpp.Function> CppActions {
			get;
			private set;
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
				throw new Exception("Empty path!");
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

			while(this.Headers.Count > 0) {
				this.RemoveHeader(this.Headers[0]);
			}

			CppMacros.Clear();

			var node = elem.Element("Headers");
			if(node != null) {
				foreach(var e in node.Elements()) {
					this.AddHeader(e.Attribute("File").Value);
				}
			}

			node = elem.Element("Macros");
			if(node != null) {
				foreach(var e in node.Elements()) {
					CppMacros.Add(e.Attribute("Name").Value, e.Attribute("Value").Value);
				}
			}

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
				throw new Exception("No source output path defined. Please open the Petri net with the graphical editor and generate the C++ code once.");
			}


			var cppGen = PetriNet.GenerateCpp();
			cppGen.Item1.AddHeader("\"" + Settings.Name + ".h\"");
			cppGen.Item1.Write(System.IO.Path.Combine(System.IO.Directory.GetParent(Path).FullName, Settings.Name) + ".cpp");

			var generator = new Cpp.Generator();
			generator.AddHeader("\"PetriUtils.h\"");

			generator += "#ifndef PETRI_" + cppGen.Item2 + "_H";
			generator += "#define PETRI_" + cppGen.Item2 + "_H\n";

			generator += "#define CLASS_NAME " + Settings.Name;
			generator += "#define PREFIX \"" + CppPrefix + "\"";

			generator += "#define PORT " + Settings.Port;

			generator += "";

			generator += "#include \"PetriDynamicLib.h\"\n";

			generator += "#undef PORT";

			generator += "#undef PREFIX";
			generator += "#undef CLASS_NAME\n";

			generator += "#endif"; // ifndef header guard

			System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(Path).FullName, Settings.Name) + ".h", generator.Value);
		}

		public virtual bool Compile() {
			var c = new CppCompiler(this);
			var o = c.CompileSource(Settings.Name + ".cpp", Settings.Name);
			if(o != "") {
				Console.WriteLine("Compilation failed with error:\n" + "Invocation du compilateur :\n" + Settings.Compiler + " " + Settings.CompilerArgumentsForSource(Settings.GetSourcePath(Settings.Name + ".cpp"), Settings.Name) + "\n\nErreurs :\n" + o);
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

		int _wX, _wY, _wW, _wH;
	}
}

