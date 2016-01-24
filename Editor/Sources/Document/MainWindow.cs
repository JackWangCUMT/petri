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
using IgeMacIntegration;
using System.Collections.Generic;

namespace Petri.Editor
{
    public class MainWindow : Gtk.Window
    {
        public MainWindow(Document doc) : base(Gtk.WindowType.Toplevel)
        {
            _document = doc;

            this.WindowPosition = ((global::Gtk.WindowPosition)(4));
            this.AllowShrink = true;
            this.DefaultWidth = 920;
            this.DefaultHeight = 640;
            this.DeleteEvent += this.OnDeleteEvent;

            this.BorderWidth = 15;
            _vbox = new VBox(false, 5);
            this.Add(_vbox);

            if(Application.Documents.Count > 0) {
                int x, y;
                Application.Documents[Application.Documents.Count - 1].Window.GetPosition(out x, out y);
                this.Move(x + 20, y + 42);
            }
            else {
                this.SetPosition(WindowPosition.Center);
                int x, y;
                this.GetPosition(out x, out y);
                this.Move(x, 2 * y / 3);
            }

            this.BuildMenus();

            _editorGui = new EditorGui(_document);
            _debugGui = new DebugGui(_document);

            this.FocusInEvent += (o, args) => {
                _document.UpdateMenuItems();
                _gui.FocusIn();
                if(Configuration.RunningPlatform == Platform.Mac) {
                    _menuBar.ShowAll();
                    IgeMacMenu.MenuBar = _menuBar;
                }
            };
            this.FocusOutEvent += (o, args) => {
                _gui.FocusOut();
            };
        }

        /// <summary>
        /// Gets or sets the currently installed main view of the window (debugger, editor…).
        /// </summary>
        /// <value>The GUI.</value>
        public Gui Gui {
            get {
                return _gui;
            }
            set {
                if(_gui != null) {
                    _gui.Hide();
                    _vbox.Remove(_gui);
                }

                _gui = value;

                _vbox.PackEnd(_gui);

                _gui.Redraw();
                _gui.FocusIn();
                _gui.UpdateToolbar();
                _gui.ShowAll();
            }
        }

        /// <summary>
        /// Gets the editor GUI.
        /// </summary>
        /// <value>The editor GUI.</value>
        public EditorGui EditorGui {
            get {
                return _editorGui;
            }
        }

        /// <summary>
        /// Gets the debug GUI.
        /// </summary>
        /// <value>The debug GUI.</value>
        public DebugGui DebugGui {
            get {
                return _debugGui;
            }
        }

        /// <summary>
        /// Gets the "Undo" menu item of the menu bar.
        /// </summary>
        /// <value>The undo item.</value>
        public MenuItem UndoItem {
            get {
                return _undoItem;
            }
        }

        /// <summary>
        /// Gets the "Redo" menu item of the menu bar.
        /// </summary>
        /// <value>The redo item.</value>
        public MenuItem RedoItem {
            get {
                return _redoItem;
            }
        }

        /// <summary>
        /// Gets the "Cut" menu item of the menu bar.
        /// </summary>
        /// <value>The cut item.</value>
        public MenuItem CutItem {
            get {
                return _cutItem;
            }
        }

        /// <summary>
        /// Gets the "Copy" menu item of the menu bar.
        /// </summary>
        /// <value>The copy item.</value>
        public MenuItem CopyItem {
            get {
                return _copyItem;
            }
        }

        /// <summary>
        /// Gets the "Paste" menu item of the menu bar.
        /// </summary>
        /// <value>The paste item.</value>
        public MenuItem PasteItem {
            get {
                return _pasteItem;
            }
        }

        /// <summary>
        /// Gets the "Find" menu item of the menu bar.
        /// </summary>
        /// <value>The find item.</value>
        public MenuItem FindItem {
            get {
                return _findItem;
            }
        }

        /// <summary>
        /// Gets the "Embed in macro" menu item of the menu bar.
        /// </summary>
        /// <value>The embed item.</value>
        public MenuItem EmbedItem {
            get {
                return _embedInMacro;
            }
        }

        /// <summary>
        /// Gets the "Revert to last save" menu item of the menu bar.
        /// </summary>
        /// <value>The revert item.</value>
        public MenuItem RevertItem {
            get {
                return _revertItem;
            }
        }

