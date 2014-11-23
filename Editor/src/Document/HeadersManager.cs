using System;
using Gtk;

namespace Petri
{
	public class HeadersManager
	{
		public HeadersManager(Document doc) {
			document = doc;
			window = new Window(WindowType.Toplevel);
			window.Title = "Headers associés à " + doc.Window.Title;

			window.DefaultWidth = 300;
			window.DefaultHeight = 300;

			window.SetPosition(WindowPosition.Center);
			int x, y;
			window.GetPosition(out x, out y);
			window.Move(x, 2 * y / 3);
			window.BorderWidth = 15;

			var vbox = new VBox(false, 5);

			window.Add(vbox);

			table = new TreeView();
			TreeViewColumn c = new TreeViewColumn();
			c.Title = "Fichier";
			var fileCell = new Gtk.CellRendererText();
			c.PackStart(fileCell, true);
			c.AddAttribute(fileCell, "text", 0);

			table.AppendColumn(c);
			headersStore = new Gtk.ListStore(typeof(string));
			table.Model = headersStore;

			vbox.PackStart(table, true, true, 0);

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
			OK.Clicked += (sender, e) => window.Hide();

			window.DeleteEvent += OnDeleteEvent;
		}

		public void Show() {
			this.BuildList();
			window.ShowAll();
			window.Present();
		}

		public void Hide() {
			window.Hide();
		}

		private void BuildList() {
			headersStore.Clear();
			foreach(string h in document.Headers) {
				headersStore.AppendValues(h);
			}

			window.ShowAll();
		}

		protected void OnRemove(object sender, EventArgs e) {
			TreeIter iter;
			TreePath[] treePath = table.Selection.GetSelectedRows();

			for (int i  = treePath.Length; i > 0; i--) {
				headersStore.GetIter(out iter, treePath[(i - 1)]);
				if(!document.RemoveHeader(headersStore.GetValue(iter, 0) as string)) {
					MessageDialog d = new MessageDialog(window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, "Le header est utilisé dans le document. Il n'a pas été supprimé.");
					d.AddButton("Annuler", ResponseType.Cancel);
					d.Run();
					d.Destroy();
					break;
				}
			}

			this.BuildList();
		}


		protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
			window.Hide();
			// We do not close the window so that there is no need to recreate it upon reponing
			a.RetVal = true;
		}

		private void OnAdd(object sender, EventArgs e) {
			var fc = new Gtk.FileChooserDialog("Choisissez le fichier contenant les déclarations C++…", window,
				FileChooserAction.Open,
				new object[]{"Annuler",ResponseType.Cancel,
					"Ouvrir",ResponseType.Accept});

			if(fc.Run() == (int)ResponseType.Accept) {
				document.AddHeader(fc.Filename);
			}
			fc.Destroy();

			this.BuildList();
		}

		Document document;
		Window window;
		TreeView table;
		ListStore headersStore;
	}
}

