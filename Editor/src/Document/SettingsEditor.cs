using System;
using Gtk;
using System.Text.RegularExpressions;

namespace Petri
{
	public class SettingsEditor
	{
		public SettingsEditor(Document doc) {
			_document = doc;

			_window = new Window(WindowType.Toplevel);
			_window.Title = "Réglages du document " + doc.Window.Title;

			_window.DefaultWidth = 300;
			_window.DefaultHeight = 400;

			_window.SetPosition(WindowPosition.Center);
			int x, y;
			_window.GetPosition(out x, out y);
			_window.Move(x, 2 * y / 3);
			_window.BorderWidth = 15;

			var vbox = new VBox(false, 5);
			_window.Add(vbox);

			{
				Label label = new Label("Nom C++ du réseau de pétri :");
				Entry entry = new Entry(_document.Settings.Name);
				entry.FocusOutEvent += (obj, eventInfo) => {
					Regex name = new Regex(Cpp.Parser.NamePattern);
					Match nameMatch = name.Match((obj as Entry).Text);

					if(!nameMatch.Success || nameMatch.Value != (obj as Entry).Text) {
						MessageDialog d = new MessageDialog(_window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "Le nom du réseau de Pétri n'est pas un identificateur C++ valide.");
						d.AddButton("Annuler", ResponseType.Cancel);
						d.Run();
						d.Destroy();

						(obj as Entry).Text = _document.Settings.Name;
					}
					else {
						_document.Settings.Name = (obj as Entry).Text;
						_document.Settings.Modified = true;
					}
				};

				var hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);

				label = new Label("Chemin vers le compilateur C++ :");
				entry = new Entry(_document.Settings.Compiler);
				entry.FocusOutEvent += (obj, eventInfo) => {
					_document.Settings.Compiler = (obj as Entry).Text;
					_document.Settings.Modified = true;
				};

				hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);

				label = new Label("Flags passés au compilateur C++ :");
				entry = new Entry(String.Join(" ", _document.Settings.CompilerFlags));
				entry.FocusOutEvent += (obj, eventInfo) => {
					_document.Settings.CompilerFlags.Clear();
					_document.Settings.CompilerFlags.AddRange((obj as Entry).Text.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries));
					_document.Settings.Modified = true;
				};

				hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);


				label = new Label("Chemin où créer la librairie dynamique :");
				entry = new Entry(_document.Settings.OutputPath);
				entry.FocusOutEvent += (obj, eventInfo) => {
					_document.Settings.OutputPath = (obj as Entry).Text;
					_document.Settings.Modified = true;
				};

				hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);

				label = new Label("Nom d'hôte du débuggeur :");
				entry = new Entry(_document.Settings.Hostname);
				entry.FocusOutEvent += (obj, eventInfo) => {
					_document.Settings.Hostname = (obj as Entry).Text;
					_document.Settings.Modified = true;
				};

				hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);

				label = new Label("Port de communication avec le débuggueur :");
				entry = new Entry(_document.Settings.Port.ToString());
				entry.FocusOutEvent += (obj, eventInfo) => {
					try {
						_document.Settings.Port = UInt16.Parse((obj as Entry).Text);
					}
					catch(Exception) {
						(obj as Entry).Text = _document.Settings.Port.ToString();
					}
					_document.Settings.Modified = true;
				};

				hbox = new HBox(false, 5);
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
				vbox.PackStart(entry, false, false, 0);
			}

			{
				var hbox = new HBox(false, 5);
				Label label = new Label("Chemins de recherche des headers :");
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);

				_headersSearchPath = new TreeView();
				TreeViewColumn c = new TreeViewColumn();
				c.Title = "Chemin";
				var pathCell = new Gtk.CellRendererText();
				pathCell.Editable = true;
				pathCell.Edited += (object o, EditedArgs args) => {
					var tup = _document.Settings.IncludePaths[int.Parse(args.Path)];
					_document.Settings.IncludePaths[int.Parse(args.Path)] =  Tuple.Create(args.NewText, tup.Item2);
					this.BuildHeadersSearchPath();
				};

				c.PackStart(pathCell, true);
				c.AddAttribute(pathCell, "text", 0);
				_headersSearchPath.AppendColumn(c);

				c = new TreeViewColumn();
				c.Title = "Récursif";
				var recursivityCell = new Gtk.CellRendererToggle();
				recursivityCell.Toggled += (object o, ToggledArgs args) => {
					var tup = _document.Settings.IncludePaths[int.Parse(args.Path)];
					_document.Settings.IncludePaths[int.Parse(args.Path)] =  Tuple.Create(tup.Item1, !tup.Item2);
					this.BuildHeadersSearchPath();
				};
				c.PackStart(recursivityCell, true);
				c.AddAttribute(recursivityCell, "active", 1);
				_headersSearchPath.AppendColumn(c);

				_headersSearchPathStore = new Gtk.ListStore(typeof(string), typeof(bool));
				_headersSearchPath.Model = _headersSearchPathStore;

				vbox.PackStart(_headersSearchPath, true, true, 0);

				hbox = new HBox(false, 5);
				_addHeaderSearchPath = new Button(new Label("+"));
				_removeHeaderSearchPath = new Button(new Label("-"));
				_addHeaderSearchPath.Clicked += OnAdd;
				_removeHeaderSearchPath.Clicked += OnRemove;
				hbox.PackStart(_addHeaderSearchPath, false, false, 0);
				hbox.PackStart(_removeHeaderSearchPath, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
			}

			{
				var hbox = new HBox(false, 5);
				Label label = new Label("Chemins de recherche des librairies :");
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);

				_libsSearchPath = new TreeView();
				TreeViewColumn c = new TreeViewColumn();
				c.Title = "Chemin";
				var pathCell = new Gtk.CellRendererText();
				pathCell.Editable = true;
				pathCell.Edited += (object o, EditedArgs args) => {
					var tup = _document.Settings.LibPaths[int.Parse(args.Path)];
					_document.Settings.LibPaths[int.Parse(args.Path)] =  Tuple.Create(args.NewText, tup.Item2);
					this.BuildLibsSearchPath();
				};
				c.PackStart(pathCell, true);
				c.AddAttribute(pathCell, "text", 0);
				_libsSearchPath.AppendColumn(c);

				c = new TreeViewColumn();
				c.Title = "Récursif";
				var recursivityCell = new Gtk.CellRendererToggle();
				recursivityCell.Toggled += (object o, ToggledArgs args) => {
					var tup = _document.Settings.LibPaths[int.Parse(args.Path)];
					_document.Settings.LibPaths[int.Parse(args.Path)] =  Tuple.Create(tup.Item1, !tup.Item2);
					this.BuildLibsSearchPath();
				};
				c.PackStart(recursivityCell, true);
				c.AddAttribute(recursivityCell, "active", 1);
				_libsSearchPath.AppendColumn(c);

				_libsSearchPathStore = new Gtk.ListStore(typeof(string), typeof(bool));
				_libsSearchPath.Model = _libsSearchPathStore;

				vbox.PackStart(_libsSearchPath, true, true, 0);

				hbox = new HBox(false, 5);
				_addLibSearchPath = new Button(new Label("+"));
				_removeLibSearchPath = new Button(new Label("-"));
				_addLibSearchPath.Clicked += OnAdd;
				_removeLibSearchPath.Clicked += OnRemove;
				hbox.PackStart(_addLibSearchPath, false, false, 0);
				hbox.PackStart(_removeLibSearchPath, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
			}

			{
				var hbox = new HBox(false, 5);
				Label label = new Label("Librairies utilisées par le document :");
				hbox.PackStart(label, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);

				_libs = new TreeView();
				TreeViewColumn c = new TreeViewColumn();
				c.Title = "Chemin";
				var pathCell = new Gtk.CellRendererText();
				pathCell.Editable = true;
				pathCell.Edited += (object o, EditedArgs args) => {
					_document.Settings.Libs[int.Parse(args.Path)] = args.NewText;
					this.BuildLibs();
				};
				c.PackStart(pathCell, true);
				c.AddAttribute(pathCell, "text", 0);
				_libs.AppendColumn(c);

				_libsStore = new Gtk.ListStore(typeof(string));
				_libs.Model = _libsStore;

				vbox.PackStart(_libs, true, true, 0);

				hbox = new HBox(false, 5);
				_addLib = new Button(new Label("+"));
				_removeLib = new Button(new Label("-"));
				_addLib.Clicked += OnAdd;
				_removeLib.Clicked += OnRemove;
				hbox.PackStart(_addLib, false, false, 0);
				hbox.PackStart(_removeLib, false, false, 0);
				vbox.PackStart(hbox, false, false, 0);
			}


			_window.DeleteEvent += this.OnDeleteEvent;

			this.BuildHeadersSearchPath();
			this.BuildLibsSearchPath();
			this.BuildLibs();
		}

		public void Show() {
			_window.ShowAll();
			_window.Present();
			_document.AssociatedWindows.Add(_window);
		}

		public void Hide() {
			_document.AssociatedWindows.Remove(_window);
			_window.Hide();
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
			_window.Hide();
			// We do not close the window so that there is no need to recreate it upon reponing
			a.RetVal = true;
		}

		private void OnAdd(object sender, EventArgs e) {
			string title = "";
			FileChooserAction action = FileChooserAction.Open;
			var filter = new FileFilter();

			if(sender == _addHeaderSearchPath) {
				title = "Sélectionnez le dossier où rechercher les headers…";
				action = FileChooserAction.SelectFolder;
			}
			else if(sender == _addLibSearchPath) {
				title = "Sélectionnez le dossier où rechercher les librairies…";
				action = FileChooserAction.SelectFolder;
			}
			else if(sender == _addLib) {
				title = "Sélectionnez la librairie…";
				action = FileChooserAction.Open;
				filter.AddPattern("*.a");
				filter.AddPattern("*.lib");
				filter.AddPattern("*.so");
				filter.AddPattern("*.dylib");
			}


			var fc = new Gtk.FileChooserDialog(title, _window,
				action,
				new object[]{"Annuler",ResponseType.Cancel,
					"Ouvrir",ResponseType.Accept});
			fc.AddFilter(filter);

			if(fc.Run() == (int)ResponseType.Accept) {
				if(sender == _addHeaderSearchPath) {
					_document.Settings.IncludePaths.Add(Tuple.Create(fc.Filename, false));
					this.BuildHeadersSearchPath();
				}
				else if(sender == _addLibSearchPath) {
					_document.Settings.LibPaths.Add(Tuple.Create(fc.Filename, false));
					this.BuildLibsSearchPath();
				}
				else if(sender == _addLib) {
					_document.Settings.Libs.Add(fc.Filename);
					this.BuildLibs();
				}
				_document.Settings.Modified = true;
			}
			fc.Destroy();
		}

		protected void OnRemove(object sender, EventArgs e) {
			if(sender == _removeHeaderSearchPath) {
				TreeIter iter;
				TreePath[] treePath = _headersSearchPath.Selection.GetSelectedRows();

				for(int i = treePath.Length; i > 0; i--) {
					_headersSearchPathStore.GetIter(out iter, treePath[(i - 1)]);
					_document.Settings.IncludePaths.Remove(Tuple.Create(_headersSearchPathStore.GetValue(iter, 0) as string, (bool)(_headersSearchPathStore.GetValue(iter, 1))));
					_document.Settings.Modified = true;
				}

				this.BuildHeadersSearchPath();
			}
			else if(sender == _removeLibSearchPath) {
				TreeIter iter;
				TreePath[] treePath = _libsSearchPath.Selection.GetSelectedRows();

				for(int i = treePath.Length; i > 0; i--) {
					_libsSearchPathStore.GetIter(out iter, treePath[(i - 1)]);
					_document.Settings.LibPaths.Remove(Tuple.Create(_libsSearchPathStore.GetValue(iter, 0) as string, (bool)(_libsSearchPathStore.GetValue(iter, 1))));
					_document.Settings.Modified = true;
				}

				this.BuildLibsSearchPath();
			}
			else if(sender == _removeLib) {
				TreeIter iter;
				TreePath[] treePath = _libs.Selection.GetSelectedRows();

				for(int i = treePath.Length; i > 0; i--) {
					_libsStore.GetIter(out iter, treePath[(i - 1)]);
					_document.Settings.Libs.Remove(_libsStore.GetValue(iter, 0) as string);
					_document.Settings.Modified = true;
				}

				this.BuildLibs();
			}
		}

		protected void BuildHeadersSearchPath() {
			_headersSearchPathStore.Clear();
			foreach(var p in _document.Settings.IncludePaths) {
				_headersSearchPathStore.AppendValues(p.Item1, p.Item2);
			}
			_window.ShowAll();
		}

		protected void BuildLibsSearchPath() {
			_libsSearchPathStore.Clear();
			foreach(var p in _document.Settings.LibPaths) {
				_libsSearchPathStore.AppendValues(p.Item1, p.Item2);
			}
		}

		protected void BuildLibs() {
			_libsStore.Clear();
			foreach(var p in _document.Settings.Libs) {
				_libsStore.AppendValues(p);
			}
		}

		Window _window;
		Document _document;

		TreeView _headersSearchPath;
		ListStore _headersSearchPathStore;
		Button _addHeaderSearchPath;
		Button _removeHeaderSearchPath;

		TreeView _libsSearchPath;
		ListStore _libsSearchPathStore;
		Button _addLibSearchPath;
		Button _removeLibSearchPath;

		TreeView _libs;
		ListStore _libsStore;
		Button _addLib;
		Button _removeLib;
	}
}

