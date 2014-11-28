using System;
using System.Collections.Generic;

namespace Petri
{
	public class DebugController : Controller
	{
		public DebugController(Document doc) {
			Document = doc;
			Server = new DebugServer(doc);
			ActiveStates = new Dictionary<State, int>();
		}

		public Document Document {
			get;
			private set;
		}

		public DebugServer Server {
			get;
			private set;
		}

		public Dictionary<State, int> ActiveStates {
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

