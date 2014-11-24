using System;

namespace Petri
{
	public class DebugController : Controller
	{
		public DebugController(Document doc) {
			Document = doc;
			Server = new DebugServer(doc);
		}

		public Document Document {
			get;
			private set;
		}

		public DebugServer Server {
			get;
			private set;
		}

		public override void ManageFocus(object focus) {

		}

		public override void UpdateMenuItems() {

		}

		public override void Copy() {

		}

		public override void Cut() {

		}

		public override void Paste() {

		}

		public override void SelectAll() {

		}
	}
}

