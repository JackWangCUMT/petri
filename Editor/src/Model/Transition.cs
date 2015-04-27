using System;
using Cairo;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Petri
{
	public class Transition : Entity
	{
		public Transition(HeadlessDocument doc, PetriNet s, State before, State after) : base(doc, s)
		{
			this.Before = before;
			this.After = after;

			this.Name = ID.ToString();

			this.Width = 50;
			this.Height = 30;

			this.Shift = new PointD(0, 0);
			this.ShiftAmplitude = PetriView.Norm(this.Direction());

			base.Position = new PointD(0, 0);

			this.Condition = new ExpressionCondition(Cpp.Expression.CreateFromString<Cpp.Expression>("Résultat == " + doc.Settings.Enum.Name + "::" + doc.Settings.Enum.Members[0], this, doc.AllFunctions, doc.CppMacros));

			this.UpdatePosition();
		}

		public Transition(HeadlessDocument doc, PetriNet parent, XElement descriptor, IDictionary<UInt64, State> statesTable) : base(doc, parent, descriptor) {
			this.Before = statesTable[UInt64.Parse(descriptor.Attribute("BeforeID").Value)];
			this.After = statesTable[UInt64.Parse(descriptor.Attribute("AfterID").Value)];

			TrySetCondition(descriptor.Attribute("Condition").Value);

			this.Width = double.Parse(descriptor.Attribute("W").Value);
			this.Height = double.Parse(descriptor.Attribute("H").Value);

			this.Shift = new Cairo.PointD(XmlConvert.ToDouble(descriptor.Attribute("ShiftX").Value), XmlConvert.ToDouble(descriptor.Attribute("ShiftY").Value));
			this.ShiftAmplitude = XmlConvert.ToDouble(descriptor.Attribute("ShiftAmplitude").Value);

			this.Position = this.Position;
		}

		private void TrySetCondition(string s) {
			try {
				this.Condition = ConditionBase.ConditionFromString(s, this, Document.AllFunctions, Document.CppMacros);
			}
			catch(Exception) {
				Document.Conflicting.Add(this);
				this.Condition = new ExpressionCondition(new Cpp.LitteralExpression(s));
			}
		}

		public void UpdateConflicts() {
			this.TrySetCondition(Condition.MakeUserReadable());
		}

		public override XElement GetXml() {
			var elem = new XElement("Transition");
			this.Serialize(elem);
			return elem;
		}

		public override void Serialize(XElement elem) {
			base.Serialize(elem);
			elem.SetAttributeValue("BeforeID", this.Before.ID);
			elem.SetAttributeValue("AfterID", this.After.ID);

			elem.SetAttributeValue("Condition", this.Condition.MakeUserReadable());

			elem.SetAttributeValue("W", this.Width);
			elem.SetAttributeValue("H", this.Height);

			elem.SetAttributeValue("ShiftX", this.Shift.X);
			elem.SetAttributeValue("ShiftY", this.Shift.Y);
			elem.SetAttributeValue("ShiftAmplitude", this.ShiftAmplitude);
		}

		public override bool UsesFunction(Cpp.Function f) {
			return Condition.UsesFunction(f);
		}

		public PointD Direction() {
			return new PointD(After.Position.X - Before.Position.X, After.Position.Y - Before.Position.Y);
		}

		public void UpdatePosition() {
			double norm = PetriView.Norm(this.Direction());
			PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
			this.Position = new PointD(center.X + Shift.X * norm / ((ShiftAmplitude > 1e-3) ? ShiftAmplitude : 1), center.Y + Shift.Y * norm / ((ShiftAmplitude > 1e-3) ? ShiftAmplitude : 1));
		}

		public State Before {
			get;
			set;
		}

		public State After {
			get;
			set;
		}

		public override PointD Position {
			get {
				return base.Position;
			}
			set {
				base.Position = value;

				// Prevents access during construction
				if(this.After != null) {
					ShiftAmplitude = PetriView.Norm(this.Direction());
					PointD center = new PointD((Before.Position.X + After.Position.X) / 2, (Before.Position.Y + After.Position.Y) / 2);
					Shift = new PointD(value.X - center.X, value.Y - center.Y);
				}
			}
		}

		public double Width {
			get;
			set;
		}

		public double Height {
			get;
			set;
		}

		public PointD Shift {
			get;
			set;
		}

		public double ShiftAmplitude {
			get;
			set;
		}

		public ConditionBase Condition {
			get;
			set;
		}

		public override string CppName {
			get {
				return "transition_" + this.ID.ToString();
			}
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
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

						source += "auto " + name + " = std::make_shared<Transition<" + Document.Settings.Enum.Name + ">>(*" + aName + ", *" + s.CppName + ");";
						source += name + "->setCondition(" + "std::make_shared<Condition>(make_callable_ptr([](){ return true; }))" + ");";

						source += name + "->setName(\"" + name + "\");";
						source += name + "->setID(" + newID.ToString() + ");";
						source += aName + "->addTransition(" + name + ");";
					}
				}
			}

			source += "auto " + this.CppName + " = std::make_shared<Transition<" + Document.Settings.Enum.Name + ">>(*" + bName + ", *" + aName + ");";
			source += this.CppName + "->setCondition(" + this.Condition.MakeCpp() + ");";
		
			source += this.CppName + "->setName(\"" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += bName + "->addTransition(" + this.CppName + ");";

			return "";
		}
	}
}

