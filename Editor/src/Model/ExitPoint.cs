using System;
using System.Resources;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public sealed class ExitPoint : NonRootState
	{
		public ExitPoint(HeadlessDocument doc, PetriNet parent, Cairo.PointD pos) : base(doc, parent, false, pos) {
			this.Radius = 25;
		}

		public ExitPoint(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			
		}

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

		public override bool UsesFunction(Cpp.Function f) {
			return false;
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source += "auto " + this.CppName + " = std::make_shared<Action<" + Document.Settings.Enum.Name + ">>();";
			source += this.CppName + "->setAction(make_callable([](){ return " + Document.Settings.Enum.Name + "(); }));";
			source += this.CppName + "->setRequiredTokens(" + this.RequiredTokens.ToString() + ");";

			source += this.CppName + "->setName(\"" + this.Parent.Name + "_" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += "petriNet.addAction(" + this.CppName + ", " + "false" + ");";

			return "";
		}
	}
}

