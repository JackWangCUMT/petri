using System;
using System.Xml.Linq;
using System.Xml;
using Gtk;
using System.Collections.Generic;

namespace Petri
{
	public class Document : HeadlessDocument {
		public Document(string path) : base(path) {
			Window = new MainWindow(this);

			this.UndoManager = new UndoManager();

			EditorController = new EditorController(this);
			DebugController = new DebugController(this);

			this.CurrentController = EditorController;

			this.Path = path;
			this.Blank = true;
			this.Restore();
			Window.PresentWindow();
			Window.EditorGui.Paned.Position = Window.Allocation.Width - 260;
			Window.DebugGui.Paned.Position = Window.Allocation.Width - 200;
			AssociatedWindows = new HashSet<Window>();
		}

		public HashSet<Window> AssociatedWindows {
			get;
			private set;
		}

		public MainWindow Window {
			get;
			private set;
		}

		public EditorController EditorController {
			get;
			private set;
		}

		public DebugController DebugController {
			get;
			private set;
		}

		public Controller CurrentController {
			get;
			private set;
		}

		public void SwitchToDebug() {
			CurrentController = DebugController;
			Window.Gui = Window.DebugGui;
		}

		public void SwitchToEditor() {
			CurrentController = EditorController;
			Window.Gui = Window.EditorGui;
		}

		public bool Blank {
			get;
			set;
		}

		public void PostAction(GuiAction a) {
			Modified = true;
			UndoManager.PostAction(a);
			UpdateUndo();
			Window.Gui.BaseView.Redraw();
		}

		public void Undo() {
			UndoManager.Undo();
			var focus = UndoManager.NextRedo.Focus;
			CurrentController.ManageFocus(focus);

			UpdateUndo();
			Window.Gui.Redraw();
		}

		public void Redo() {
			UndoManager.Redo();
			var focus = UndoManager.NextUndo.Focus;
			CurrentController.ManageFocus(focus);

			UpdateUndo();
			Window.Gui.Redraw();
		}

		protected override Tuple<int, int> GetWindowSize() {
			int w, h;
			Window.GetSize(out w, out h);
			return Tuple.Create(w, h);
		}

		protected override void SetWindowSize(int w, int h) {
			Window.Resize(w, h);
		}

		protected override Tuple<int, int> GetWindowPosition() {
			int x, y;
			Window.GetPosition(out x, out y);
			return Tuple.Create(x, y);
		}

		protected override void SetWindowPosition(int x, int y) {
			Window.Move(x, y);
		}

		public override void AddHeader(string header) {
			base.AddHeader(header);

			if(header.Length > 0) {
				Modified = true;
			}
		}

		// Performs the removal if possible
		public override bool RemoveHeader(string header) {
			bool result = base.RemoveHeader(header);
			if(result) {
				Modified = true;
			}

			return result;
		}

		public override void Save() {
			if(Path == "") {
				this.SaveAs();
			}
			else {
				base.Save();
			}
			Modified = false;
		}

		public bool Modified {
			get {
				return _modified || (Settings != null && Settings.Modified);
			}
			set {
				_modified = value;
				if(value == true) {
					this.Blank = false;
				}
				else {
					// We require the current undo stack to represent an unmodified state
					_guiActionToMatchSave = UndoManager.NextUndo;
				}
			}
		}

		public bool CloseAndConfirm() {
			if(this.DebugController.Server.SessionRunning) {
				Window.Present();
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "Une session de débuggeur est toujours active. Souhaitez-vous l'arrêter ?");
				d.AddButton("Annuler", ResponseType.Cancel);
				d.AddButton("Arrêter la session", ResponseType.Yes).HasDefault = true;

				ResponseType result = (ResponseType)d.Run();

				if(result == ResponseType.Yes) {
					DebugController.Server.Detach();
					d.Destroy();
				}
				else {
					d.Destroy();
					return false;
				}
			}

