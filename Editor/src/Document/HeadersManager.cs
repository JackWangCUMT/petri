using System;
using Gtk;

namespace Petri
{
	public class HeadersManager
	{
		public HeadersManager(Document doc) {
			_document = doc;
			_window = new Window(WindowType.Toplevel);
			_window.Title = "Headers associés à " + doc.Window.Title;

			_window.DefaultWidth = 300;
			_window.DefaultHeight = 300;

			_window.SetPosition(WindowPosition.Center);
			int x, y;
			_window.GetPosition(out x, out y);
			_window.Move(x, 2 * y / 3);
			_window.BorderWidth = 15;

			var vbox = new VBox(false, 5);

			_window.Add(vbox);

			_table = new TreeView();
			TreeViewColumn c = new TreeViewColumn();
			c.Title = "Fichier";
			var fileCell = new Gtk.CellRendererText();
			c.PackStart(fileCell, true);
			c.AddAttribute(fileCell, "text", 0);

			_table.AppendColumn(c);
			_headersStore = new Gtk.ListStore(typeof(string));
			_table.Model = _headersStore;

			vbox.PackStart(_table, true, true, 0);

			var hbox = new HBox(false, 5);
			var plus = new Button(new Label("+"));
			var minus = new Button(new Label("-"));
			plus.Clicked += OnAdd;
			minus.Clicked += OnRemove;
			hbox.PackStart(plus, false, false, 0);
			hbox.PackStart(minus, false, false, 0);
			vbox.PackStart(hbox, false, false, 0);

			var OK = new Button(new Label("OK"));
			hbox.PackEnd(OK, false, false, 0);
			OK.Clicked += (sender, e) => _window.Hide();

			_window.DeleteEvent += OnDeleteEvent;
		}

		public void Show() {
			this.BuildList();
			_window.ShowAll();
			_window.Present();
			_document.AssociatedWindows.Add(_window);
		}

		public void Hide() {
			_document.AssociatedWindows.Remove(_window);
			_window.Hide();
		}

		private void BuildList() {
			_headersStore.Clear();
			foreach(string h in _document.Headers) {
				_headersStore.AppendValues(h);
			}

			_window.ShowAll();
		}

		protected void OnRemove(object sender, EventArgs e) {
			TreeIter iter;
			TreePath[] treePath = _table.Selection.GetSelectedRows();

			for (int i  = treePath.Length; i > 0; i--) {
				_headersStore.GetIter(out iter, treePath[(i - 1)]);
				if(!_document.RemoveHeader(_headersStore.GetValue(iter, 0) as string)) {
					MessageDialog d = new MessageDialog(_window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, "Le header est utilisé dans le document. Il n'a pas été supprimé.");
					d.AddButton("Annuler", ResponseType.Cancel);
					d.Run();
					d.Destroy();
					break;
				}
			}

			this.BuildList();
		}


		protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
			_window.Hide();
			// We do not close the window so that there is no need to recreate it upon reopening
			a.RetVal = true;
		}

		private void OnAdd(object sender, EventArgs e) {
			var fc = new Gtk.FileChooserDialog("Choisissez le fichier contenant les déclarations C++…", _window,
				FileChooserAction.Open,
				new object[]{"Annuler",ResponseType.Cancel,
					"Ouvrir",ResponseType.Accept});

			CheckButton b = new CheckButton("Chemin relatif");
			b.Active = true;
			fc.ActionArea.PackEnd(b);
			b.Show();

			if(fc.Run() == (int)ResponseType.Accept) {
				string filename = fc.Filename;
				if(b.Active) {
					filename = _document.GetRelativeToDoc(filename);
				}
				_document.AddHeader(filename);
			}
			fc.Destroy();

			this.BuildList();
		}

		Document _document;
		Window _window;
		TreeView _table;
		ListStore _headersStore;
	}
}

