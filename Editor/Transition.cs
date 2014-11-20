using System;
using Cairo;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Petri
{
	public class Transition : Entity
	{
		public Transition(Document doc, PetriNet s, State before, State after) : base(doc, s)
		{
			this.before = before;
			this.after = after;

			this.Name = ID.ToString();

			this.Width = 50;
			this.Height = 30;

			//this.Before.AddTransitionAfter(this);
			//this.After.AddTransitionBefore(this);

			this.shiftAgainstAxis = new PointD(0, 0);
			this.shiftAmplitude = PetriView.Norm(this.Direction());

			base.Position = new PointD(0, 0);

			this.Condition = new CheckResultCondition(this, Action.ResultatAction.REUSSI);

			this.UpdatePosition();
		}

		public Transition(Document doc, PetriNet parent, XElement descriptor, IDictionary<UInt64, State> statesTable, List<Cpp.Function> conditions) : base(doc, parent, descriptor) {
			this.before = statesTable[UInt64.Parse(descriptor.Attribute("BeforeID").Value)];
			this.after = statesTable[UInt64.Parse(descriptor.Attribute("AfterID").Value)];

			//this.before.AddTransitionAfter(this);
			//this.after.AddTransitionBefore(this);

			this.Condition = ConditionBase.ConditionFromString(descriptor.Attribute("Condition").Value, this, conditions);

			this.Width = double.Parse(descriptor.Attribute("W").Value);
			this.Height = double.Parse(descriptor.Attribute("H").Value);

			this.Shift = new Cairo.PointD(double.Parse(descriptor.Attribute("ShiftX").Value), double.Parse(descriptor.Attribute("ShiftY").Value));
			this.ShiftAmplitude = double.Parse(descriptor.Attribute("ShiftAmplitude").Value);

			this.Position = this.Position;
		}

		public override XElement GetXml() {
			var elem = new XElement("Transition");
			this.Serialize(elem);
			return elem;
		}

		public override void Serialize(XElement elem) {
			base.Serialize(elem);
			elem.SetAttributeValue("BeforeID", this.Before.ID.ToString());
			elem.SetAttributeValue("AfterID", this.After.ID.ToString());

			elem.SetAttributeValue("Condition", this.Condition.MakeUserReadable());

			elem.SetAttributeValue("W", this.Width.ToString());
			elem.SetAttributeValue("H", this.Height.ToString());

			elem.SetAttributeValue("ShiftX", this.Shift.X.ToString());
			elem.SetAttributeValue("ShiftY", this.Shift.Y.ToString());
			elem.SetAttributeValue("ShiftAmplitude", this.ShiftAmplitude.ToString());
		}

		public override bool UsesHeader(string h) {
			return this.Condition.UsesHeader(h);
		}

		public PointD Direction()
		{
			return new PointD(after.Position.X - before.Position.X, after.Position.Y - before.Position.Y);
		}

		public void UpdatePosition()
		{
			double norm = PetriView.Norm(this.Direction());
			PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
			this.Position = new PointD(center.X + shiftAgainstAxis.X * norm / ((shiftAmplitude > 1e-3) ? shiftAmplitude : 1), center.Y + shiftAgainstAxis.Y * norm / ((shiftAmplitude > 1e-3) ? shiftAmplitude : 1));
			Document.Controller.Modified = true;
		}

		public State Before {
			get {
				return before;
			}
			set {
				before = value;
			}
		}

		public State After {
			get {
				return after;
			}
			set {
				after = value;
			}
		}

		public override PointD Position {
			get {
				return base.Position;
			}
			set {
				base.Position = value;

				// Prevents access during construction
				if(this.After != null) {
					shiftAmplitude = PetriView.Norm(this.Direction());
					PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
					shiftAgainstAxis = new PointD(value.X - center.X, value.Y - center.Y);
					Document.Controller.Modified = true;
				}
			}
		}

		public double Width {
			get {
				return width;
			}
			set {
				width = value;
				Document.Controller.Modified = true;
			}
		}

		public double Height {
			get {
				return height;
			}
			set {
				height = value;
				Document.Controller.Modified = true;
			}
		}

		public PointD Shift {
			get {
				return shiftAgainstAxis;
			}
			set {
				shiftAgainstAxis = value;
				Document.Controller.Modified = true;
			}
		}

		public double ShiftAmplitude {
			get {
				return shiftAmplitude;
			}
			set {
				shiftAmplitude = value;
				Document.Controller.Modified = true;
			}
		}

		public ConditionBase Condition {
			get {
				return condition;
			}
			set {
				condition = value;
				Document.Controller.Modified = true;
			}
		}

		public override string CppName {
			get {
				return "transition_" + this.ID.ToString();
			}
		}

		public override void GenerateCpp(Cpp.Generator source, IDManager lastID) {
			string bName = this.Before.CppName;
			string aName = this.After.CppName;

			var b = this.Before as InnerPetriNet;
			if(b != null) {
				bName = b.ExitPoint.CppName;
			}

			var a = this.After as InnerPetriNet;
			if(a != null) {
				aName = a.EntryPointName;

				// Adding a transition from the entry point to all of the initially active states
				foreach(State s in a.States) {
					if(s.Active) {
						var newID = lastID.Consume();
						string name = this.CppName + "_Entry_" + newID.ToString();

						source += "auto " + name + " = std::make_shared<Transition>(*" + aName + ", *" + s.CppName + ");";
						source += name + "->setCondition(" + "std::make_shared<Condition>(make_callable_ptr([](){ return true; }))" + ");";

						source += name + "->setName(\"" + name + "\");";
						source += name + "->setID(" + newID.ToString() + ");";
						source += aName + "->addTransition(" + name + ");";
					}
				}
			}

			source += "auto " + this.CppName + " = std::make_shared<Transition>(*" + bName + ", *" + aName + ");";
			source += this.CppName + "->setCondition(" + this.Condition.MakeCpp() + ");";
		
			source += this.CppName + "->setName(\"" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += bName + "->addTransition(" + this.CppName + ");";
		}

		private State before;
		private State after;

		private PointD shiftAgainstAxis;
		private double shiftAmplitude;

		double width;
		double height;

		private ConditionBase condition;
	}
}

