﻿using System;
using System.Xml.Linq;
using System.Xml;
using Gtk;

namespace Petri
{
	public class Document {
		public Document(string path) {
			window = new MainWindow(this);
			controller = new PetriController(this);

			this.path = path;
			this.Blank = true;
			this.Restore();
			window.PresentWindow();
		}

		public MainWindow Window {
			get {
				return window;
			}
		}

		public PetriController Controller {
			get {
				return controller;
			}
		}

		public string Path {
			get {
				return path;
			}
			set {
				path = value;
			}
		}

		public bool Blank {
			get;
			set;
		}

		public bool CloseAndConfirm() {
			if(Controller.Modified) {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "Souhaitez-vous enregistrer les modifications apportées au graphe ? Vos modifications seront perdues si vous ne les enregistrez pas.");
				d.AddButton("Ne pas enregistrer", ResponseType.No);
				d.AddButton("Annuler", ResponseType.Cancel);
				d.AddButton("Enregistrer", ResponseType.Yes).HasDefault = true;

				ResponseType result = (ResponseType)d.Run();

				if(result == ResponseType.Yes) {
					Save();
					d.Destroy();
				}
				else if(result == ResponseType.No) {
					d.Destroy();
				}
				else {
					d.Destroy();
					return false;
				}
			}

			MainClass.RemoveDocument(this);
			if(MainClass.Documents.Count == 0)
				MainClass.SaveAndQuit();
			return true;
		}

		public void SaveAs() {
			var fc = new Gtk.FileChooserDialog("Enregistrer le graphe sous…", window,
				FileChooserAction.Save,
				new object[]{"Annuler",ResponseType.Cancel,
					"Enregistrer",ResponseType.Accept});

			if(Configuration.SavePath.Length > 0) {
				fc.SetCurrentFolder(System.IO.Directory.GetParent(Configuration.SavePath).FullName);
				fc.CurrentName = System.IO.Path.GetFileName(Configuration.SavePath);
			}

			fc.DoOverwriteConfirmation = true;

			if(fc.Run() == (int)ResponseType.Accept) {
				this.path = fc.Filename;
				if(!this.path.EndsWith(".petri"))
					this.path += ".petri";
				Window.Title = System.IO.Path.GetFileName(this.path).Split(new string[]{".petri"}, StringSplitOptions.None)[0];
				fc.Destroy();
			}
			else {
				fc.Destroy();
				return;
			}

			this.Save();
		}

		public void Save()
		{
			string tempFileName = "";
			try {
				if(path == "") {
					this.SaveAs();
					return;
				}

				var doc = new XDocument();
				var root = new XElement("Document");

				root.Add(settings.GetXml());

				var winConf = new XElement("Window");
				{
					int w, h, x, y;
					Window.GetSize(out w, out h);
					Window.GetPosition(out x, out y);
					winConf.SetAttributeValue("X", x.ToString());
					winConf.SetAttributeValue("Y", y.ToString());
					winConf.SetAttributeValue("W", w.ToString());
					winConf.SetAttributeValue("H", h.ToString());
				}

				var headers = new XElement("Headers");
				foreach(var h in Controller.Headers) {
					var hh = new XElement("Header");
					hh.SetAttributeValue("File", h);
					headers.Add(hh);
				}
				doc.Add(root);
				root.Add(winConf);
				root.Add(headers);
				root.Add(petriNet.GetXml());

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

				Controller.Modified = false;
			}
			catch(Exception e) {
				if(tempFileName.Length > 0)
					System.IO.File.Delete(tempFileName);

				MessageDialog d = new MessageDialog(window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, "Une erreur est survenue lors de l'enregistrement : " + e.ToString());
				d.AddButton("OK", ResponseType.Cancel);
				d.Run();
				d.Destroy();
			}
		}

