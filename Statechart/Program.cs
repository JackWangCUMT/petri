using System;
using Gtk;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Statechart
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

		public static void Main(string[] args)
		{
			/*var ff = Cpp.Parser.Parse("/Users/remi/Documents/Programmation/C#/IA Robot/IA Pétri/IA Pétri/TransitionsHeader.h");
			foreach(var f in ff) {
				Console.WriteLine(f.Signature);
			}
			var s = Cpp.Expression.CreateFromString<Cpp.Expression>("c3(b+c)* \"huhuihuhiu\" - f4('g'[456+3])", null, ff);
			Console.WriteLine(s.MakeUserReadable());
			var s2 = Cpp.Expression.CreateFromString<Cpp.Expression>("a<(b+3)*4", null, ff);
			Console.WriteLine(s2.MakeUserReadable());
			var s3 = Cpp.Expression.CreateFromString<Cpp.Expression>("getA().AA::init2(42<c3(42+7))", null, ff);
			Console.WriteLine(s3.MakeUserReadable());
			return;*/

			/*var f2 = ff[7];
			var i = new Cpp.MethodInvocation((Cpp.Method)f2, new Cpp.LitteralExpression("patata"), true, new Cpp.LitteralExpression("myVec"));
			Console.WriteLine(i.MakeUserReadable());
			var expr = Cpp.Expression.CreateFromString<Cpp.Expression>("(::f2()).AA::print(f3(\"fjioejo\", 42))", null, ff);
			Console.WriteLine(expr.MakeUserReadable());
			return*/

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
			filter.AddPattern("*.stct");
			fc.AddFilter(filter);

			if(fc.Run() == (int)ResponseType.Accept) {
				foreach(var d in documents) {
					if(d.Path == fc.Filename) {
						d.Window.Present();
						fc.Destroy();
						return;
					}
				}

				// Reuse last virgin document which was created (typically the first one created on program launch)
				if(documents.Count > 0 && !documents[documents.Count - 1].Dirty) {
					documents[documents.Count - 1].Path = fc.Filename;
					documents[documents.Count - 1].Restore();
					documents[documents.Count - 1].Window.Present();
				}
				else {
					var doc = new Document(fc.Filename);
					MainClass.AddDocument(doc);
				}
			}
			fc.Destroy();
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
