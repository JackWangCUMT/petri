using System;
using Gtk;
using IgeMacIntegration;

namespace Petri
{
	public class MainWindow : Gtk.Window
	{
		public MainWindow(Document doc) : base(Gtk.WindowType.Toplevel) {
			_document = doc;

			this.Name = "IA_Robot.MainWindow";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.AllowShrink = true;
			this.DefaultWidth = 800;
			this.DefaultHeight = 600;
			this.DeleteEvent += this.OnDeleteEvent;

			this.BorderWidth = 15;
			_vbox = new VBox(false, 5);
			this.Add(_vbox);

			if(MainClass.Documents.Count > 0) {
				int x, y;
				MainClass.Documents[MainClass.Documents.Count - 1].Window.GetPosition(out x, out y);
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
					_menuBar.Hide();
					IgeMacMenu.MenuBar = _menuBar;
				}
			};
			this.FocusOutEvent += (o, args) => {
				_gui.FocusOut();
			};
		}

		public void PresentWindow() {
			if(Configuration.RunningPlatform == Platform.Mac) {
				MonoDevelop.MacInterop.ApplicationEvents.Quit += delegate (object sender, MonoDevelop.MacInterop.ApplicationQuitEventArgs e) {
					MainClass.SaveAndQuit();
					// If we get here, the user has cancelled the action
					e.UserCancelled = true;
					e.Handled = true;
				};

				IgeMacMenu.GlobalKeyHandlerEnabled = true;

				IgeMacMenu.QuitMenuItem = _quitItem;

				var appGroup = IgeMacMenu.AddAppMenuGroup();
				appGroup.AddMenuItem(_aboutItem, "À propos de Petri…");
				appGroup.AddMenuItem(_preferencesItem, "Préférences…");

				_vbox.Show();
				this.Show();
			}
			else {
				this.ShowAll();
			}
		}

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

		public EditorGui EditorGui {
			get {
				return _editorGui;
			}
		}

		public DebugGui DebugGui {
			get {
				return _debugGui;
			}
		}

		public MenuItem UndoItem {
			get {
				return _undoItem;
			}
		}

		public MenuItem RedoItem {
			get {
				return _redoItem;
			}
		}

		public MenuItem CutItem {
			get {
				return _cutItem;
			}
		}
		public MenuItem CopyItem {
			get {
				return _copyItem;
			}
		}
		public MenuItem PasteItem {
			get {
				return _pasteItem;
			}
		}
		public MenuItem EmbedItem {
			get {
				return _embedInMacro;
			}
		}

		public MenuItem RevertItem {
			get {
				return _revertItem;
			}
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			bool result = _document.CloseAndConfirm();
			a.RetVal = !result;
		}

