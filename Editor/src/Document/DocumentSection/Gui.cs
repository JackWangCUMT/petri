using System;
using Gtk;

namespace Petri
{
	public abstract class Gui : VBox
	{
		public Gui() {
			_status = new Label();
			var hbox = new HBox(false, 0);
			hbox.PackStart(_status, false, true, 5);
			PackEnd(hbox, false, true, 5);
		}

		public abstract void FocusIn();
		public abstract void FocusOut();

		public abstract void Redraw();

		public abstract void UpdateToolbar();

		public abstract PetriView BaseView {
			get;
		}

		public abstract Fixed Editor {
			get;
		}

		public HPaned Paned {
			get {
				return _paned;
			}
		}

		public string Status {
			get {
				return _status.Text;
			}
			set {
				_status.Text = value;
			}
		}

		protected HPaned _paned;
		protected Label _status;
	}
}

