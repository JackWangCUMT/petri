using System;
using Gtk;
using IgeMacIntegration;

namespace Petri
{
	public class MainWindow : Gtk.Window
	{
		public MainWindow(Document doc) : base(Gtk.WindowType.Toplevel) {
			this.document = doc;

			this.Name = "IA_Robot.MainWindow";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.AllowShrink = true;
			this.DefaultWidth = 800;
			this.DefaultHeight = 600;
			this.DeleteEvent += this.OnDeleteEvent;

			this.BorderWidth = 15;
			this.vbox = new VBox(false, 5);
			this.Add(vbox);

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

			editorGui = new EditorGui(document);
			debugGui = new DebugGui(document);

			this.FocusInEvent += (o, args) => {
				document.UpdateMenuItems();
				gui.FocusIn();
				if(Configuration.RunningPlatform == Platform.Mac) {
					menuBar.ShowAll();
					menuBar.Hide();
					IgeMacMenu.MenuBar = menuBar;
				}
			};
			this.FocusOutEvent += (o, args) => {
				gui.FocusOut();
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

				IgeMacMenu.QuitMenuItem = quitItem;

				var appGroup = IgeMacMenu.AddAppMenuGroup();
				appGroup.AddMenuItem(aboutItem, "À propos de Petri…");
				appGroup.AddMenuItem(preferencesItem, "Préférences…");

				vbox.Show();
				this.Show();
			}
			else {
				this.ShowAll();
			}
		}

		public Gui Gui {
			get {
				return gui;
			}
			set {
				if(gui != null) {
					gui.Hide();
					vbox.Remove(gui);
				}

				gui = value;

				vbox.PackEnd(gui);

				gui.Redraw();
				gui.FocusIn();
				gui.UpdateToolbar();

				gui.ShowAll();
			}
		}

		public EditorGui EditorGui {
			get {
				return editorGui;
			}
		}

		public DebugGui DebugGui {
			get {
				return debugGui;
			}
		}

		public MenuItem UndoItem {
			get {
				return undoItem;
			}
		}

		public MenuItem RedoItem {
			get {
				return redoItem;
			}
		}

		public MenuItem CutItem {
			get {
				return cutItem;
			}
		}
		public MenuItem CopyItem {
			get {
				return copyItem;
			}
		}
		public MenuItem PasteItem {
			get {
				return pasteItem;
			}
		}

		public MenuItem RevertItem {
			get {
				return revertItem;
			}
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			// TODO: close debugger session
			bool result = this.document.CloseAndConfirm();
			a.RetVal = !result;
		}

		protected void OnClickMenu(object sender, EventArgs e) {
			if(sender == quitItem) {
				bool shouldExit = MainClass.OnExit();
				if(shouldExit) {
					MainClass.SaveAndQuit();
				}
			}
			else if(sender == saveItem) {
				document.Save();
			}
			else if(sender == saveAsItem) {
				document.SaveAs();
			}
			else if(sender == exportItem) {
				document.ExportAsPDF();
			}
			else if(sender == revertItem) {
				document.Restore();
			}
			else if(sender == undoItem) {
				document.Undo();
			}
			else if(sender == redoItem) {
				document.Redo();
			}
			else if(sender == copyItem) {
				document.CurrentController.Copy();
			}
			else if(sender == cutItem) {
				document.CurrentController.Cut();
			}
			else if(sender == pasteItem) {
				document.CurrentController.Paste();
			}
			else if(sender == selectAllItem) {
				document.CurrentController.SelectAll();
			}
			else if(sender == openItem) {
				MainClass.OpenDocument();
			}
			else if(sender == newItem) {
				var doc = new Document("");
				MainClass.AddDocument(doc);
			}
			else if(sender == closeItem) {
				if(this.document.CloseAndConfirm())
					this.Destroy();
			}
			else if(sender == documentSettings) {
				document.EditSettings();
			}
		}

