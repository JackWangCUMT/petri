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
		public Entity(HeadlessDocument doc, PetriNet parent) {
			_parent = parent;
			this.Document = doc;
			this.ID = Document.LastEntityID++;
		}

		public static Entity EntityFromXml(HeadlessDocument document, XElement descriptor, PetriNet parent, IDictionary<UInt64, State> statesTable) {
			switch(descriptor.Name.ToString()) {
			case "Action":
				return new Action(document, parent, descriptor);
			case "Exit":
				return new ExitPoint(document, parent, descriptor);
			case "PetriNet":
				if(parent == null)
					return new RootPetriNet(document, descriptor);
				else
					return new InnerPetriNet(document, parent, descriptor);
			case "Transition":
				return new Transition(document, parent, descriptor, statesTable);
			case "Comment":
				return new Comment(document, parent, descriptor);
			default:
				return null;
			}
		}

		public Entity(HeadlessDocument doc, PetriNet parent, XElement descriptor) {
			_parent = parent;
			this.Document = doc;

			this.ID = XmlConvert.ToUInt64(descriptor.Attribute("ID").Value);
			this.Name = descriptor.Attribute("Name").Value;
			this.Position = new Cairo.PointD(XmlConvert.ToDouble(descriptor.Attribute("X").Value), XmlConvert.ToDouble(descriptor.Attribute("Y").Value));

			if(this.ID >= Document.LastEntityID)
				Document.LastEntityID = this.ID + 1;
		}

		public abstract XElement GetXml();

		public virtual void Serialize(XElement element) {
			element.SetAttributeValue("ID", this.ID);
			element.SetAttributeValue("Name", this.Name);
			element.SetAttributeValue("X", this.Position.X);
			element.SetAttributeValue("Y", this.Position.Y);
		}

		public UInt64 ID {
			get {
				return _id;
			}
			set {
				_id = value;
			}
		}

		public virtual string Name {
			get {
				return _name;
			}
			set {
				if(value == "_") {
					throw new ArgumentException();
				}
				_name = value;
			}
		}

		public PetriNet Parent {
			get {
				return _parent;
			}
			set {
				_parent = value;
			}
		}

		public virtual HeadlessDocument Document {
			get {
				return this.Parent.Document;
			}
			set {
				if(this.Parent != null)
					_parent.Document = value;
			}
		}

		public virtual Cairo.PointD Position {
			get {
				return _position;
			}
			set {
				int gridSize = 10;
				_position = new Cairo.PointD(Math.Truncate(value.X / gridSize) * gridSize, Math.Truncate(value.Y / gridSize) * gridSize);
			}
		}

		public abstract bool UsesFunction(Cpp.Function f);
		public abstract string GenerateCpp(Cpp.Generator source, IDManager lastID);
		public abstract string CppName {
			get;
		}

		UInt64 _id;
		string _name;
		PetriNet _parent;
		Cairo.PointD _position;
	}
}

