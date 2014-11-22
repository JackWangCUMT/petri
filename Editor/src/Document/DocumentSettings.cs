using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Petri
{
	public class DocumentSettings
	{
		public static DocumentSettings CreateSettings(Document doc, XElement node) {
			return new DocumentSettings(doc, node);
		}

		public static DocumentSettings GetDefaultSettings(Document doc) {
			return new DocumentSettings(doc, null);
		}

		public XElement GetXml() {
			var elem = new XElement("Settings");
			elem.SetAttributeValue("Name", Name);
			elem.SetAttributeValue("OutputPath", OutputPath);

			var node = new XElement("Compiler");
			node.SetAttributeValue("Invocation", Compiler);
			foreach(var f in CompilerFlags) {
				var e = new XElement("Flag");
				e.SetAttributeValue("Value", f);
				node.Add(e);
			}
			elem.Add(node);

			node = new XElement("IncludePaths");
			foreach(var p in IncludePaths) {
				var e = new XElement("IncludePath");
				e.SetAttributeValue("Path", p);
				node.Add(e);
			}
			elem.Add(node);

			node = new XElement("LibPaths");
			foreach(var p in LibPaths) {
				var e = new XElement("LibPath");
				e.SetAttributeValue("Path", p);
				node.Add(e);
			}
			elem.Add(node);

			node = new XElement("Libs");
			foreach(var p in Libs) {
				var e = new XElement("Lib");
				e.SetAttributeValue("Path", p);
				node.Add(e);
			}
			elem.Add(node);

			return elem;
		}

		private DocumentSettings(Document doc, XElement elem) {
			document = doc;

			IncludePaths = new List<string>();
			LibPaths = new List<string>();
			Libs = new List<string>();
			CompilerFlags = new List<string>();

			this.Compiler = "/usr/bin/c++";
			this.OutputPath = "";

			Name = "MyPetriNet";

			if(elem == null) {
				CompilerFlags.Add("-std=c++1y");
				CompilerFlags.Add("-g");
			}
			else {
				if(elem.Attribute("Name") != null)
					Name = elem.Attribute("Name").Value;

				if(elem.Attribute("OutputPath") != null)
					OutputPath = elem.Attribute("OutputPath").Value;

				var node = elem.Element("Compiler");
				if(node != null) {
					Compiler = node.Attribute("Invocation").Value;

					foreach(var e in node.Elements()) {
						CompilerFlags.Add(e.Attribute("Value").Value);
					}
				}

				node = elem.Element("IncludePaths");
				if(node != null) {
					foreach(var e in node.Elements()) {
						IncludePaths.Add(e.Attribute("Path").Value);
					}
				}

				node = elem.Element("LibPaths");
				if(node != null) {
					foreach(var e in node.Elements()) {
						LibPaths.Add(e.Attribute("Path").Value);
					}
				}

				node = elem.Element("Libs");
				if(node != null) {
					foreach(var e in node.Elements()) {
						Libs.Add(e.Attribute("Path").Value);
					}
				}
			}
		}

		public List<string> IncludePaths {
			get;
			private set;
		}

		public List<string> LibPaths {
			get;
			private set;
		}

		public List<string> Libs {
			get;
			private set;
		}

		public string Compiler {
			get;
			private set;
		}

		public List<string> CompilerFlags {
			get;
			private set;
		}

		public string OutputPath {
			get;
			set;
		}

		public string CompilerArguments {
			get {
				string val = "";

				val += "-shared -undefined dynamic_lookup ";

				foreach(var f in CompilerFlags) {
					val += f + " ";
				}

				foreach(var i in IncludePaths) {
					val += "-I'" + i + "' ";
				}
				val += "-I" + OutputPath + " ";

				foreach(var l in LibPaths) {
					val += "-L'" + l + "' ";
				}

				foreach(var l in Libs) {
					val += "-l'" + l + "' ";
				}

				val += "-o " + System.IO.Path.Combine(OutputPath, Name) + ".so ";

				val += System.IO.Path.Combine(OutputPath, Name) + ".cpp";

				return val;
			}
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value.Replace(" ", "_");
			}
		}

		string name;
		Document document;
	}
}

