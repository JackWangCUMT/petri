using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Petri
{
	public class IDManager {
		public IDManager(UInt64 LastID) {
			ID = LastID + 1;
		}

		public UInt64 Consume() {
			return ID++;
		}

		UInt64 ID;
	}

	public abstract class Entity
	{
		public Entity(Document doc, PetriNet parent) {
			this.parent = parent;
			this.Document = doc;
			this.ID = Document.LastEntityID++;
		}

		public static Entity EntityFromXml(Document document, XElement descriptor, PetriNet parent, IDictionary<UInt64, State> statesTable) {
			switch(descriptor.Name.ToString()) {
			case "Action":
				return new Action(document, parent, descriptor, document.AllFunctions);
			case "Exit":
				return new ExitPoint(document, parent, descriptor);
			case "PetriNet":
				if(parent == null)
					return new RootPetriNet(document, descriptor);
				else
					return new InnerPetriNet(document, parent, descriptor);
			case "Transition":
				return new Transition(document, parent, descriptor, statesTable, document.AllFunctions);
			case "Comment":
				return new Comment(document, parent, descriptor);
			default:
				return null;
			}
		}

		public Entity(Document doc, PetriNet parent, XElement descriptor) {
			this.parent = parent;
			this.Document = doc;

			this.ID = UInt64.Parse(descriptor.Attribute("ID").Value);
			this.Name = descriptor.Attribute("Name").Value;
			this.Position = new Cairo.PointD(double.Parse(descriptor.Attribute("X").Value), double.Parse(descriptor.Attribute("Y").Value));

			if(this.ID >= Document.LastEntityID)
				Document.LastEntityID = this.ID + 1;
		}

		public abstract XElement GetXml();

		public virtual void Serialize(XElement element) {
			element.SetAttributeValue("ID", this.ID.ToString());
			element.SetAttributeValue("Name", this.Name.ToString());
			element.SetAttributeValue("X", this.Position.X.ToString());
			element.SetAttributeValue("Y", this.Position.Y.ToString());
		}

		public UInt64 ID {
			get {
				return id;
			}
			set {
				id = value;
			}
		}

		public virtual string Name {
			get {
				return name;
			}
			set {
				if(value == "_") {
					throw new ArgumentException();
				}
				name = value;
			}
		}

		public PetriNet Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public virtual Document Document {
			get {
				return this.Parent.Document;
			}
			set {
				if(this.Parent != null)
					parent.Document = value;
			}
		}

		public virtual Cairo.PointD Position {
			get {
				return position;
			}
			set {
				position = value;
			}
		}

		public abstract bool UsesHeader(string header);
		public abstract string GenerateCpp(Cpp.Generator source, IDManager lastID);
		public abstract string CppName {
			get;
		}

		UInt64 id;
		string name;
		PetriNet parent;
		Cairo.PointD position;
	}
}

