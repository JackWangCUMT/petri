using System;
using System.Resources;
using System.Xml;
using System.Xml.Linq;

namespace Statechart
{
	public sealed class ExitPoint : NonRootState
	{
		public ExitPoint(Document doc, StateChart parent, Cairo.PointD pos) : base(doc, parent, false, pos)
		{
			this.Radius = 25;
		}
		public ExitPoint(Document doc, StateChart parent, XElement descriptor) : base(doc, parent, descriptor) {}

		public override XElement GetXml() {
			var elem = new XElement("Exit");
			this.Serialize(elem);
			return elem;
		}

		public override bool Active {
			get {
				return false;
			}
			set {
				base.Active = false;
			}
		}

		public override int RequiredTokens {
			get {
				return this.TransitionsBefore.Count;
			}
			set {
			}
		}

		public override string Name {
			get {
				return "End";
			}
			set {
				base.Name = "End";
			}
		}

		public override bool UsesHeader(string h) {
			return false;
		}

		public override void GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source += "auto " + this.CppName + " = std::make_shared<Action>();";
			source += this.CppName + "->setAction(" + "make_callable_ptr([](){ return ResultatAction::ActionReussie; })" + ");";
			source += this.CppName + "->setRequiredTokens(" + this.RequiredTokens.ToString() + ");";

			source += this.CppName + "->setName(\"" + this.Parent.Name + "_" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += "stateChart->addAction(" + this.CppName + ", " + "false" + ");";
		}
	}
}