        /// <summary>
        /// Updates the recent documents menu items.
        /// </summary>
        public void UpdateRecentDocuments()
        {
            _openRecentItem.Submenu = null;
            if(_openRecentMenu != null) {
                _openRecentMenu.Destroy();
            }

            _openRecentMenu = new Menu();
            _openRecentItem.Submenu = _openRecentMenu;

            var recentItems = Application.RecentDocuments;

            foreach(var pair in recentItems) {
                MenuItem item = new MenuItem(pair.Value);
                item.Activated += OnClickRecentMenu;
                _openRecentMenu.Append(item);
                item.Show();
            }

            if(recentItems.Count > 0) {
                _openRecentMenu.Append(new SeparatorMenuItem());
            }
            _clearRecentItems = new MenuItem(Configuration.GetLocalized("Clear Recent"));
            _clearRecentItems.Activated += OnClickMenu;
            _openRecentMenu.Append(_clearRecentItems);
            _clearRecentItems.Sensitive = recentItems.Count > 0;

            _openRecentItem.ShowAll();
        }

        /// <summary>
        /// Called when the window is closed by its close button.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        protected void OnDeleteEvent(object sender, DeleteEventArgs args)
        {
            bool result = _document.CloseAndConfirm();
            args.RetVal = !result;
        }

        /// <summary>
        /// Called when a recent item menu is clicked.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        protected void OnClickRecentMenu(object sender, EventArgs args)
        {
            var item = (Label)((MenuItem)sender).Child;
            Application.OpenDocument(item.Text);
        }

        /// <summary>
        /// Called when any of the non-static menu item is selected from the menu bar.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        protected void OnClickMenu(object sender, EventArgs args)
        {
            if(sender == _quitItem) {
                bool shouldExit = Application.OnExit();
                if(shouldExit) {
                    Application.SaveAndQuit();
                }
            }
            else if(sender == _saveItem) {
                _document.Save();
            }
            else if(sender == _saveAsItem) {
                _document.SaveAs();
            }
            else if(sender == _exportItem) {
                _document.ExportAsPDF();
            }
            else if(sender == _revertItem) {
                _document.Restore();
            }
            else if(sender == _undoItem) {
                _document.Undo();
            }
            else if(sender == _redoItem) {
                _document.Redo();
            }
            else if(sender == _copyItem) {
                if(Focus == Gui.BaseView)
                    _document.CurrentController.Copy();
                else if(Focus is Editable) {
                    (Focus as Editable).CopyClipboard();
                }
                else if(Focus is TextView) {
                    var v = Focus as TextView;
                    v.Buffer.CopyClipboard(v.GetClipboard(Gdk.Selection.Clipboard));
                }
            }
            else if(sender == _cutItem) {
                if(Focus == Gui.BaseView)
                    _document.CurrentController.Cut();
                else if(Focus is Editable) {
                    (Focus as Editable).CutClipboard();
                }
                else if(Focus is TextView) {
                    var v = Focus as TextView;
                    v.Buffer.CutClipboard(v.GetClipboard(Gdk.Selection.Clipboard), true);
                }
            }
            else if(sender == _pasteItem) {
                if(Focus == Gui.BaseView)
                    _document.CurrentController.Paste();
                else if(Focus is Editable) {
                    (Focus as Editable).PasteClipboard();
                }
                else if(Focus is TextView) {
                    var v = Focus as TextView;
                    v.Buffer.PasteClipboard(v.GetClipboard(Gdk.Selection.Clipboard));
                }
            }
            else if(sender == _selectAllItem) {
                if(Focus == Gui.BaseView)
                    _document.CurrentController.SelectAll();
                else if(Focus is Editable) {
                    (Focus as Editable).SelectRegion(0, -1);
                }
                else if(Focus is TextView) {
                    var v = Focus as TextView;
                    TextIter start = v.Buffer.GetIterAtOffset(0), end = v.Buffer.GetIterAtOffset(0);
                    end.ForwardToEnd();

                    v.Buffer.SelectRange(start, end);
                }
            }
            else if(sender == _embedInMacro) {
                if(_document.CurrentController == _document.EditorController) {
                    _document.EditorController.EmbedInMacro();
                }
            }
            else if(sender == _openItem) {
                Application.OpenDocument();
            }
            else if(sender == _clearRecentItems) {
                Application.RecentDocuments.Clear();
                Application.UpdateRecentDocuments();
            }
            else if(sender == _newItem) {
                var doc = new Document("");
                Application.AddDocument(doc);
            }
            else if(sender == _closeItem) {
                if(_document.CloseAndConfirm())
                    this.Destroy();
            }
            else if(sender == _showEditorItem) {
                _document.SwitchToEditor();
            }
            else if(sender == _showDebuggerItem) {
                _document.SwitchToDebug();
            }
            else if(sender == _manageHeadersItem) {
                _document.ManageHeaders();
            }
            else if(sender == _manageMacrosItem) {
                _document.ManageMacros();
            }
            else if(sender == _documentSettingsItem) {
                _document.EditSettings();
            }
            else if(sender == _actualSizeItem) {
                _document.Window.Gui.BaseView.Zoom = 1;
                _document.Window.Gui.BaseView.Redraw();
            }
            else if(sender == _zoomInItem) {
                _document.Window.Gui.BaseView.Zoom /= 0.8f;
                if(_document.Window.Gui.BaseView.Zoom > 8f) {
                    _document.Window.Gui.BaseView.Zoom = 8f;
                }
                _document.Window.Gui.BaseView.Redraw();
            }
            else if(sender == _zoomOutItem) {
                _document.Window.Gui.BaseView.Zoom *= 0.8f;
                if(_document.Window.Gui.BaseView.Zoom < 0.01f) {
                    _document.Window.Gui.BaseView.Zoom = 0.01f;
                }
                _document.Window.Gui.BaseView.Redraw();
            }
            else if(sender == _findItem) {
                _document.Window.EditorGui.Find();
            }
        }

