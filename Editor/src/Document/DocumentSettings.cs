using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Petri
{
	public class DocumentSettings
	{
		public static DocumentSettings CreateSettings(XElement node) {
			return new DocumentSettings(node);
		}

		public static DocumentSettings GetDefaultSettings() {
			return new DocumentSettings(null);
		}

		public XElement GetXml() {
			var elem = new XElement("Settings");
			elem.SetAttributeValue("Name", Name);
			elem.SetAttributeValue("SourceOutputPath", SourceOutputPath);
			elem.SetAttributeValue("LibOutputPath", LibOutputPath);
			elem.SetAttributeValue("Hostname", Hostname);
			elem.SetAttributeValue("Port", Port.ToString());

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
				e.SetAttributeValue("Path", p.Item1);
				e.SetAttributeValue("Recursive", p.Item2);
				node.Add(e);
			}
			elem.Add(node);

			node = new XElement("LibPaths");
			foreach(var p in LibPaths) {
				var e = new XElement("LibPath");
				e.SetAttributeValue("Path", p.Item1);
				e.SetAttributeValue("Recursive", p.Item2);
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

		private DocumentSettings(XElement elem) {
			IncludePaths = new List<Tuple<string, bool>>();
			LibPaths = new List<Tuple<string, bool>>();
			Libs = new List<string>();
			CompilerFlags = new List<string>();

			this.Compiler = "/usr/bin/c++";
			this.SourceOutputPath = "";
			this.LibOutputPath = "";
			this.Hostname = "localhost";
			this.Port = 12345;

			Name = "MyPetriNet";

			if(elem == null) {
				CompilerFlags.Add("-std=c++1y");
				CompilerFlags.Add("-g");
			}
			else {
				if(elem.Attribute("Name") != null)
					Name = elem.Attribute("Name").Value;

				if(elem.Attribute("SourceOutputPath") != null)
					SourceOutputPath = elem.Attribute("SourceOutputPath").Value;
				if(elem.Attribute("LibOutputPath") != null)
					LibOutputPath = elem.Attribute("LibOutputPath").Value;

				if(elem.Attribute("Hostname") != null)
					Hostname = elem.Attribute("Hostname").Value;

				if(elem.Attribute("Port") != null)
					Port = UInt16.Parse(elem.Attribute("Port").Value);

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
						IncludePaths.Add(Tuple.Create(e.Attribute("Path").Value, bool.Parse(e.Attribute("Recursive").Value)));
					}
				}

				node = elem.Element("LibPaths");
				if(node != null) {
					foreach(var e in node.Elements()) {
						LibPaths.Add(Tuple.Create(e.Attribute("Path").Value, bool.Parse(e.Attribute("Recursive").Value)));
					}
				}

				node = elem.Element("Libs");
				if(node != null) {
					foreach(var e in node.Elements()) {
						Libs.Add(e.Attribute("Path").Value);
					}
				}
			}

			Modified = false;
		}

		public bool Modified {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		// The bool stands for "recursive or not"
		public List<Tuple<string, bool>> IncludePaths {
			get;
			private set;
		}

		public List<Tuple<string, bool>> LibPaths {
			get;
			private set;
		}

		public List<string> Libs {
			get;
			private set;
		}

		public string Compiler {
			get;
			set;
		}

		public List<string> CompilerFlags {
			get;
			private set;
		}

		public string LibOutputPath {
			get;
			set;
		}

		public string SourceOutputPath {
			get;
			set;
		}

		public string Hostname {
			get;
			set;
		}

		public UInt16 Port {
			get;
			set;
		}

		public string CompilerArguments {
			get {
				string val = "";

				val += "-shared ";
				if(Configuration.RunningPlatform == Platform.Mac) {
					val += "-undefined dynamic_lookup ";
				}

				foreach(var f in CompilerFlags) {
					val += f + " ";
				}

				foreach(var i in IncludePaths) {
					// Recursive?
					if(i.Item2) {
						var directories = System.IO.Directory.EnumerateDirectories(i.Item1, "*", System.IO.SearchOption.AllDirectories);
						foreach(var dir in directories) {
							val += "-I'" + dir + "' ";
						}
					}
					val += "-iquote'" + i.Item1 + "' ";
					val += "-I'" + i.Item1 + "' ";
				}
				val += "-iquote'" + SourceOutputPath + "' ";
				val += "-I'" + SourceOutputPath + "' ";

				foreach(var i in LibPaths) {
					// Recursive?
					if(i.Item2) {
						var directories = System.IO.Directory.EnumerateDirectories(i.Item1, "*", System.IO.SearchOption.AllDirectories);
						foreach(var dir in directories) {
							val += "-L'" + dir + "' ";
						}
					}
					val += "-L'" + i.Item1 + "' ";
				}

				foreach(var l in Libs) {
					val += "-l'" + l + "' ";
				}

				val += "-o '" + System.IO.Path.Combine(LibOutputPath, Name + ".so") + "' ";

				val += "'" + System.IO.Path.Combine(SourceOutputPath, Name) + ".cpp'";

				return val;
			}
		}
	}
}

