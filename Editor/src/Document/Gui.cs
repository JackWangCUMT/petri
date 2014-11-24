using System;
using Gtk;

namespace Petri
{
	public abstract class Gui : VBox
	{
		public Gui() {}

		public abstract void FocusIn();
		public abstract void FocusOut();

		public abstract void Redraw();

		public abstract void UpdateToolbar();
	}
}

