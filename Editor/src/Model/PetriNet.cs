using System;
using System.Collections.Generic;
using System.Xml;
using System.Collections;
using System.Xml.Linq;

namespace Petri
{
	public abstract class PetriNet : NonRootState
	{
		public PetriNet(Document doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos) {
			states = new List<State>();
			transitions = new List<Transition>();

			this.Radius = 30;
		}

		public PetriNet(Document doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			this.states = new List<State>();
			this.transitions = new List<Transition>();

			// Used to map XML's IDs of Transitions to actual States, after loading them.
			var statesTable = new Dictionary<UInt64, State>();

			this.Name = descriptor.Attribute("Name").Value;

			foreach(var e in descriptor.Element("States").Elements()) {
				var s = Entity.EntityFromXml(Document, e, this, null) as State;
				statesTable.Add(s.ID, s);
			}

			foreach(var e in descriptor.Element("Transitions").Elements("Transition")) {
				var t = new Transition(doc, this, e, statesTable, Document.AllFunctions);
				this.AddTransition(t);
				t.Before.AddTransitionAfter(t);
				t.After.AddTransitionBefore(t);
			}

			foreach(var s in statesTable.Values) {
				this.AddState(s);
			}
		}

		public override void Serialize(XElement elem) {
			base.Serialize(elem);
			var states = new XElement("States");
			foreach(var s in this.States) {
				states.Add(s.GetXml());
			}
			var transitions = new XElement("Transitions");
			foreach(var t in this.Transitions) {
				transitions.Add(t.GetXml());
			}

			elem.Add(states);
			elem.Add(transitions);
		}

		public override XElement GetXml() {
			var elem = new XElement("PetriNet");
			this.Serialize(elem);
			return elem;
		}

		public override bool UsesHeader(string h) {
			foreach(var t in transitions)
				if(t.UsesHeader(h))
					return true;
			foreach(var s in states)
				if(s.UsesHeader(h))
					return true;

			return false;
		}

		public void AddState(State a)
		{
			states.Add(a);
		}

		public void AddTransition(Transition t)
		{
			transitions.Add(t);
		}

		// TODO: come back with a better collision algorithm :p
		public State StateAtPosition(Cairo.PointD position)
		{
			for(int i = states.Count - 1; i >= 0; --i) {
				var s = states[i];
				if(s.PointInState(position)) {
					return s;
				}
			}

			return null;
		}
		
		public Transition TransitionAtPosition(Cairo.PointD position)
		{
			for(int i = transitions.Count - 1; i >= 0; --i) {
				var t = transitions[i];
				if(Math.Abs(t.Position.X - position.X) <= t.Width / 2 && Math.Abs(t.Position.Y - position.Y) < t.Height / 2) {
					return t;
				}
			}

			return null;
		}

		public void RemoveState(State a)
		{
			states.Remove(a);
		}

		public void RemoveTransition(Transition t)
		{
			t.Before.RemoveTransitionAfter(t);
			t.After.RemoveTransitionBefore(t);

			transitions.Remove(t);
		}

		public List<State> States {
			get {
				return states;
			}
		}

		public List<Transition> Transitions {
			get {
				return transitions;
			}
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			foreach(State s in this.States) {
				s.GenerateCpp(source, lastID);
			}
			source += "\n";

			foreach(Transition t in this.Transitions) {
				t.GenerateCpp(source, lastID);
			}

			source += "\n";
		
			return "";
		}

		public Entity EntityFromID(UInt64 id) {
			foreach(var s in States) {
				if(s.ID == id)
					return s;
				if(s is PetriNet) {
					Entity e = (s as PetriNet).EntityFromID(id);
					if(e != null)
						return e;
				}
				if(s is InnerPetriNet && (s as InnerPetriNet).EntryPointID == id) {
					return s;
				}
			}
			foreach(var t in Transitions) {
				if(t.ID == id)
					return t;
			}

			return null;
		}

		public bool ContainsEntity(UInt64 id) {
			foreach(var s in States) {
				if(s.ID == id)
					return true;
				if(s is PetriNet) {
					Entity e = (s as PetriNet).EntityFromID(id);
					if(e != null)
						return true;
				}
				if(s is InnerPetriNet && (s as InnerPetriNet).EntryPointID == id) {
					return true;
				}
			}
			foreach(var t in Transitions) {
				if(t.ID == id)
					return true;
			}

			return false;
		}


		// Recursively gets all of the Action/PetriNet
		protected List<Entity> BuildActionsList() {
			var l = new List<Entity>();
			l.AddRange(this.States);

			for(int i = 0; i < this.States.Count; ++i) {
				var s = l[i] as PetriNet;
				if(s != null) {
					l.AddRange(s.BuildActionsList());
				}
			}

			return l;
		}

		private List<State> states;
		private List<Transition> transitions;
	}
}

