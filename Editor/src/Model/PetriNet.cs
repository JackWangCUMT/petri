using System;
using System.Collections.Generic;
using System.Xml;
using System.Collections;
using System.Xml.Linq;

namespace Petri
{
	public abstract class PetriNet : NonRootState
	{
		public PetriNet(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos) {
			Comments = new List<Comment>();
			States = new List<State>();
			Transitions = new List<Transition>();

			this.Radius = 30;
			this.Size = new Cairo.PointD(0, 0);
		}

		public PetriNet(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			Comments = new List<Comment>();
			States = new List<State>();
			Transitions = new List<Transition>();

			// Used to map XML's IDs of Transitions to actual States, after loading them.
			var statesTable = new Dictionary<UInt64, State>();

			this.Name = descriptor.Attribute("Name").Value;

			foreach(var e in descriptor.Element("Comments").Elements()) {
				var c = Entity.EntityFromXml(Document, e, this, null) as Comment;
				Comments.Add(c);
			}

			foreach(var e in descriptor.Element("States").Elements()) {
				var s = Entity.EntityFromXml(Document, e, this, null) as State;
				statesTable.Add(s.ID, s);
			}

			foreach(var e in descriptor.Element("Transitions").Elements("Transition")) {
				var t = new Transition(doc, this, e, statesTable, Document.AllFunctions, Document.CppMacros);
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
			var comments = new XElement("Comments");
			foreach(var c in this.Comments) {
				comments.Add(c.GetXml());
			}
			var states = new XElement("States");
			foreach(var s in this.States) {
				states.Add(s.GetXml());
			}
			var transitions = new XElement("Transitions");
			foreach(var t in this.Transitions) {
				transitions.Add(t.GetXml());
			}

			elem.Add(comments);
			elem.Add(states);
			elem.Add(transitions);
		}

		public override XElement GetXml() {
			var elem = new XElement("PetriNet");
			this.Serialize(elem);
			return elem;
		}

		public override bool UsesFunction(Cpp.Function f) {
			foreach(var t in Transitions)
				if(t.UsesFunction(f))
					return true;
			foreach(var s in States)
				if(s.UsesFunction(f))
					return true;

			return false;
		}

		public void AddComment(Comment c) {
			Comments.Add(c);
		}

		public void AddState(State a) {
			States.Add(a);
		}

		public void AddTransition(Transition t) {
			Transitions.Add(t);
		}

		public Cairo.PointD Size {
			get;
			set;
		}

		// TODO: come back with a better collision algorithm :p
		public Comment CommentAtPosition(Cairo.PointD position) {
			for(int i = Comments.Count - 1; i >= 0; --i) {
				var c = Comments[i];
				if(Math.Abs(c.Position.X - position.X) <= c.Size.X / 2 && Math.Abs(c.Position.Y - position.Y) < c.Size.Y / 2) {
					return c;
				}
			}

			return null;
		}

		public State StateAtPosition(Cairo.PointD position) {
			for(int i = States.Count - 1; i >= 0; --i) {
				var s = States[i];
				if(s.PointInState(position)) {
					return s;
				}
			}

			return null;
		}
		
		public Transition TransitionAtPosition(Cairo.PointD position) {
			for(int i = Transitions.Count - 1; i >= 0; --i) {
				var t = Transitions[i];
				if(Math.Abs(t.Position.X - position.X) <= t.Width / 2 && Math.Abs(t.Position.Y - position.Y) < t.Height / 2) {
					return t;
				}
			}

			return null;
		}

		public void RemoveComment(Comment c) {
			Comments.Remove(c);
		}

		public void RemoveState(State a) {
			States.Remove(a);
		}

		public void RemoveTransition(Transition t)
		{
			t.Before.RemoveTransitionAfter(t);
			t.After.RemoveTransitionBefore(t);

			Transitions.Remove(t);
		}

		public List<Comment> Comments {
			get;
			private set;
		}

		public List<State> States {
			get;
			private set;
		}

		public List<Transition> Transitions {
			get;
			private set;
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
			foreach(var c in Comments) {
				if(c.ID == id)
					return c;
			}

			return null;
		}

		// Recursively gets all of the Action/PetriNet/Transitions
		public List<Entity> BuildEntitiesList() {
			var l = new List<Entity>();
			l.AddRange(this.States);

			for(int i = 0; i < this.States.Count; ++i) {
				var s = l[i] as PetriNet;
				if(s != null) {
					l.AddRange(s.BuildEntitiesList());
				}
			}

			l.AddRange(this.Transitions);
			l.AddRange(this.Comments);

			return l;
		}
	}
}

