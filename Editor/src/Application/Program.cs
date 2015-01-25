using System;
using Gtk;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

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

		public static void Main(string[] args)
		{
			Application.Init();

			var doc = new Document("");
			AddDocument(doc);

			Application.Run();
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
