using System;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using Cairo;

namespace Petri
{
	public class Comment : Entity
	{
		public Comment(HeadlessDocument doc, PetriNet parent, Cairo.PointD pos) : base(doc, parent) {
			this.Position = pos;
			this.Name = "Commentaire";
			this.SizeToFit();

			this.Color = new Color(1, 1, 0.7, 1);
		}

		public Comment(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			var size = new PointD();
			size.X = XmlConvert.ToDouble(descriptor.Attribute("Width").Value);
			size.Y = XmlConvert.ToDouble(descriptor.Attribute("Height").Value);
			Size = size;

			var color = new Color();
			color.R = XmlConvert.ToDouble(descriptor.Attribute("R").Value);
			color.G = XmlConvert.ToDouble(descriptor.Attribute("G").Value);
			color.B = XmlConvert.ToDouble(descriptor.Attribute("B").Value);
			color.A = XmlConvert.ToDouble(descriptor.Attribute("A").Value);
			Color = color;
		}

		public override XElement GetXml() {
			var elem = new XElement("Comment");
			this.Serialize(elem);
			return elem;
		}

		public override void Serialize(XElement elem) {
			base.Serialize(elem);
			elem.SetAttributeValue("Width", Size.X);
			elem.SetAttributeValue("Height", Size.Y);

			elem.SetAttributeValue("R", Color.R);
			elem.SetAttributeValue("G", Color.G);
			elem.SetAttributeValue("B", Color.B);
			elem.SetAttributeValue("A", Color.A);
		}

		public PointD Size {
			get;
			set;
		}

		public Color Color {
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

		public void SizeToFit() {
			var layout = new Pango.Layout(Gdk.PangoHelper.ContextGet());

			layout.FontDescription = new Pango.FontDescription();
			layout.FontDescription.Family = "Arial";
			layout.FontDescription.Size = Pango.Units.FromPixels(12);

			layout.SetText(Name);

			int width, height;
			layout.GetPixelSize(out width, out height);

			this.Size = new PointD(width + 10, height + 10);
		}
	}
}

