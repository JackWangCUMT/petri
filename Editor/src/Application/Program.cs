using System;
using Gtk;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Collections;

namespace Petri
{
	public class MainClass
	{
		public static bool OnExit() {
			while(_documents.Count > 0) {
				if(!_documents[_documents.Count - 1].CloseAndConfirm())
					return false;
			}

			return true;
		}

		public static void SaveAndQuit() {
			bool exit = OnExit();
			if(!exit)
				return;

			Configuration.Save();
			Application.Quit();
		}

		public static String SafeMarkupFromString(string s) {
			return System.Web.HttpUtility.HtmlEncode(s);
		}

		private static int PrintUsage() {
			Console.WriteLine("Usage: mono Petri.exe [--generate] [--compile \"document.petri\"]");
			return 1;
		}

		public static int Main(string[] args) {
			if(args.Length > 1) {
				bool generate = args[0] == "--generate";
				bool compile = generate ? args.Length == 3 && args[1] == "--compile" : args[0] == "--compile";
				string path = generate && compile ? args[2] : args[1];

				if(!compile && !generate) {
					return PrintUsage();
				}

				try {
					HeadlessDocument document = new HeadlessDocument(path);
					document.Load();
					Console.WriteLine("Processing Petri net " + document.Settings.Name + "…");

					string cppPath = System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName, document.Settings.Name) + ".cpp";

					bool forceGeneration = false;
					if(!generate && compile) {
						if(!System.IO.File.Exists(cppPath)
							|| System.IO.File.GetLastWriteTime(cppPath) < System.IO.File.GetLastWriteTime(document.Path)) {
							generate = true;
							forceGeneration = true;
						}
						else {
							Console.WriteLine("Previously generated C++ code is up to date, no need for code generation");
						}
					}

					if(generate) {
						if(forceGeneration) {
							Console.WriteLine("Previously generated C++ code is outdated or nonexistent, generating new code…");
						}
						document.SaveCppDontAsk();
						document.Save();
						Console.WriteLine("Successfully generated C++ code");
					}
					if(compile) {
						string dylibPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName, System.IO.Path.Combine(document.Settings.LibOutputPath, document.Settings.Name  + ".so")));
						if(!System.IO.File.Exists(dylibPath) || System.IO.File.GetLastWriteTime(dylibPath) < System.IO.File.GetLastWriteTime(cppPath)) {
							Console.WriteLine("Compiling the C++ code…");
							bool res = document.Compile(false);
							if(!res) {
								Console.WriteLine("Compilation failed, aborting!");
								return 3;
							}
							else {
								Console.WriteLine("Compilation successful!");
							}
						}
						else {
							Console.WriteLine("Previously compiled dylib is up to date, no need for recompilation");
						}
					}
				}
				catch(Exception e) {
					Console.WriteLine("An exception occurred: " + e.Message);
					return 2;
				}

				return 0;
			}
			else {
				Application.Init();

				var doc = new Document("");
				AddDocument(doc);

				Application.Run();

				return 0;
			}
		}

		public static void AddDocument(Document doc) {
			_documents.Add(doc);
		}

		public static void RemoveDocument(Document doc) {
			_documents.Remove(doc);
		}

		public static List<Document> Documents {
			get {
				return _documents;
			}
		}

		public static void OpenDocument() {
			var fc = new Gtk.FileChooserDialog("Ouvrir le graphe…", null,
				FileChooserAction.Open,
				new object[] {"Annuler", ResponseType.Cancel,
					"Ouvrir", ResponseType.Accept
				});

			var filter = new FileFilter();
			filter.AddPattern("*.petri");
			fc.AddFilter(filter);

			if(fc.Run() == (int)ResponseType.Accept) {
				string filename = fc.Filename;
				fc.Destroy();
				foreach(var d in _documents) {
					if(d.Path == filename) {
						d.Window.Present();
						return;
					}
				}

				// Reuse last blank document which was created (typically the first one created on program launch)
				if(_documents.Count > 0 && _documents[_documents.Count - 1].Blank) {
					_documents[_documents.Count - 1].Path = filename;
					_documents[_documents.Count - 1].Restore();
					_documents[_documents.Count - 1].Window.Present();
				}
				else {
					var doc = new Document(filename);
					MainClass.AddDocument(doc);
				}
			}
			else {
				fc.Destroy();
			}
		}

		public static HashSet<Entity> Clipboard {
			get {
				return _clipboard;
			}
			set {
				_clipboard = value;
			}
		}

		static List<Document> _documents = new List<Document>();
		static HashSet<Entity> _clipboard = new HashSet<Entity>();
	}
}
