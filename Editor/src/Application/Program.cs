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
			while(documents.Count > 0) {
				if(!documents[documents.Count - 1].CloseAndConfirm())
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
			return s.Replace("&", "&amp;");
		}

		public static void Main(string[] args)
		{
			Application.Init();

			var doc = new Document("");
			AddDocument(doc);

			Application.Run();
		}

		public static void AddDocument(Document doc) {
			documents.Add(doc);
		}

		public static void RemoveDocument(Document doc) {
			documents.Remove(doc);
		}

		public static List<Document> Documents {
			get {
				return documents;
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
				foreach(var d in documents) {
					if(d.Path == filename) {
						d.Window.Present();
						return;
					}
				}

				// Reuse last blank document which was created (typically the first one created on program launch)
				if(documents.Count > 0 && documents[documents.Count - 1].Blank) {
					documents[documents.Count - 1].Path = filename;
					documents[documents.Count - 1].Restore();
					documents[documents.Count - 1].Window.Present();
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
				return clipboard;
			}
			set {
				clipboard = value;
			}
		}

		static List<Document> documents = new List<Document>();
		static HashSet<Entity> clipboard = new HashSet<Entity>();
	}
}
