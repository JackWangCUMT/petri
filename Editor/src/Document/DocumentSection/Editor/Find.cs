using System;
using Gtk;

namespace Petri
{
	public class Find
	{
		public enum FindType {
			All,
			Action,
			Transition,
			Comment,
		}

		public void Show() {
			_window.Title = "Rechercher dans le document " + _document.Window.Title;
			_window.ShowAll();
			_window.Present();
			_document.AssociatedWindows.Add(_window);
			_what.GrabFocus();
		}

		public void Hide() {
			_document.AssociatedWindows.Remove(_window);
			_window.Hide();
		}

		protected void OnFind(object sender, EventArgs e) {
			_document.Window.EditorGui.PerformFind(_what.Text, Type);
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
			_window.Hide();
			// We do not close the window so that there is no need to recreate it upon reopening
			a.RetVal = true;
		}

		public FindType Type {
			get;
			protected set;
		}

		public Find(Document doc) {
			_document = doc;

			_window = new Window(WindowType.Toplevel);
			_window.Title = "Rechercher dans le document";

			_window.DefaultWidth = 400;
			_window.DefaultHeight = 100;

			_window.SetPosition(WindowPosition.Center);
			int x, y;
			_window.GetPosition(out x, out y);
			_window.Move(x, 2 * y / 3);
			_window.BorderWidth = 15;

			var vbox = new VBox(false, 5);

			_window.Add(vbox);

			var hbox = new HBox();
			var label = new Label("Rechercher parmi les entités de type :");
			hbox.PackStart(label, false, false, 0);
			vbox.PackStart(hbox, false, false, 0);

			ComboBox combo = ComboBox.NewText();

			combo.AppendText("Toutes les entités");
			combo.AppendText("Action");
			combo.AppendText("Transition");
			combo.AppendText("Commentaire");
			TreeIter iter;
			combo.Model.GetIterFirst(out iter);
			combo.SetActiveIter(iter);

			combo.Changed += (object sender, EventArgs e) => {
				TreeIter it;

				if(combo.GetActiveIter(out it)) {
					Type = (FindType)int.Parse(combo.Model.GetStringFromIter(it));
				}
			};

			hbox = new HBox();
			hbox.PackStart(combo, false, false, 0);
			vbox.PackStart(hbox, false, false, 0);

			_what = new Entry();
			_what.Activated += OnFind;

			vbox.PackStart(_what, true, true, 0);

			hbox = new HBox(false, 5);
			var cancel = new Button(new Label("Annuler"));
			var find = new Button(new Label("Rechercher"));
			cancel.Clicked += (sender, e) => {
				Hide();
			};
			find.Clicked += OnFind;

			hbox.PackStart(cancel, false, false, 0);
			hbox.PackStart(find, false, false, 0);
			vbox.PackStart(hbox, false, false, 0);

			_window.DeleteEvent += OnDeleteEvent;
		}

		Entry _what;
		Document _document;
		Window _window;
	}
}

