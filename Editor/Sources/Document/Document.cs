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

namespace Petri.Editor
{
    public class LanguageChangeEventArgs : EventArgs
    {
        public LanguageChangeEventArgs(Code.Language l)
        {
            NewLanguage = l;
        }

        public Code.Language NewLanguage {
            get;
            private set;
        }
    }

    public delegate void LanguageChangeEventHandler(object sender, LanguageChangeEventArgs e);

    /// <summary>
    /// A document with all the GUI attached to it, including the editor and the debugger.
    /// </summary>
    public class Document : HeadlessDocument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.Document"/> class.
        /// </summary>
        /// <param name="path">The path ot the document to load, or a empty string if new document.</param>
        public Document(string path) : base(path)
        {
            Window = new MainWindow(this);

            this.UndoManager = new UndoManager();

            EditorController = new EditorController(this);
            DebugController = new DebugController(this);

            this.CurrentController = EditorController;

            this.Path = path;
            this.Blank = true;
            this.Restore();
            Window.ShowAll();
            Window.EditorGui.Paned.Position = Window.Allocation.Width - 260;
            Window.DebugGui.Paned.Position = Window.Allocation.Width - 200;
            AssociatedWindows = new HashSet<Window>();

            if(path == "") {
                Window.UpdateRecentDocuments();
            }
        }

        /// <summary>
        /// Gets the associated windows of the document, such as the document settings window, the header/macro managers and the search dialog.
        /// </summary>
        /// <value>The associated windows.</value>
        public HashSet<Window> AssociatedWindows {
            get;
            private set;
        }

        /// <summary>
        /// The window of the document.
        /// </summary>
        /// <value>The window.</value>
        public MainWindow Window {
            get;
            private set;
        }

        /// <summary>
        /// Gets the editor controller.
        /// </summary>
        /// <value>The editor controller.</value>
        public EditorController EditorController {
            get;
            private set;
        }

