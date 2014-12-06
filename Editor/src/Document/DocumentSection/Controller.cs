using System;

namespace Petri
{
	public abstract class Controller
	{
		public abstract void ManageFocus(object focus);
		public abstract void UpdateMenuItems();
		public abstract void Copy();
		public abstract void Cut();
		public abstract void Paste();
		public abstract void SelectAll();
	}
}