        /// <summary>
        /// Called when a static menu item is selected.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        protected static void OnClickMenuStatic(object sender, EventArgs args)
        {
            if(sender == _staticQuitItem) {
                bool shouldExit = Application.OnExit();
                if(shouldExit) {
                    Application.SaveAndQuit();
                }
            }
        }

        /// <summary>
        /// Builds the menus and the menu bar.
        /// </summary>
        protected void BuildMenus()
        {
            _accelGroup = new AccelGroup();
            this.AddAccelGroup(_accelGroup);
            this.AddAccelGroup(_staticAccelGroup);

            _menuBar = new MenuBar();

            Menu fileMenu = new Menu();
            Menu editMenu = new Menu();
            Menu viewMenu = new Menu();
            Menu documentMenu = new Menu();
            Menu helpMenu = new Menu();
            MenuItem file = new MenuItem(Configuration.GetLocalized("File"));
            MenuItem edit = new MenuItem(Configuration.GetLocalized("Edit"));
            MenuItem view = new MenuItem(Configuration.GetLocalized("View"));
            MenuItem document = new MenuItem(Configuration.GetLocalized("Document"));
            MenuItem help = new MenuItem(Configuration.GetLocalized("Help"));
            file.Submenu = fileMenu;
            edit.Submenu = editMenu;
            view.Submenu = viewMenu;
            document.Submenu = documentMenu;
            help.Submenu = helpMenu;

            _newItem = new MenuItem(Configuration.GetLocalized("New"));
            _newItem.Activated += OnClickMenu;
            _newItem.AddAccelerator("activate",
                                    _accelGroup,
                                    new AccelKey(Gdk.Key.n,
                                                 Gdk.ModifierType.ControlMask,
                                                 AccelFlags.Visible));

            _openItem = new MenuItem(Configuration.GetLocalized("Open…"));
            _openItem.Activated += OnClickMenu;
            _openItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.o,
                                                  Gdk.ModifierType.ControlMask,
                                                  AccelFlags.Visible));

            _openRecentItem = new MenuItem(Configuration.GetLocalized("Open Recent"));

            _closeItem = new MenuItem(Configuration.GetLocalized("Close"));
            _closeItem.Activated += OnClickMenu;
            _closeItem.AddAccelerator("activate",
                                      _accelGroup,
                                      new AccelKey(Gdk.Key.w,
                                                   Gdk.ModifierType.ControlMask,
                                                   AccelFlags.Visible));

