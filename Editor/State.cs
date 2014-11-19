using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public abstract class State : Entity
	{
		public State(Document doc, PetriNet parent, bool active, int requiredTokens, Cairo.PointD pos) : base(doc, parent) {
			this.transitionsBefore = new List<Transition>();
			this.transitionsAfter = new List<Transition>();

			this.Parent = parent;
			this.Active = active;
			this.RequiredTokens = requiredTokens;
			this.Position = pos;
			this.Name = this.ID.ToString();
		}

		public State(Document doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			this.transitionsBefore = new List<Transition>();
			this.transitionsAfter = new List<Transition>();

			this.Active = bool.Parse(descriptor.Attribute("Active").Value);
			this.RequiredTokens = int.Parse(descriptor.Attribute("RequiredTokens").Value);
			this.Radius = double.Parse(descriptor.Attribute("Radius").Value);
		}

		public override void Serialize(XElement element) {
			base.Serialize(element);
			element.SetAttributeValue("Active", this.Active.ToString());
			element.SetAttributeValue("RequiredTokens", this.RequiredTokens.ToString());
			element.SetAttributeValue("Radius", this.Radius.ToString());
		}

		public virtual bool Active {
			get {
				return active;
			}
			set {
				active = value;
				Document.Controller.Modified = true;
			}
		}

		public virtual int RequiredTokens {
			get {
				return requiredTokens;
			}
			set {
				requiredTokens = value;
				Document.Controller.Modified = true;
			}
		}

		public List<Transition> TransitionsBefore {
			get {
				return transitionsBefore;
			}
		}

		public List<Transition> TransitionsAfter {
			get {
				return transitionsAfter;
			}
		}

		public void AddTransitionBefore(Transition t)
		{
			transitionsBefore.Add(t);
			Document.Controller.Modified = true;
		}

		public void AddTransitionAfter(Transition t)
		{
			transitionsAfter.Add(t);
			Document.Controller.Modified = true;
		}

		public void RemoveTransitionBefore(Transition t) {
			TransitionsBefore.Remove(t);
		}

		public void RemoveTransitionAfter(Transition t) {
			TransitionsAfter.Remove(t);
		}

		public override Cairo.PointD Position {
			get {
				return base.Position;
			}
			set {
				base.Position = value;

				// Prevent execution during State construction
				if(this.TransitionsBefore != null) {
					foreach(Transition t in TransitionsBefore) {
						t.UpdatePosition();
					}
					foreach(Transition t in TransitionsAfter) {
						t.UpdatePosition();
					}
				}

				Document.Controller.Modified = true;
			}
		}

		public double Radius {
			get {
				return radius;
			}
			set {
				radius = value;
				Document.Controller.Modified = true;
			}
		}

		public override string CppName {
			get {
				return "state_" + this.ID.ToString();
			}
		}

		public virtual bool PointInState(Cairo.PointD p) {
			if(Math.Pow(p.X - this.Position.X, 2) + Math.Pow(p.Y - this.Position.Y, 2) < Math.Pow(this.Radius, 2)) {
				return true;
			}

			return false;
		}

		private List<Transition> transitionsBefore;
		private List<Transition> transitionsAfter;
		private bool active;
		double radius = 10;
		int requiredTokens = 0;
	}

	public abstract class NonRootState : State {
		public NonRootState(Document doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, 0, pos) {}

		public NonRootState(Document doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {}
	}
}

