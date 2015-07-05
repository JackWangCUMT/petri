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
using System.Xml.Linq;
using System.Xml;
using Gtk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Petri
{
	public class LanguageChangeEventArgs : EventArgs {
		public LanguageChangeEventArgs(Language l) {
			NewLanguage = l;
		}

		public Language NewLanguage {
			get;
			private set;
		}
	}

	public delegate void LanguageChangeEventHandler(object sender, LanguageChangeEventArgs e);

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

		public event LanguageChangeEventHandler LanguageChanged;

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
			if(a is ConditionChangeAction) {
				Transition t = (a as ConditionChangeAction).Transition;
				if(Conflicts(t)) {
					Conflicting.Remove(t);
				}
			}
			else if(a is InvocationChangeAction) {
				Action action = (a as InvocationChangeAction).Action;
				if(Conflicts(action)) {
					Conflicting.Remove(action);
				}
			}
			Modified = true;
			UndoManager.PostAction(a);
			UpdateUndo();
			if(a.Focus is Entity && CurrentController == EditorController) {
				Window.EditorGui.View.SelectedEntity = (Entity)a.Focus;
			}
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

		public void RemoveHeader(string header) {
			RemoveHeaderNoUpdate(header);
			DispatchFunctions();
			UpdateConflicts();

			Modified = true;
		}

		public void OnLanguageChanged() {
			if(LanguageChanged != null) {
				LanguageChanged(this, new LanguageChangeEventArgs(Settings.Language));
			}
		}

		private void RemoveHeaderNoUpdate(string header) {
			string filename = header;

			// If path is relative, then make it absolute
			if(!System.IO.Path.IsPathRooted(header)) {
				filename = System.IO.Path.Combine(System.IO.Directory.GetParent(this.Path).FullName, filename);
			}

			AllFunctionsList.RemoveAll(s => s.Header == filename);

			Headers.Remove(header);
		}

		public void ReloadHeaders() {
			var backup = Headers.ToArray();

			foreach(var h in backup) {
				RemoveHeaderNoUpdate(h);
			}

			foreach(var h in backup) {
				AddHeaderNoUpdate(h);
			}

			DispatchFunctions();
			UpdateConflicts();
		}

		public override void Save() {
			if(Path == "") {
				if(SaveAs()) {
					Modified = false;
				}
			}
			else {
				base.Save();
				Modified = false;
			}
		}

		public bool Modified {
			get {
				return _modified || (Settings != null && Settings.Modified);
			}
			set {
				_modified = value;
				if(value == true) {
					this.Blank = false;
					_modifiedSinceGeneration = true;
				}
				else {
					// We require the current undo stack to represent an unmodified state
					_guiActionToMatchSave = UndoManager.NextUndo;
				}
			}
		}

		public bool CloseAndConfirm() {
			if(this.DebugController.Client.SessionRunning) {
				Window.Present();
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, Configuration.GetLocalized("A debugger session is still running. Do you want to stop it?"));
				d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
				d.AddButton(Configuration.GetLocalized("Stop the session"), ResponseType.Yes).HasDefault = true;

				ResponseType result = (ResponseType)d.Run();

				if(result == ResponseType.Yes) {
					DebugController.Client.Detach();
					d.Destroy();
				}
				else {
					d.Destroy();
					return false;
				}
			}

			if(Modified) {
				Window.Present();
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, Configuration.GetLocalized("Do you want to save the changes made to the graph? If you don't save, all changes will be permanently lost."));
				d.AddButton(Configuration.GetLocalized("Don't save"), ResponseType.No);
				d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
				d.AddButton(Configuration.GetLocalized("Save"), ResponseType.Yes).HasDefault = true;

				ResponseType result = (ResponseType)d.Run();

				if(result == ResponseType.Yes) {
					Save();
					d.Destroy();
					if(Modified) {
						return false;
					}
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

			var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Export the PDF as…"), Window,
				FileChooserAction.Save,
				new object[]{Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
					Configuration.GetLocalized("Save"), ResponseType.Accept});

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

		public bool SaveAs() {
			string filename = null;

			var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Save the graph as…"), Window,
				FileChooserAction.Save,
				new object[]{Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
					Configuration.GetLocalized("Save"), ResponseType.Accept});

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
				return false;
			}

			Window.Title = filename;
			Settings.Name = filename;

			this.Save();

			return true;
		}

		public void Restore()
		{
			if(Modified) {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, MainClass.SafeMarkupFromString(Configuration.GetLocalized("Do you want to revert the graph to the last opened version? ? All changes will be permanently lost.")));
				d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
				d.AddButton(Configuration.GetLocalized("Revert"), ResponseType.Accept);

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
					string prefix = Configuration.GetLocalized("Untitled") + " ";
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


				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, MainClass.SafeMarkupFromString(Configuration.GetLocalized("An error occurred upon document loading:") + " " + e.Message));
				d.AddButton(Configuration.GetLocalized("OK"), ResponseType.Cancel);
				d.Run();
				d.Destroy();
			}
			if(Settings == null) {
				Settings = DocumentSettings.GetDefaultSettings(this);
			}

			OnLanguageChanged();

			Window.EditorGui.View.CurrentPetriNet = PetriNet;
			Window.DebugGui.View.CurrentPetriNet = PetriNet;

			SwitchToEditor();
		}

		public void SaveCpp() {
			if(Path != "") {
				if(Settings.SourceOutputPath != "") {
					if(!Conflicts(PetriNet)) {
						this.SaveCppDontAsk();
						_modifiedSinceGeneration = false;
						Window.EditorGui.Status = Configuration.GetLocalized("The <language> code has been sucessfully generated.", Settings.LanguageName());
					}
					else {
						MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, Configuration.GetLocalized("The Petri Net contains conflicting entities. Please solve them before you can generate the source code."));
						d.AddButton(Configuration.GetLocalized("OK"), ResponseType.Accept);
						d.Run();
						d.Destroy();
					}
				}
				else {
					var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Save the generated code as…"), Window,
								FileChooserAction.SelectFolder,
								new object[] {Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
								Configuration.GetLocalized("Save"), ResponseType.Accept
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
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, Configuration.GetLocalized("Please save the document before generating the <language> source code.", Settings.LanguageName()));

				d.Run();
				d.Destroy();
			}
		}

		public override bool Compile(bool wait) {
			if(this.Path != "") {
				if(_modifiedSinceGeneration) {
					SaveCpp();
				}
				Window.Gui.Status = Configuration.GetLocalized("Compiling…");
				Task t = Task.Run((System.Action)CompileTask);
				if(wait) {
					t.Wait();
				}
				return true;
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, Configuration.GetLocalized("Please save the document before compiling it."));

				d.Run();
				d.Destroy();

				return true;
			}
		}

		private void CompileTask() {
			Window.EditorGui.Compilation = true;
			Window.DebugGui.Compilation = true;

			var c = new CppCompiler(this);
			var o = c.CompileSource(Settings.SourcePath, Settings.LibPath);
			if(o != "") {
				GLib.Timeout.Add(0, () => {
					MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, Configuration.GetLocalized("The compilation has failed. Do you want to see the generated errors?"));
					d.AddButton(Configuration.GetLocalized("No"), ResponseType.Cancel);
					d.AddButton(Configuration.GetLocalized("Yes"), ResponseType.Accept);
					d.DefaultResponse = ResponseType.Accept;

					ResponseType result = (ResponseType)d.Run();

					d.Destroy();
					if(result == ResponseType.Accept) {
						o = Configuration.GetLocalized("Compiler invocation:") + "\n" + Settings.Compiler + " " + Settings.CompilerArguments(Settings.SourcePath, Settings.LibPath) + "\n\n" + Configuration.GetLocalized("Errors:") + "\n" + o;
						new CompilationErrorPresenter(this, o).Show();
					}

					Window.Gui.Status = Configuration.GetLocalized("The compilation has failed.");

					return false;
				});
			}
			else {
				GLib.Timeout.Add (0, () => { 
					Window.Gui.Status = Configuration.GetLocalized("The compilation has been successful.");
					return false;
				});
			}

			Window.EditorGui.Compilation = false;
			Window.DebugGui.Compilation = false;
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
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, Configuration.GetLocalized("Please save the document before changing its headers."));

				d.Run();
				d.Destroy();
			}
		}

		public void EditSettings() {
			if(this.Path != "") {
				if(_settingsEditor == null) {
					_settingsEditor = new DocumentSettingsEditor(this);
				}

				_settingsEditor.Show();
			}
			else {
				MessageDialog d = new MessageDialog(Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel, Configuration.GetLocalized("Please save the document before changing its settings."));

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

			(Window.UndoItem.Child as Label).Text = (UndoManager.NextUndo != null ? Configuration.GetLocalized("Undo {0}", UndoManager.NextUndoDescription) : Configuration.GetLocalized("Undo"));
			(Window.RedoItem.Child as Label).Text = (UndoManager.NextRedo != null ? Configuration.GetLocalized("Redo {0}", UndoManager.NextRedoDescription) : Configuration.GetLocalized("Redo"));
		}

		public void UpdateMenuItems() {
			CurrentController.UpdateMenuItems();
			Window.UndoItem.Sensitive = UndoManager.NextUndo != null;
			Window.RedoItem.Sensitive = UndoManager.NextRedo != null;
			Window.FindItem.Sensitive = Window.Gui == Window.EditorGui;
		}

		public override void UpdateConflicts() {
			base.UpdateConflicts();
			if(Conflicting.Count > 1) {
				Window.EditorGui.Status = Configuration.GetLocalized("{0} conflicting entities.", Conflicting.Count);
			}
			else if(Conflicting.Count == 1) {
				Window.EditorGui.Status = Configuration.GetLocalized("1 confliting entity.");
			}
			else {
				Window.EditorGui.Status = Configuration.GetLocalized("No conflicting entity.");
			}
			if(Conflicting.Count > 0) {
				Window.EditorGui.Status += " " + Configuration.GetLocalized("Conflicts have to be solved before the generation/compilation step.");
			}

			Window.EditorGui.View.Redraw();
		}

		GuiAction _guiActionToMatchSave = null;
		HeadersManager _headersManager;
		MacrosManager _macrosManager;
		DocumentSettingsEditor _settingsEditor;
		bool _modified;
		bool _modifiedSinceGeneration = true;
	}
}

