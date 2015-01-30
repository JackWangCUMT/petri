using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public abstract class State : Entity
	{
		public State(HeadlessDocument doc, PetriNet parent, bool active, int requiredTokens, Cairo.PointD pos) : base(doc, parent) {
			this.TransitionsBefore = new List<Transition>();
			this.TransitionsAfter = new List<Transition>();

			this.Active = active;
			this.RequiredTokens = requiredTokens;
			this.Position = pos;
			this.Name = this.ID.ToString();
		}

		public State(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			this.TransitionsBefore = new List<Transition>();
			this.TransitionsAfter = new List<Transition>();

			this.Active = XmlConvert.ToBoolean(descriptor.Attribute("Active").Value);
			this.RequiredTokens = XmlConvert.ToInt32(descriptor.Attribute("RequiredTokens").Value);
			this.Radius = XmlConvert.ToDouble(descriptor.Attribute("Radius").Value);
		}

		public override void Serialize(XElement element) {
			base.Serialize(element);
			element.SetAttributeValue("Active", this.Active);
			element.SetAttributeValue("RequiredTokens", this.RequiredTokens);
			element.SetAttributeValue("Radius", this.Radius);
		}

		public virtual bool Active {
			get;
			set;
		}

		public virtual int RequiredTokens {
			get;
			set;
		}

		public List<Transition> TransitionsBefore {
			get;
			private set;
		}

		public List<Transition> TransitionsAfter {
			get;
			private set;
		}

		public void AddTransitionBefore(Transition t)
		{
			TransitionsBefore.Add(t);
		}

		public void AddTransitionAfter(Transition t)
		{
			TransitionsAfter.Add(t);
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
			}
		}

		public double Radius {
			get;
			set;
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
	}

	public abstract class NonRootState : State {
		public NonRootState(HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, 0, pos) {}

		public NonRootState(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {}
	}
}

