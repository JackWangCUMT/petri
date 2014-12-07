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
			Breakpoints = new HashSet<Action>();
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

		public HashSet<Action> Breakpoints {
			get;
			private set;
		}
			
		public void AddBreakpoint(Action a) {
			Breakpoints.Add(a);
			Server.UpdateBreakpoints();
		}

		public void RemoveBreakpoint(Action a) {
			Breakpoints.Remove(a);
			Server.UpdateBreakpoints();
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