			if(Modified) {
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

			foreach(Window w in AssociatedWindows) {
				w.Hide();
			}

			MainClass.RemoveDocument(this);
			if(MainClass.Documents.Count == 0)
				MainClass.SaveAndQuit();

			return true;
		}

		public void ExportAsPDF() {
			string exportPath = "";

			var fc = new Gtk.FileChooserDialog("Exporter le PDF sous…", Window,
				FileChooserAction.Save,
				new object[]{"Annuler",ResponseType.Cancel,
					"Enregistrer",ResponseType.Accept});

			if(Configuration.SavePath.Length > 0) {
				fc.SetCurrentFolder(System.IO.Directory.GetParent(Configuration.SavePath).FullName);
				fc.CurrentName = System.IO.Path.GetFileName(Configuration.SavePath);
			}

			fc.DoOverwriteConfirmation = true;

			if(fc.Run() == (int)ResponseType.Accept) {
				exportPath = fc.Filename;
				if(!exportPath.EndsWith(".pdf"))
					exportPath += ".pdf";
			}
			fc.Destroy();

			if(exportPath != "") {
				var renderView = new RenderView(this);

				renderView.Render(exportPath);
			}
		}

		public void SaveAs() {
			string filename = null;

			var fc = new Gtk.FileChooserDialog("Enregistrer le graphe sous…", Window,
				FileChooserAction.Save,
				new object[]{"Annuler",ResponseType.Cancel,
					"Enregistrer",ResponseType.Accept});

			if(Configuration.SavePath.Length > 0) {
				fc.SetCurrentFolder(System.IO.Directory.GetParent(Configuration.SavePath).FullName);
				fc.CurrentName = System.IO.Path.GetFileName(Configuration.SavePath);
			}

			fc.DoOverwriteConfirmation = true;

			if(fc.Run() == (int)ResponseType.Accept) {
				this.Path = fc.Filename;
				if(!this.Path.EndsWith(".petri"))
					this.Path += ".petri";

				filename = System.IO.Path.GetFileName(this.Path).Split(new string[]{".petri"}, StringSplitOptions.None)[0];

				fc.Destroy();
			}
			else {
				fc.Destroy();
				return;
			}

			Window.Title = filename;
			Settings.Name = filename;

			this.Save();
		}

		public void Restore()
		{
			if(Modified) {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, MainClass.SafeMarkupFromString("Souhaitez-vous revenir à la dernière version enregistrée du graphe ? Vos modifications seront perdues."));
				d.AddButton("Annuler", ResponseType.Cancel);
				d.AddButton("Revenir", ResponseType.Accept);

				ResponseType result = (ResponseType)d.Run();

				d.Destroy();
				if(result != ResponseType.Accept) {
					return;
				}
			}

			EditorController.EditedObject = null;

			var oldPetriNet = PetriNet;

			this.ResetID();
			Settings = null;

			try {
				if(Path == "") {
					PetriNet = new RootPetriNet(this);
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
					Modified = false;
					Blank = true;
				}
				else {
					this.Load();
					Blank = false;
					Modified = false;
					Window.Title = System.IO.Path.GetFileName(this.Path).Split(new string[]{".petri"}, StringSplitOptions.None)[0];
				}
			}
			catch(Exception e) {
				if(oldPetriNet != null) {
					// The document was already open and the user-requested restore failed for some reason. What could we do?
					// At least the modified document is preserved so that the user has a chance to save his work.
				}
				else {
					// If it is a fresh opening, just get back to an empty state.
					PetriNet = new RootPetriNet(this);
					Window.EditorGui.View.CurrentPetriNet = PetriNet;
					Settings = null;
					Modified = false;
					this.Blank = true;
				}


				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, MainClass.SafeMarkupFromString("Une erreur est survenue lors de du chargement du document : " + e.ToString()));
				d.AddButton("OK", ResponseType.Cancel);
				d.Run();
				d.Destroy();
			}
			if(Settings == null) {
				Settings = DocumentSettings.GetDefaultSettings();
			}
			Window.EditorGui.View.CurrentPetriNet = PetriNet;
			Window.DebugGui.View.CurrentPetriNet = PetriNet;