        /// <summary>
        /// Gets the debug controller.
        /// </summary>
        /// <value>The debug controller.</value>
        public DebugController DebugController {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current controller.
        /// </summary>
        /// <value>The current controller.</value>
        public Controller CurrentController {
            get;
            private set;
        }

        /// <summary>
        /// Switchs to the debug view.
        /// </summary>
        public void SwitchToDebug()
        {
            CurrentController = DebugController;
            Window.Gui = Window.DebugGui;
        }

        /// <summary>
        /// Switchs to the editor view.
        /// </summary>
        public void SwitchToEditor()
        {
            CurrentController = EditorController;
            Window.Gui = Window.EditorGui;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.Document"/> is newly created and has not been modified.
        /// </summary>
        /// <value><c>true</c> if blank; otherwise, <c>false</c>.</value>
        public bool Blank {
            get;
            set;
        }

        /// <summary>
        /// Commits the GUI action and update the GUI accordingly (undo/redo submenus etc.).
        /// </summary>
        /// <param name="a">The alpha component.</param>
        public void CommitGuiAction(GuiAction a)
        {
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

            UndoManager.CommitGuiAction(a);
            UpdateUndo();
            if(a.Focus is Entity && CurrentController == EditorController) {
                Window.EditorGui.View.SelectedEntity = (Entity)a.Focus;
            }
            Window.Gui.BaseView.Redraw();
        }

        /// <summary>
        /// Asks the undo manager to undo the last committed GUI action, and update some GUI accordingly.
        /// </summary>
        public void Undo()
        {
            UndoManager.Undo();
            var focus = UndoManager.NextRedo.Focus;
            focus.Focus();

            UpdateUndo();
            Window.Gui.Redraw();
        }

        /// <summary>
        /// Asks the undo manager to redo the last undone GUI action, and update some GUI accordingly.
        /// </summary>
        public void Redo()
        {
            UndoManager.Redo();
            var focus = UndoManager.NextUndo.Focus;
            focus.Focus();

            UpdateUndo();
            Window.Gui.Redraw();
        }

        /// <summary>
        /// Gets the size of the window.
        /// </summary>
        /// <returns>The window size: {Width, Height}.</returns>
        protected override Tuple<int, int> GetWindowSize()
        {
            int w, h;
            Window.GetSize(out w, out h);
            return Tuple.Create(w, h);
        }

        /// <summary>
        /// Sets the size of the window.
        /// </summary>
        /// <param name="w">The width.</param>
        /// <param name="h">The height.</param>
        protected override void SetWindowSize(int w, int h)
        {
            Window.Resize(w, h);
        }

        /// <summary>
        /// Gets the window position.
        /// </summary>
        /// <returns>The window position: {X, Y}.</returns>
        protected override Tuple<int, int> GetWindowPosition()
        {
            int x, y;
            Window.GetPosition(out x, out y);
            return Tuple.Create(x, y);
        }

        /// <summary>
        /// Sets the window position.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        protected override void SetWindowPosition(int x, int y)
        {
            Window.Move(x, y);
        }

        /// <summary>
        /// Adds a header to the document.
        /// </summary>
        /// <param name="header">The header's path.</param>
        public void AddHeader(string header)
        {
            if(header.Length > 0) {
                CommitGuiAction(new AddHeaderAction(this, header));
            }
        }

        /// <summary>
        /// Removes a header from the document.
        /// </summary>
        /// <param name="header">The header's path.</param>
        public void RemoveHeader(string header)
        {
            CommitGuiAction(new RemoveHeaderAction(this, header));
        }

        /// <summary>
        /// Removes a header and removes the functions it contains from the known functions.
        /// </summary>
        /// <param name="header">The header's path.</param>
        public void RemoveHeaderNoUpdate(string header)
        {
            string filename = header;

            // If path is relative, then make it absolute
            if(!System.IO.Path.IsPathRooted(header)) {
                filename = System.IO.Path.Combine(System.IO.Directory.GetParent(this.Path).FullName,
                                                  filename);
            }

            AllFunctionsList.RemoveAll(s => s.Header == filename);

            Headers.Remove(header);
        }

        /// <summary>
        /// Reloads the document's headers if necessary, i.e. if they have been updated since the last time we read them.
        /// </summary>
        public void ReloadHeadersIfNecessary()
        {
            bool needsToReload = false;
            foreach(string h in Headers) {
                string hh = GetAbsoluteFromRelativeToDoc(h);
                if(!System.IO.File.Exists(hh) || System.IO.File.GetLastWriteTime(hh) > LastHeadersUpdate) {
                    needsToReload = true;
                    break;
                }
            }

            if(needsToReload) {
                var backup = new List<string>(Headers);

                LastHeadersUpdate = DateTime.Now;

                foreach(var h in backup) {
                    RemoveHeaderNoUpdate(h);
                }

                foreach(var h in backup) {
                    AddHeaderNoUpdate(h);
                }

                DispatchFunctions();
            }
        }

        /// <summary>
        /// Saves the document to disk, and asks for a save path if the document has never been saved.
        /// </summary>
        public override void Save()
        {
            if(Path == "") {
                if(SaveAs()) {
                    // We require the current undo stack to represent an unmodified state
                    _guiActionToMatchSave = UndoManager.NextUndo;
                }
            }
            else {
                base.Save();
                // We require the current undo stack to represent an unmodified state
                _guiActionToMatchSave = UndoManager.NextUndo;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Petri.Editor.Document"/> has been  modified since its last save.
        /// </summary>
        /// <value><c>true</c> if modified; otherwise, <c>false</c>.</value>
        public bool Modified {
            get {
                return _guiActionToMatchSave != UndoManager.NextUndo;
            }
        }

        /// <summary>
        /// Closes the document and asks a confirmation first, so the user can review his change and cancel the process.
        /// </summary>
        /// <returns><c>true</c>, if the document was allowed to be closed, <c>false</c> otherwise.</returns>
        public bool CloseAndConfirm()
        {
            if(this.DebugController.Client.SessionRunning) {
                Window.Present();
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Question,
                                                    ButtonsType.None,
                                                    Configuration.GetLocalized("A debugger session is still running. Do you want to stop it?"));
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
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Question,
                                                    ButtonsType.None,
                                                    Configuration.GetLocalized("Do you want to save the changes made to the graph? If you don't save, all changes will be permanently lost."));
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
                w.Destroy();
            }

            Application.RemoveDocument(this);

            return true;
        }

        /// <summary>
        /// Exports the petri net as a PDF document. It asks the user for the save path, and the creates a multipage document, with each page corresponding to instances of the PetriNet class.
        /// So, the root petri net is exported as well as each inner petri nets, each one on its page.
        /// </summary>
        public void ExportAsPDF()
        {
            string exportPath = "";

            var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Export the PDF as…"), Window,
                                               FileChooserAction.Save,
                                               new object[] {Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
                Configuration.GetLocalized("Save"), ResponseType.Accept
            });

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

        /// <summary>
        /// Saves the document as a new document, at a path asked to the user.
        /// </summary>
        /// <returns><c>false</c>, if the process was cancelled, <c>true</c> otherwise.</returns>
        public bool SaveAs()
        {
            if(!_saving) {
                try {
                    _saving = true;

                    string filename = null;

                    var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Save the graph as…"), Window,
                                                       FileChooserAction.Save,
                                                       new object[] {Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
                        Configuration.GetLocalized("Save"), ResponseType.Accept
                    });

                    if(Configuration.SavePath.Length > 0) {
                        fc.SetCurrentFolder(System.IO.Directory.GetParent(Configuration.SavePath).FullName);
                        fc.CurrentName = System.IO.Path.GetFileName(Configuration.SavePath);
                    }

                    fc.DoOverwriteConfirmation = true;

                    if(fc.Run() == (int)ResponseType.Accept) {
                        this.Path = fc.Filename;
                        if(!this.Path.EndsWith(".petri"))
                            this.Path += ".petri";

                        filename = System.IO.Path.GetFileName(this.Path).Split(new string[]{ ".petri" },
                                                                               StringSplitOptions.None)[0];

                        fc.Destroy();
                    }
                    else {
                        fc.Destroy();
                        return false;
                    }

                    Window.Title = filename;
                    Settings.Name = filename;

                    this.Save();

                    Application.AddRecentDocument(Path);

                    return true;
                }
                finally {
                    _saving = false;
                }
            }

            return false;
        }

        /// <summary>
        /// Reverts/Restores the document to the content of its file.
        /// If the <see cref="Document.Path"/> is empty, then this method sets up a new untitled document.
        /// On error, the error message is presented to the user, and the document is left in the state as before this call (strong exception guarantee).
        /// </summary>
        public void Restore()
        {
            if(Modified) {
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Warning,
                                                    ButtonsType.None,
                                                    Application.SafeMarkupFromString(Configuration.GetLocalized("Do you want to revert the graph to the last opened version? ? All changes will be permanently lost.")));
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
                    foreach(var d in Application.Documents) {
                        if(d.Window.Title.StartsWith(prefix)) {
                            int id = 0;
                            if(int.TryParse(d.Window.Title.Substring(prefix.Length), out id)) {
                                docID = id + 1;
                            }
                        }
                    }
                    Window.Title = prefix + docID.ToString();
                    UndoManager.Clear();
                    // We require the current undo stack to represent an unmodified state
                    _guiActionToMatchSave = UndoManager.NextUndo;
                    Blank = true;
                }
                else {
                    this.Load();
                    UndoManager.Clear();
                    // We require the current undo stack to represent an unmodified state
                    _guiActionToMatchSave = UndoManager.NextUndo;
                    Window.Title = System.IO.Path.GetFileName(this.Path).Split(new string[]{ ".petri" },
                                                                               StringSplitOptions.None)[0];
                }
            }
            catch(Exception e) {
                if(oldPetriNet != null) {
                    // The document was already opened and the user-requested restore failed for some reason. What could we do?
                    // At least the modified document is preserved so that the user has a chance to save his work.
                }
                else {
                    // If it is a fresh opening, just get back to an empty state.
                    PetriNet = new RootPetriNet(this);
                    Window.EditorGui.View.CurrentPetriNet = PetriNet;
                    Settings = null;
                    // We require the current undo stack to represent an unmodified state
                    UndoManager.Clear();
                    _guiActionToMatchSave = UndoManager.NextUndo;
                    this.Blank = true;
                }


                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Error,
                                                    ButtonsType.None,
                                                    Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred upon document loading:") + " " + e.Message));
                d.AddButton(Configuration.GetLocalized("OK"), ResponseType.Cancel);
                d.Run();
                d.Destroy();
            }
            if(Settings == null) {
                Settings = DocumentSettings.GetDefaultSettings(this);
            }

