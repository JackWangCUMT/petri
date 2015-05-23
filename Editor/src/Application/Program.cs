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
			return System.Web.HttpUtility.HtmlEncode(s).Replace("{", "{{").Replace("}", "}}");
		}

		public delegate void EntryValDel(Gtk.Entry e, params object[] args);

		public static void RegisterValidation(Gtk.Entry e, bool change, EntryValDel a, params object[] p) {
			if(change) {
				e.Changed += (obj, eventInfo) => {
					a(e, p);
				};
			}
			else {
				e.FocusOutEvent += (obj, eventInfo) => {
					a(e, p);
				};
				e.Activated += (o, args) => {
					a(e, p);
				};
			}
		}

		private static int PrintUsage() {
			Console.WriteLine("Usage: mono Petri.exe [--generate] [--compile \"document.petri\" --arch (32|64)]");
			return 1;
		}

		public static int Main(string[] args) {
			if(args.Length > 1) {
				bool generate = args[0] == "--generate";
				bool compile = generate ? args.Length == 3 && args[1] == "--compile" : args[0] == "--compile";
				int arch = 0;
				for(int i = 1; i < args.Length; ++i) {
					if(args[i] == "--arch") {
						if(i < args.Length - 1) {
							if(int.TryParse(args[i + 1], out arch)) {
								if(arch == 32 || arch == 64) {
									Configuration.Arch = arch;
									Configuration.Save();
								}
								else {
									return PrintUsage();
								}
							}
							else {
								return PrintUsage();
							}
						}
						else {
							return PrintUsage();
						}
					}
				}
				string path = generate && compile ? args[2] : args[1];

				if(!compile && !generate) {
					return PrintUsage();
				}

				try {
					HeadlessDocument document = new HeadlessDocument(path);
					document.Load();
					Console.WriteLine("Processing Petri net " + document.Settings.Name + "…");

					string cppPath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName, document.Settings.SourceOutputPath), document.Settings.Name) + ".cpp";

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

		public static int PasteCount {
			get;
			set;
		}

		static List<Document> _documents = new List<Document>();
		static HashSet<Entity> _clipboard = new HashSet<Entity>();
	}
}
