using System;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public sealed class InnerPetriNet : PetriNet
	{
		public InnerPetriNet(Document doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
		{
			this.exitPoint = new ExitPoint(doc, this, new Cairo.PointD(300, 100));
			this.AddState(this.ExitPoint);
		}

		public InnerPetriNet(Document doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			foreach(var s in this.States) {
				if(s.GetType() == typeof(ExitPoint)) {
					this.exitPoint = s as ExitPoint;
					break;
				}
			}

			if(this.exitPoint == null)
				throw new Exception("No Exit node found in the saved Petri net!");
		}

		public ExitPoint ExitPoint {
			get {
				return exitPoint;
			}
		}

		public string EntryPointName {
			get {
				return this.CppName + "_Entry";
			}
		}

		public override void GenerateCpp(Cpp.Generator source, IDManager lastID) {
			string name = this.EntryPointName;

			// Adding an entry point
			source += "auto " + name + " = std::make_shared<Action>();";
			source += name + "->setAction(" + "make_callable_ptr([](){ return ResultatAction::REUSSI; })" + ");";
			source += name + "->setRequiredTokens(" + this.RequiredTokens.ToString() + ");";

			source += name + "->setName(\"" + this.Name + "_Entry" + "\");";
			source += name + "->setRequiredTokens(" + RequiredTokens.ToString() + ");";
			source += name + "->setID(" + lastID.Consume() + ");";

			source += "stateChart->addAction(" + name + ", " + "false" + ");";

			base.GenerateCpp(source, lastID);
		}

		ExitPoint exitPoint;
	}
}