            _saveItem = new MenuItem(Configuration.GetLocalized("Save"));
            _saveItem.Activated += OnClickMenu;
            _saveItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.s,
                                                  Gdk.ModifierType.ControlMask,
                                                  AccelFlags.Visible));

            _saveAsItem = new MenuItem(Configuration.GetLocalized("Save As…"));
            _saveAsItem.Activated += OnClickMenu;
            _saveAsItem.AddAccelerator("activate",
                                       _accelGroup,
                                       new AccelKey(Gdk.Key.s,
                                                    Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask,
                                                    AccelFlags.Visible));

            _exportItem = new MenuItem(Configuration.GetLocalized("Export as PDF…"));
            _exportItem.Activated += OnClickMenu;
            _exportItem.AddAccelerator("activate",
                                       _accelGroup,
                                       new AccelKey(Gdk.Key.e,
                                                    Gdk.ModifierType.ControlMask,
                                                    AccelFlags.Visible));

            _revertItem = new MenuItem(Configuration.GetLocalized("Revert…"));
            _revertItem.Activated += OnClickMenu;
            _revertItem.Sensitive = false;

            fileMenu.Append(_newItem);
            fileMenu.Append(_openItem);
            fileMenu.Append(_openRecentItem);
            fileMenu.Append(new SeparatorMenuItem());
            fileMenu.Append(_closeItem);
            fileMenu.Append(_saveItem);
            fileMenu.Append(_saveAsItem);
            fileMenu.Append(_exportItem);
            fileMenu.Append(_revertItem);

            _undoItem = new MenuItem(Configuration.GetLocalized("Undo"));
            _undoItem.Activated += OnClickMenu;
            _undoItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.z,
                                                  Gdk.ModifierType.ControlMask,
                                                  AccelFlags.Visible));

            _redoItem = new MenuItem(Configuration.GetLocalized("Redo"));
            _redoItem.Activated += OnClickMenu;
            _redoItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.z,
                                                  Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask,
                                                  AccelFlags.Visible));

            _cutItem = new MenuItem(Configuration.GetLocalized("Cut"));
            _cutItem.Activated += OnClickMenu;
            _cutItem.AddAccelerator("activate",
                                    _accelGroup,
                                    new AccelKey(Gdk.Key.x,
                                                 Gdk.ModifierType.ControlMask,
                                                 AccelFlags.Visible));

            _copyItem = new MenuItem(Configuration.GetLocalized("Copy"));
            _copyItem.Activated += OnClickMenu;
            _copyItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.c,
                                                  Gdk.ModifierType.ControlMask,
                                                  AccelFlags.Visible));

            _selectAllItem = new MenuItem(Configuration.GetLocalized("Select All"));
            _selectAllItem.Activated += OnClickMenu;
            _selectAllItem.AddAccelerator("activate",
                                          _accelGroup,
                                          new AccelKey(Gdk.Key.a,
                                                       Gdk.ModifierType.ControlMask,
                                                       AccelFlags.Visible));

            _pasteItem = new MenuItem(Configuration.GetLocalized("Paste"));
            _pasteItem.Activated += OnClickMenu;
            _pasteItem.AddAccelerator("activate",
                                      _accelGroup,
                                      new AccelKey(Gdk.Key.v,
                                                   Gdk.ModifierType.ControlMask,
                                                   AccelFlags.Visible));

            _findItem = new MenuItem(Configuration.GetLocalized("Find…"));
            _findItem.Activated += OnClickMenu;
            _findItem.AddAccelerator("activate",
                                     _accelGroup,
                                     new AccelKey(Gdk.Key.f,
                                                  Gdk.ModifierType.ControlMask,
                                                  AccelFlags.Visible));

            _embedInMacro = new MenuItem(Configuration.GetLocalized("Group in a Macro"));
            _embedInMacro.Activated += OnClickMenu;
            _embedInMacro.AddAccelerator("activate",
                                         _accelGroup,
                                         new AccelKey(Gdk.Key.e,
                                                      Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask,
                                                      AccelFlags.Visible));

            editMenu.Append(_undoItem);
            editMenu.Append(_redoItem);
            editMenu.Append(new SeparatorMenuItem());
            editMenu.Append(_cutItem);
            editMenu.Append(_copyItem);
            editMenu.Append(_pasteItem);
            editMenu.Append(new SeparatorMenuItem());
            editMenu.Append(_selectAllItem);
            editMenu.Append(new SeparatorMenuItem());
            editMenu.Append(_findItem);
            editMenu.Append(new SeparatorMenuItem());
            editMenu.Append(_embedInMacro);

            _undoItem.Sensitive = false;
            _redoItem.Sensitive = false;
            _cutItem.Sensitive = false;
            _copyItem.Sensitive = false;
            _pasteItem.Sensitive = false;

            _actualSizeItem = new MenuItem(Configuration.GetLocalized("Actual Size"));
            _actualSizeItem.Activated += OnClickMenu;
            _actualSizeItem.AddAccelerator("activate",
                                           _accelGroup,
                                           new AccelKey(Gdk.Key.Key_0,
                                                        Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask,
                                                        AccelFlags.Visible));
            _zoomInItem = new MenuItem(Configuration.GetLocalized("Zoom In"));
            _zoomInItem.Activated += OnClickMenu;
            _zoomInItem.AddAccelerator("activate",
                                       _accelGroup,
                                       new AccelKey(Gdk.Key.plus,
                                                    Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask,
                                                    AccelFlags.Visible));
            _zoomOutItem = new MenuItem(Configuration.GetLocalized("Zoom Out"));
            _zoomOutItem.Activated += OnClickMenu;
            _zoomOutItem.AddAccelerator("activate",
                                        _accelGroup,
                                        new AccelKey(Gdk.Key.minus,
                                                     Gdk.ModifierType.ControlMask,
                                                     AccelFlags.Visible));

            viewMenu.Append(_actualSizeItem);
            viewMenu.Append(_zoomInItem);
            viewMenu.Append(_zoomOutItem);

            _showEditorItem = new MenuItem(Configuration.GetLocalized("Show Editor"));
            _showEditorItem.Activated += OnClickMenu;
            _showEditorItem.AddAccelerator("activate",
                                           _accelGroup,
                                           new AccelKey(Gdk.Key.e,
                                                        Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask,
                                                        AccelFlags.Visible));
            _showDebuggerItem = new MenuItem(Configuration.GetLocalized("Show Debugger"));
            _showDebuggerItem.Activated += OnClickMenu;
            _showDebuggerItem.AddAccelerator("activate",
                                             _accelGroup,
                                             new AccelKey(Gdk.Key.d,
                                                          Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask,
                                                          AccelFlags.Visible));
            _manageHeadersItem = new MenuItem(Configuration.GetLocalized("Manage Headers…"));
            _manageHeadersItem.Activated += OnClickMenu;
            _manageMacrosItem = new MenuItem(Configuration.GetLocalized("Manage Macros…"));
            _manageMacrosItem.Activated += OnClickMenu;
            _documentSettingsItem = new MenuItem(Configuration.GetLocalized("Settings…"));
            _documentSettingsItem.Activated += OnClickMenu;
            _documentSettingsItem.AddAccelerator("activate",
                                                 _accelGroup,
                                                 new AccelKey(Gdk.Key.comma,
                                                              Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask,
                                                              AccelFlags.Visible));

            documentMenu.Append(_showEditorItem);
            documentMenu.Append(_showDebuggerItem);
            documentMenu.Append(new SeparatorMenuItem());
            documentMenu.Append(_manageHeadersItem);
            documentMenu.Append(_manageMacrosItem);
            documentMenu.Append(_documentSettingsItem);


            _showHelpItem = new MenuItem(Configuration.GetLocalized("Help…"));
            _showHelpItem.Activated += OnClickMenu;

            helpMenu.Append(_showHelpItem);

            if(Configuration.RunningPlatform != Platform.Mac) {
                _quitItem = new MenuItem(Configuration.GetLocalized("Quit"));
                _quitItem.Activated += OnClickMenu;
                _quitItem.AddAccelerator("activate",
                                         _accelGroup,
                                         new AccelKey(Gdk.Key.q,
                                                      Gdk.ModifierType.ControlMask,
                                                      AccelFlags.Visible));

                _preferencesItem = new MenuItem(Configuration.GetLocalized("Preferences…"));
                _preferencesItem.Activated += OnClickMenu;
                _preferencesItem.AddAccelerator("activate",
                                                _accelGroup,
                                                new AccelKey(Gdk.Key.comma,
                                                             Gdk.ModifierType.ControlMask,
                                                             AccelFlags.Visible));

                _aboutItem = new MenuItem(Configuration.GetLocalized("About…"));
                _aboutItem.Activated += OnClickMenu;

                fileMenu.Append(_quitItem);
                editMenu.Append(new SeparatorMenuItem());
                editMenu.Append(_preferencesItem);
                helpMenu.Append(_aboutItem);
            }

            _menuBar.Append(file);
            _menuBar.Append(edit);
            _menuBar.Append(view);
            _menuBar.Append(document);
            _menuBar.Append(help);

            if(Configuration.RunningPlatform != Platform.Mac) {
                _vbox.PackStart(_menuBar, false, false, 0);
            }
        }

        /// <summary>
        /// Inits the static components of GUI. At the moment, it initializes the OS X menu bar and document association stuff.
        /// To be called once at application startup.
        /// </summary>
        public static void InitGUI()
        {
            _staticAccelGroup = new AccelGroup();

            _staticQuitItem = new MenuItem(Configuration.GetLocalized("Quit"));
            _staticQuitItem.Activated += OnClickMenuStatic;
            _staticQuitItem.AddAccelerator("activate",
                                           _staticAccelGroup,
                                           new AccelKey(Gdk.Key.q,
                                                        Gdk.ModifierType.ControlMask,
                                                        AccelFlags.Visible));

            _staticPreferencesItem = new MenuItem(Configuration.GetLocalized("Preferences…"));
            _staticPreferencesItem.Activated += OnClickMenuStatic;
            _staticPreferencesItem.AddAccelerator("activate",
                                                  _staticAccelGroup,
                                                  new AccelKey(Gdk.Key.comma,
                                                               Gdk.ModifierType.ControlMask,
                                                               AccelFlags.Visible));

            _staticAboutItem = new MenuItem(Configuration.GetLocalized("About…"));
            _staticAboutItem.Activated += OnClickMenuStatic;

            var aboutGroup = IgeMacMenu.AddAppMenuGroup();
            aboutGroup.AddMenuItem(_staticAboutItem, null);
            var prefsGroup = IgeMacMenu.AddAppMenuGroup();
            prefsGroup.AddMenuItem(_staticPreferencesItem, null);

            IgeMacMenu.QuitMenuItem = _staticQuitItem;

            if(Configuration.RunningPlatform == Platform.Mac) {
                MonoDevelop.MacInterop.ApplicationEvents.Quit += delegate (object sender,
                                                                           MonoDevelop.MacInterop.ApplicationQuitEventArgs e) {
                    Application.SaveAndQuit();
                    // If we get here, the user has cancelled the action
                    e.UserCancelled = true;
                    e.Handled = true;
                };

                MonoDevelop.MacInterop.ApplicationEvents.OpenDocument += delegate (object sender,
                                                                                   MonoDevelop.MacInterop.ApplicationDocumentEventArgs e) {
                    foreach(var pair in e.Documents) {
                        Application.OpenDocument(pair.Key);
                    }

                    e.Handled = true;
                };

                IgeMacMenu.GlobalKeyHandlerEnabled = true;
            }
        }

        Document _document;

        VBox _vbox;
        Gui _gui;

        MenuBar _menuBar;
        MenuItem _quitItem;
        MenuItem _aboutItem;
        MenuItem _preferencesItem;
        static MenuItem _staticQuitItem;
        static MenuItem _staticAboutItem;
        static MenuItem _staticPreferencesItem;

        MenuItem _newItem;
        MenuItem _openItem;
        MenuItem _openRecentItem;
        Menu _openRecentMenu;
        MenuItem _clearRecentItems;
        
        MenuItem _closeItem;
        MenuItem _saveItem;
        MenuItem _saveAsItem;
        MenuItem _exportItem;
        MenuItem _revertItem;

        MenuItem _undoItem;
        MenuItem _redoItem;
        MenuItem _cutItem;
        MenuItem _copyItem;
        MenuItem _pasteItem;
        MenuItem _selectAllItem;
        MenuItem _findItem;
        MenuItem _embedInMacro;

        MenuItem _showEditorItem;
        MenuItem _showDebuggerItem;
        MenuItem _manageHeadersItem;
        MenuItem _manageMacrosItem;
        MenuItem _documentSettingsItem;

        MenuItem _actualSizeItem;
        MenuItem _zoomInItem;
        MenuItem _zoomOutItem;

        MenuItem _showHelpItem;

        AccelGroup _accelGroup;
        static AccelGroup _staticAccelGroup;

        EditorGui _editorGui;
        DebugGui _debugGui;
    }
}