		protected void BuildMenus() {
			accelGroup = new AccelGroup();
			this.AddAccelGroup(accelGroup);

			menuBar = new MenuBar();

			Menu fileMenu = new Menu();
			Menu editMenu = new Menu();
			Menu documentMenu = new Menu();
			Menu helpMenu = new Menu();
			MenuItem file = new MenuItem("Fichier");
			MenuItem edit = new MenuItem("Édition");
			MenuItem document = new MenuItem("Document");
			MenuItem help = new MenuItem("Aide");
			file.Submenu = fileMenu;
			edit.Submenu = editMenu;
			document.Submenu = documentMenu;
			help.Submenu = helpMenu;

			quitItem = new MenuItem("Quitter");
			quitItem.Activated += OnClickMenu;
			quitItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			newItem = new MenuItem("Nouveau");
			newItem.Activated += OnClickMenu;
			newItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.n, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			openItem = new MenuItem("Ouvrir…");
			openItem.Activated += OnClickMenu;
			openItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.o, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			closeItem = new MenuItem("Fermer");
			closeItem.Activated += OnClickMenu;
			closeItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.w, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			saveItem = new MenuItem("Enregistrer");
			saveItem.Activated += OnClickMenu;
			saveItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.s, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			saveAsItem = new MenuItem("Enregistrer sous…");
			saveAsItem.Activated += OnClickMenu;
			saveAsItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.s, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));

			exportItem = new MenuItem("Exporter en PDF…");
			exportItem.Activated += OnClickMenu;

			revertItem = new MenuItem("Revenir…");
			revertItem.Activated += OnClickMenu;
			revertItem.Sensitive = false;

			fileMenu.Append(newItem);
			fileMenu.Append(openItem);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(closeItem);
			fileMenu.Append(saveItem);
			fileMenu.Append(saveAsItem);
			fileMenu.Append(exportItem);
			fileMenu.Append(revertItem);

			undoItem = new MenuItem("Annuler");
			undoItem.Activated += OnClickMenu;
			undoItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.z, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			redoItem = new MenuItem("Rétablir");
			redoItem.Activated += OnClickMenu;
			redoItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));

			cutItem = new MenuItem("Couper");
			cutItem.Activated += OnClickMenu;
			cutItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.x, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			copyItem = new MenuItem("Copier");
			copyItem.Activated += OnClickMenu;
			copyItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.c, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			selectAllItem = new MenuItem("Tout sélectionner");
			selectAllItem.Activated += OnClickMenu;
			selectAllItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.a, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			pasteItem = new MenuItem("Coller");
			pasteItem.Activated += OnClickMenu;
			pasteItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.v, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			preferencesItem = new MenuItem("Préférences…");
			preferencesItem.Activated += OnClickMenu;
			preferencesItem.AddAccelerator("activate", accelGroup, new AccelKey(Gdk.Key.comma, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

			editMenu.Append(undoItem);
			editMenu.Append(redoItem);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(cutItem);
			editMenu.Append(copyItem);
			editMenu.Append(pasteItem);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(selectAllItem);

			undoItem.Sensitive = false;
			redoItem.Sensitive = false;
			cutItem.Sensitive = false;
			copyItem.Sensitive = false;
			pasteItem.Sensitive = false;

			documentSettings = new MenuItem("Options…");
			documentSettings.Activated += OnClickMenu;
			documentMenu.Append(documentSettings);


			showHelpItem = new MenuItem("Aide…");
			showHelpItem.Activated += OnClickMenu;
			aboutItem = new MenuItem("À propos…");
			aboutItem.Activated += OnClickMenu;

			helpMenu.Append(showHelpItem);
			helpMenu.Append(aboutItem);

			if(Configuration.RunningPlatform != Platform.Mac) {
				fileMenu.Append(quitItem);
				editMenu.Append(new SeparatorMenuItem());
				editMenu.Append(preferencesItem);
				helpMenu.Append(aboutItem);
			}

			menuBar.Append(file);
			menuBar.Append(edit);
			menuBar.Append(document);
			menuBar.Append(help);

			vbox.PackStart(menuBar);
		}

		Document document;

		VBox vbox;
		Gui gui;

		MenuBar menuBar;
		MenuItem quitItem;
		MenuItem aboutItem;
		MenuItem preferencesItem;

		MenuItem newItem;
		MenuItem openItem;
		MenuItem closeItem;
		MenuItem saveItem;
		MenuItem saveAsItem;
		MenuItem exportItem;
		MenuItem revertItem;

		MenuItem undoItem;
		MenuItem redoItem;
		MenuItem cutItem;
		MenuItem copyItem;
		MenuItem pasteItem;
		MenuItem selectAllItem;

		MenuItem showHelpItem;

		MenuItem documentSettings;

		AccelGroup accelGroup;

		EditorGui editorGui;
		DebugGui debugGui;
	}
}