		public void Restore()
		{
			if(Controller.Modified) {
				MessageDialog d = new MessageDialog(window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, "Souhaitez-vous revenir à la dernière version enregistrée du graphe ? Vos modifications seront perdues.");
				d.AddButton("Annuler", ResponseType.Cancel);
				d.AddButton("Revenir", ResponseType.Accept);

				ResponseType result = (ResponseType)d.Run();

				d.Destroy();
				if(result != ResponseType.Accept) {
					return;
				}
			}

			window.PetriView.EditedPetriNet = null;
			Controller.EditedObject = null;

			var oldPetriNet = petriNet;

			this.ResetID();
			settings = null;

			try {
				if(path == "") {
					petriNet = new RootPetriNet(this);
					int docID = 1;
					string prefix = "Sans titre ";
					foreach(var d in MainClass.Documents) {
						if(d.Window.Title.StartsWith(prefix)) {
							int id = 0;
							if(int.TryParse(d.Window.Title.Substring(prefix.Length), out id)) {
								docID = id + 1;
							}
						}
					}
					Window.Title = prefix + docID.ToString();
					Controller.Modified = false;
					Blank = true;
				}
				else {
					var document = XDocument.Load(path);

					var elem = document.FirstNode as XElement;

					settings = DocumentSettings.CreateSettings(this, elem.Element("Settings"));

					var winConf = elem.Element("Window");
					Window.Move(int.Parse(winConf.Attribute("X").Value), int.Parse(winConf.Attribute("Y").Value));
					Window.Resize(int.Parse(winConf.Attribute("W").Value), int.Parse(winConf.Attribute("H").Value));

					var node = elem.Element("Headers");
					foreach(var e in node.Elements()) {
						Controller.AddHeader(e.Attribute("File").Value);
					}

					petriNet = new RootPetriNet(this, elem.Element("PetriNet"));
					petriNet.Canonize();
					Window.Title = System.IO.Path.GetFileName(this.path).Split(new string[]{".petri"}, StringSplitOptions.None)[0];
					this.Blank = false;
					Controller.Modified = false;
				}
			}
			catch(Exception e) {
				MessageDialog d = new MessageDialog(window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, "Une erreur est survenue lors de du chargement du document : " + e.ToString());
				d.AddButton("OK", ResponseType.Cancel);
				d.Run();
				d.Destroy();
				Console.WriteLine("Error during Petri net loading: {0}", e.Message);

				if(oldPetriNet != null) {
					// The document was already open and the user-requested restore failed for some reason. What could we do?
					// At least the modified document is preserved so that the user has a chance to save his work.
				}
				else {
					// If it is a fresh opening, just get back to an empty state.
					petriNet = new RootPetriNet(this);
					Controller.Modified = false;
					this.Blank = true;
				}
			}
			if(settings == null) {
				settings = DocumentSettings.GetDefaultSettings(this);
			}
			Controller.PetriNet = petriNet;
			window.PetriView.EditedPetriNet = petriNet;

			window.PetriView.Redraw();
		}

		public void SaveCpp()
		{
			var fc = new Gtk.FileChooserDialog("Enregistrer le code généré sous…", window,
				FileChooserAction.SelectFolder,
				new object[]{"Annuler",ResponseType.Cancel,
					"Enregistrer",ResponseType.Accept});
			
			if(this.settings.OutputPath.Length > 0) {
				fc.SetCurrentFolder(this.settings.OutputPath);
			}

			fc.DoOverwriteConfirmation = true;

			if(fc.Run() == (int)ResponseType.Accept) {
				var cppGen = petriNet.GenerateCpp();
				cppGen.Item1.AddHeader("\"" + System.IO.Path.Combine(fc.Filename, settings.Name) + ".h\"");
				cppGen.Item1.Write(System.IO.Path.Combine(fc.Filename, settings.Name) + ".cpp");

				var generator = new Cpp.Generator();
				generator.AddHeader("\"PetriUtils.h\"");

				generator += "#ifndef PETRI_" + cppGen.Item2 + "_H";
				generator += "#define PETRI_" + cppGen.Item2 + "_H\n";

				generator += "#define CLASS_NAME MyPetriNet";
				generator += "#define PREFIX \"" + settings.Name + "\"";
				generator += "#define LIB_PATH \"" + System.IO.Path.Combine(fc.Filename, settings.Name) + ".so" + "\"\n";

				generator += "#include \"PetriDynamicLib.h\"\n";

				generator += "#endif"; // ifndef header guard

				System.IO.File.WriteAllText(System.IO.Path.Combine(fc.Filename, settings.Name) + ".h", generator.Value);
			
				string old = settings.OutputPath;
				this.settings.OutputPath = fc.Filename;
				Controller.Modified = old != settings.OutputPath;
			}

			fc.Destroy();
		}

		public void Compile() {
			var c = new CppCompiler(this);
			var o = c.Compile();
			if(o != "") {
				// TODO: manage errors output
			}
		}

		public void ManageHeaders() {
			if(headersManager == null) {
				headersManager = new HeadersManager(this);
			}

			headersManager.Show();
		}

		public UInt64 LastEntityID {
			get;
			set;
		}

		public void ResetID() {
			this.LastEntityID = 0;
		}

		public DocumentSettings Settings {
			get {
				return settings;
			}
		}

		string path;
		MainWindow window;
		PetriController controller;
		RootPetriNet petriNet;
		HeadersManager headersManager;
		DocumentSettings settings;
	}
}

