using System;
using System.Xml.Linq;
using System.Collections.Generic;
using Cairo;

namespace Petri
{
	public class Comment : Entity
	{
		public Comment(Document doc, PetriNet parent, Cairo.PointD pos) : base(doc, parent) {
			this.Position = pos;
			this.Name = "Commentaire";
		}

		public Comment(Document doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {

		}

		public override XElement GetXml() {
			var elem = new XElement("Comment");
			this.Serialize(elem);
			return elem;
		}

		public PointD Size {
			get;
			set;
		}

		public override bool UsesHeader(string Header) {
			return false;
		}

		public override string CppName {
			get {
				return "";
			}
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			return "";
		}

	}
}