		protected void OnClickMenu(object sender, EventArgs e) {
			if(sender == _quitItem) {
				bool shouldExit = MainClass.OnExit();
				if(shouldExit) {
					MainClass.SaveAndQuit();
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
				MainClass.OpenDocument();
			}
			else if(sender == _newItem) {
				var doc = new Document("");
				MainClass.AddDocument(doc);
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
		}

		protected void BuildMenus() {
			_accelGroup = new AccelGroup();
			this.AddAccelGroup(_accelGroup);

			_menuBar = new MenuBar();

			Menu fileMenu = new Menu();
			Menu editMenu = new Menu();
			Menu viewMenu = new Menu();
			Menu documentMenu = new Menu();
			Menu helpMenu = new Menu();
			MenuItem file = new MenuItem("Fichier");
			MenuItem edit = new MenuItem("Édition");
			MenuItem view = new MenuItem("Affichage");
			MenuItem document = new MenuItem("Document");
			MenuItem help = new MenuItem("Aide");
			file.Submenu = fileMenu;
			edit.Submenu = editMenu;
			view.Submenu = viewMenu;
			document.Submenu = documentMenu;
			help.Submenu = helpMenu;

			_quitItem = new MenuItem("Quitter");
			_quitItem.Activated += OnClickMenu;
			_quitItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_newItem = new MenuItem("Nouveau");
			_newItem.Activated += OnClickMenu;
			_newItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.n, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_openItem = new MenuItem("Ouvrir…");
			_openItem.Activated += OnClickMenu;
			_openItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.o, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_closeItem = new MenuItem("Fermer");
			_closeItem.Activated += OnClickMenu;
			_closeItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.w, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_saveItem = new MenuItem("Enregistrer");
			_saveItem.Activated += OnClickMenu;
			_saveItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.s, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_saveAsItem = new MenuItem("Enregistrer sous…");
			_saveAsItem.Activated += OnClickMenu;
			_saveAsItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.s, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));

			_exportItem = new MenuItem("Exporter en PDF…");
			_exportItem.Activated += OnClickMenu;
			_exportItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.e, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_revertItem = new MenuItem("Revenir…");
			_revertItem.Activated += OnClickMenu;
			_revertItem.Sensitive = false;

			fileMenu.Append(_newItem);
			fileMenu.Append(_openItem);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(_closeItem);
			fileMenu.Append(_saveItem);
			fileMenu.Append(_saveAsItem);
			fileMenu.Append(_exportItem);
			fileMenu.Append(_revertItem);

			_undoItem = new MenuItem("Annuler");
			_undoItem.Activated += OnClickMenu;
			_undoItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.z, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_redoItem = new MenuItem("Rétablir");
			_redoItem.Activated += OnClickMenu;
			_redoItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));

			_cutItem = new MenuItem("Couper");
			_cutItem.Activated += OnClickMenu;
			_cutItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.x, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_copyItem = new MenuItem("Copier");
			_copyItem.Activated += OnClickMenu;
			_copyItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.c, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_selectAllItem = new MenuItem("Tout sélectionner");
			_selectAllItem.Activated += OnClickMenu;
			_selectAllItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.a, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_pasteItem = new MenuItem("Coller");
			_pasteItem.Activated += OnClickMenu;
			_pasteItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.v, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			_embedInMacro = new MenuItem("Regrouper dans une macro");
			_embedInMacro.Activated += OnClickMenu;
			_embedInMacro.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.e, Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));

			_preferencesItem = new MenuItem("Préférences…");
			_preferencesItem.Activated += OnClickMenu;
			_preferencesItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.comma, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			editMenu.Append(_undoItem);
			editMenu.Append(_redoItem);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(_cutItem);
			editMenu.Append(_copyItem);
			editMenu.Append(_pasteItem);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(_selectAllItem);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(_embedInMacro);

			_undoItem.Sensitive = false;
			_redoItem.Sensitive = false;
			_cutItem.Sensitive = false;
			_copyItem.Sensitive = false;
			_pasteItem.Sensitive = false;

			_actualSizeItem = new MenuItem("Taille réelle");
			_actualSizeItem.Activated += OnClickMenu;
			_actualSizeItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.Key_0, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
			_zoomInItem = new MenuItem("Zoom avant");
			_zoomInItem.Activated += OnClickMenu;
			_zoomInItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.plus, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
			_zoomOutItem = new MenuItem("Zoom arrière");
			_zoomOutItem.Activated += OnClickMenu;
			_zoomOutItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.minus, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			viewMenu.Append(_actualSizeItem);
			viewMenu.Append(_zoomInItem);
			viewMenu.Append(_zoomOutItem);

			_showEditorItem = new MenuItem("Afficher l'éditeur");
			_showEditorItem.Activated += OnClickMenu;
			_showEditorItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.e, Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
			_showDebuggerItem = new MenuItem("Afficher le débuggueur");
			_showDebuggerItem.Activated += OnClickMenu;
			_showDebuggerItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.d, Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
			_manageHeadersItem = new MenuItem("Gérer les headers…");
			_manageHeadersItem.Activated += OnClickMenu;
			_manageMacrosItem = new MenuItem("Gérer les macros…");
			_manageMacrosItem.Activated += OnClickMenu;
			_documentSettingsItem = new MenuItem("Réglages…");
			_documentSettingsItem.Activated += OnClickMenu;
			_documentSettingsItem.AddAccelerator("activate", _accelGroup, new AccelKey(Gdk.Key.comma, Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));

			documentMenu.Append(_showEditorItem);
			documentMenu.Append(_showDebuggerItem);
			documentMenu.Append(new SeparatorMenuItem());
			documentMenu.Append(_manageHeadersItem);
			documentMenu.Append(_manageMacrosItem);
			documentMenu.Append(_documentSettingsItem);


			_showHelpItem = new MenuItem("Aide…");
			_showHelpItem.Activated += OnClickMenu;
			_aboutItem = new MenuItem("À propos…");
			_aboutItem.Activated += OnClickMenu;

			helpMenu.Append(_showHelpItem);
			helpMenu.Append(_aboutItem);

			if(Configuration.RunningPlatform != Platform.Mac) {
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

			_vbox.PackStart(_menuBar, false, false, 0);
		}

		Document _document;

		VBox _vbox;
		Gui _gui;

		MenuBar _menuBar;
		MenuItem _quitItem;
		MenuItem _aboutItem;
		MenuItem _preferencesItem;

		MenuItem _newItem;
		MenuItem _openItem;
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

		EditorGui _editorGui;
		DebugGui _debugGui;
	}
}