			this.CurrentController = this.EditorController;
			Window.Gui = Window.EditorGui;
		}

		public void SaveCpp() {
			if(Path != "") {
				if(Settings.SourceOutputPath != "") {
					this.SaveCppDontAsk();
				}
				else {
					var fc = new Gtk.FileChooserDialog("Enregistrer le code généré sous…", Window,
						         FileChooserAction.SelectFolder,
						         new object[] {"Annuler", ResponseType.Cancel,
							"Enregistrer", ResponseType.Accept
						});

					if(fc.Run() == (int)ResponseType.Accept) {
						string old = Settings.SourceOutputPath;
						this.Settings.SourceOutputPath = GetRelativeToDoc(fc.Filename);
						Modified = old != Settings.SourceOutputPath;

						this.SaveCppDontAsk();
					}

					fc.Destroy();
				}
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, "Veuillez enregistrer le document avant de générer le code C++.");

				d.Run();
				d.Destroy();
			}
		}

		public override bool Compile() {
			if(this.Path != "") {
				var c = new CppCompiler(this);
				var o = c.CompileSource(Settings.Name + ".cpp", Settings.Name);
				if(o != "") {
					MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, "La compilation a échoué. Souhaitez-vous consulter les erreurs générées ?");
					d.AddButton("Non", ResponseType.Cancel);
					d.AddButton("Oui", ResponseType.Accept);
					d.DefaultResponse = ResponseType.Accept;

					ResponseType result = (ResponseType)d.Run();

					d.Destroy();
					if(result == ResponseType.Accept) {
						string sourcePath = Settings.GetSourcePath(Settings.Name + ".cpp");
						o = "Invocation du compilateur :\n" + Settings.Compiler + " " + Settings.CompilerArgumentsForSource(sourcePath, Settings.Name) + "\n\nErreurs :\n" + o;
						new CompilationErrorPresenter(this, o).Show();
					}

					return false;
				}

				return true;
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, "Veuillez enregistrer le document avant de le compiler.");

				d.Run();
				d.Destroy();

				return true;
			}
		}

		public void ManageMacros() {
			if(_macrosManager == null) {
				_macrosManager = new MacrosManager(this);
			}

			_macrosManager.Show();
		}

		public void ManageHeaders() {
			if(this.Path != "") {
				if(_headersManager == null) {
					_headersManager = new HeadersManager(this);
				}

				_headersManager.Show();
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, "Veuillez enregistrer le document avant d'en modifier les headers.");

				d.Run();
				d.Destroy();
			}
		}

		public void EditSettings() {
			if(this.Path != "") {
				if(_settingsEditor == null) {
					_settingsEditor = new SettingsEditor(this);
				}

				_settingsEditor.Show();
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, "Veuillez enregistrer le document avant d'en modifier les réglages.");

				d.Run();
				d.Destroy();
			}
		}

		public UndoManager UndoManager {
			get;
			private set;
		}

		private void UpdateUndo() {
			// If we fall back to the state we consider unmodified, let it be considered so
			Modified = UndoManager.NextUndo != this._guiActionToMatchSave;

			Window.RevertItem.Sensitive = this.Modified;

			this.UpdateMenuItems();

			(Window.UndoItem.Child as Label).Text = "Annuler" + (UndoManager.NextUndo != null ? " " + UndoManager.NextUndoDescription : "");
			(Window.RedoItem.Child as Label).Text = "Rétablir" + (UndoManager.NextRedo != null ? " " + UndoManager.NextRedoDescription : "");
		}

		public void UpdateMenuItems() {
			CurrentController.UpdateMenuItems();
			Window.UndoItem.Sensitive = UndoManager.NextUndo != null;
			Window.RedoItem.Sensitive = UndoManager.NextRedo != null;
		}

		GuiAction _guiActionToMatchSave = null;
		HeadersManager _headersManager;
		MacrosManager _macrosManager;
		SettingsEditor _settingsEditor;
		bool _modified;
	}
}