            DebugController.DebugEditor = new DebugEditor(this, null);

            Window.EditorGui.View.CurrentPetriNet = PetriNet;
            Window.DebugGui.View.CurrentPetriNet = PetriNet;

            SwitchToEditor();
        }

        /// <summary>
        /// Generates the document's code.
        /// If the code has never been generated for this document, a file save dialog asks for where the code is to be saved.
        /// Presents an error in case the document has not been saved for the first time.
        /// </summary>
        public void GenerateCode()
        {
            if(Path != "") {
                if(Settings.RelativeSourceOutputPath != "") {
                    try {
                        _codeRanges = this.GenerateCodeDontAsk();
                        _modifiedSinceGeneration = false;
                        Window.EditorGui.Status = Configuration.GetLocalized("The <language> code has been sucessfully generated.",
                                                                             Settings.LanguageName());
                    }
                    catch(Exception e) {
                        MessageDialog d = new MessageDialog(Window,
                                                            DialogFlags.Modal,
                                                            MessageType.Error,
                                                            ButtonsType.None,
                                                            Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred upon code generation:") + " " + e.Message));
                        d.AddButton(Configuration.GetLocalized("OK"), ResponseType.Cancel);
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
                        string oldPath = Settings.RelativeSourceOutputPath;
                        var newPath = GetRelativeToDoc(fc.Filename);

                        if(newPath != oldPath) {
                            var newSettings = Settings.Clone();
                            newSettings.RelativeSourceOutputPath = newPath;
                            CommitGuiAction(new ChangeSettingsAction(this,
                                                                     newSettings));
                        }

                        _codeRanges = this.GenerateCodeDontAsk();
                    }

                    fc.Destroy();
                }
            }
            else {
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Error,
                                                    ButtonsType.Cancel,
                                                    Configuration.GetLocalized("Please save the document before generating the <language> source code.",
                                                                               Settings.LanguageName()));

                d.Run();
                d.Destroy();
            }
        }

        /// <summary>
        /// Compile the document's generated code, asynchronously or not, depending on the parameter.
        /// If the document has been modified since the last code generation, the code is generated again.
        /// Presents an error in case the document has not been saved for the first time.
        /// </summary>
        /// <param name="wait">If set to <c>true</c> the compilation process is synchronous. If <c>false</c>, it is asynchronous.</param>
        public override bool Compile(bool wait)
        {
            if(this.Path != "") {
                Conflicting.Clear();

                if(_modifiedSinceGeneration) {
                    GenerateCode();
                }
                Window.Gui.Status = Configuration.GetLocalized("Compiling…");
                Task<bool> t = Task.Run((System.Func<bool>)CompileTask);
                if(wait) {
                    t.Wait();
                }
                return t.Result;
            }
            else {
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Error,
                                                    ButtonsType.Cancel,
                                                    Configuration.GetLocalized("Please save the document before compiling it."));

                d.Run();
                d.Destroy();

                return false;
            }
        }

        /// <summary>
        /// Parses the compilation errors and attempt to give a meaningful diagnostic.
        /// </summary>
        /// <param name="errors">The error string.</param>
        protected override void ParseCompilationErrors(string errors)
        {
            string linePattern = "(?<line>(\\d+))", rowPattern = "(?<row>(\\d+))";
            string pattern = "^" + Settings.RelativeSourcePath.Replace(".", "\\.") + "((:" + linePattern + ":" + rowPattern + ")|(\\(" + linePattern + "," + rowPattern + "\\))): error:? (?<msg>(.*))$";
            var lines = errors.Split(new string[] { Environment.NewLine },
                                     StringSplitOptions.RemoveEmptyEntries);

            foreach(string line in lines) {
                var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                if(match.Success) {
                    int lineNumber = int.Parse(match.Groups["line"].Value);
                    if(_codeRanges != null) {
                        // TODO: sort + binary search
                        foreach(var entry in _codeRanges) {
                            if(lineNumber >= entry.Value.FirstLine && lineNumber <= entry.Value.LastLine) {
                                AddConflicting(entry.Key,
                                               Configuration.GetLocalized("Line {0}, Row {1}:",
                                                                          lineNumber,
                                                                          match.Groups["row"].Value) + "\n" + match.Groups["msg"].Value);
                                break;
                            }
                        }
                    }
                }
            }

            if(Conflicting.Count > 1) {
                Window.EditorGui.Status = Configuration.GetLocalized("{0} conflicting entities.",
                                                                     Conflicting.Count);
            }
            else if(Conflicting.Count == 1) {
                Window.EditorGui.Status = Configuration.GetLocalized("1 confliting entity.");
            }
            else {
                Window.EditorGui.Status = Configuration.GetLocalized("No conflicting entity.");
            }

            Window.EditorGui.View.Redraw();
        }

        /// <summary>
        /// An method that is meant to be called asynchronously and that compiles the document's generated code.
        /// </summary>
        private bool CompileTask()
        {
            Window.EditorGui.Compilation = true;
            Window.DebugGui.Compilation = true;

            try {
                var c = new Compiler(this);
                var o = c.CompileSource(Settings.RelativeSourcePath, Settings.RelativeLibPath);
                if(o != "") {
                    Application.RunOnUIThread(() => {
                        ParseCompilationErrors(o);

                        MessageDialog d = new MessageDialog(Window,
                                                            DialogFlags.Modal,
                                                            MessageType.Warning,
                                                            ButtonsType.None,
                                                            Configuration.GetLocalized("The compilation has failed. Do you want to see the generated errors?"));
                        d.AddButton(Configuration.GetLocalized("No"), ResponseType.Cancel);
                        d.AddButton(Configuration.GetLocalized("Yes"), ResponseType.Accept);
                        d.DefaultResponse = ResponseType.Accept;

                        ResponseType result = (ResponseType)d.Run();

                        d.Destroy();
                        if(result == ResponseType.Accept) {
                            o = Configuration.GetLocalized("Compiler invocation:") + "\n" + Settings.Compiler + " " + Settings.CompilerArguments(Settings.RelativeSourcePath,
                                                                                                                                                 Settings.RelativeLibPath) + "\n\n" + Configuration.GetLocalized("Compilation errors:") + "\n" + o;
                            new CompilationErrorPresenter(this, o).Show();
                        }

                        Window.Gui.Status = Configuration.GetLocalized("The compilation has failed.");
                    });

                    return false;
                }
                else {
                    Application.RunOnUIThread(() => { 
                        Window.Gui.Status = Configuration.GetLocalized("The compilation has been successful.");
                    });

                    return true;
                }
            }
            finally {
                Window.EditorGui.Compilation = false;
                Window.DebugGui.Compilation = false;
            }
        }

        /// <summary>
        /// Shows the document's preprocessor macros' manager.
        /// </summary>
        public void ManageMacros()
        {
            if(_macrosManager == null) {
                _macrosManager = new MacrosManager(this);
            }

            _macrosManager.Show();
        }

        /// <summary>
        /// Show the document's headers editor, or presents an error in case the document has not been saved for the first time.
        /// </summary>
        public void ManageHeaders()
        {
            if(this.Path != "") {
                if(_headersManager == null) {
                    _headersManager = new HeadersManager(this);
                }

                _headersManager.Show();
            }
            else {
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Error,
                                                    ButtonsType.Cancel,
                                                    Configuration.GetLocalized("Please save the document before changing its headers."));

                d.Run();
                d.Destroy();
            }
        }

        /// <summary>
        /// Show the document's settings editor, or presents an error in case the document has not been saved for the first time.
        /// </summary>
        public void EditSettings()
        {
            if(this.Path != "") {
                if(_settingsEditor == null) {
                    _settingsEditor = new DocumentSettingsEditor(this);
                }

                _settingsEditor.Show();
            }
            else {
                MessageDialog d = new MessageDialog(Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Error,
                                                    ButtonsType.Cancel,
                                                    Configuration.GetLocalized("Please save the document before changing its settings."));

                d.Run();
                d.Destroy();
            }
        }

        /// <summary>
        /// The document's undo manager.
        /// </summary>
        /// <value>The undo manager.</value>
        public UndoManager UndoManager {
            get;
            private set;
        }

        /// <summary>
        /// Updates the undo/redo menu items titles to the correct GUI action description, enables or disables the Revert to saved action, and checks whether the document is in its last saved state or not.
        /// </summary>
        private void UpdateUndo()
        {
            this.Blank = false;
            _modifiedSinceGeneration = true;

            Window.RevertItem.Sensitive = this.Modified;

            this.UpdateMenuItems();

            (Window.UndoItem.Child as Label).Text = (UndoManager.NextUndo != null ? Configuration.GetLocalized("Undo {0}",
                                                                                                               UndoManager.NextUndoDescription) : Configuration.GetLocalized("Undo"));
            (Window.RedoItem.Child as Label).Text = (UndoManager.NextRedo != null ? Configuration.GetLocalized("Redo {0}",
                                                                                                               UndoManager.NextRedoDescription) : Configuration.GetLocalized("Redo"));
        }

        /// <summary>
        /// Updates some of the menu items' sensitivity to some conditions.
        /// </summary>
        public void UpdateMenuItems()
        {
            CurrentController.UpdateMenuItems();
            Window.UndoItem.Sensitive = UndoManager.NextUndo != null;
            Window.RedoItem.Sensitive = UndoManager.NextRedo != null;
            Window.FindItem.Sensitive = Window.Gui == Window.EditorGui;
        }

        GuiAction _guiActionToMatchSave = null;
        HeadersManager _headersManager;
        MacrosManager _macrosManager;
        DocumentSettingsEditor _settingsEditor;
        bool _modifiedSinceGeneration = true;
        Dictionary<Entity, CodeRange> _codeRanges = null;
        bool _saving = false;
    }
}

