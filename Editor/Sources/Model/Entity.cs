/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Petri.Editor
{
    /// <summary>
    /// The base class from every entity enclosed in a petri net.
    /// </summary>
    public abstract class Entity
    {
        public Entity(HeadlessDocument doc, PetriNet parent)
        {
            Parent = parent;
            this.Document = doc;
            this.ID = Document.IDManager.Consume();
        }

        public static Entity EntityFromXml(HeadlessDocument document,
                                           XElement descriptor,
                                           PetriNet parent,
                                           IDictionary<UInt64, State> statesTable)
        {
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

        public Entity(HeadlessDocument doc, PetriNet parent, XElement descriptor)
        {
            Parent = parent;
            this.Document = doc;

            this.ID = XmlConvert.ToUInt64(descriptor.Attribute("ID").Value);
            this.Name = descriptor.Attribute("Name").Value;
            this.Position = new Cairo.PointD(XmlConvert.ToDouble(descriptor.Attribute("X").Value),
                                             XmlConvert.ToDouble(descriptor.Attribute("Y").Value));

            if(this.ID >= Document.IDManager.ID)
                Document.IDManager = new IDManager(this.ID + 1);
        }

        /// <summary>
        /// Creates an empty node correctly named to represent an entity, and fill it with the serialization data.
        /// </summary>
        /// <returns>The empty node.</returns>
        public abstract XElement GetXML();

        /// <summary>
        /// Serialize the specified element into the provided XML element.
        /// </summary>
        /// <param name="element">The XML element to fill with the serialization data.</param>
        protected virtual void Serialize(XElement element)
        {
            element.SetAttributeValue("ID", this.ID);
            element.SetAttributeValue("Name", this.Name);
            element.SetAttributeValue("X", this.Position.X);
            element.SetAttributeValue("Y", this.Position.Y);
        }

        /// <summary>
        /// Gets or sets the identifier of the instance.
        /// </summary>
        /// <value>The identifier of the instance.</value>
        public UInt64 ID {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <exception cref="ArgumentException">When the value contains two consecutive '_' characters, or starts or ends with '_' (this has to do with C++ name mangling and UB).</exception>
        /// <value>The name.</value>
        public virtual string Name {
            get {
                return _name;
            }
            set {
                if(value.StartsWith("_") || value.EndsWith("_") || value.Contains("__")) {
                    throw new ArgumentException();
                }
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public PetriNet Parent {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parent's document.
        /// </summary>
        /// <value>The document.</value>
        public virtual HeadlessDocument Document {
            get {
                return this.Parent.Document;
            }
            set {
                if(this.Parent != null)
                    Parent.Document = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Petri.Editor.Entity"/> sticks to the grid.
        /// </summary>
        /// <value><c>true</c> if stick to grid; otherwise, <c>false</c>.</value>
        public virtual bool StickToGrid {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets the size of the grid on which the entities will stick if requested.
        /// </summary>
        /// <value>The size of the grid.</value>
        static public int GridSize {
            get {
                return 10;
            }
        }

        /// <summary>
        /// Gets or sets the position of the entity in its parent's frame.
        /// </summary>
        /// <value>The position.</value>
        public virtual Cairo.PointD Position {
            get {
                return _position;
            }
            set {
                _position = new Cairo.PointD(value.X, value.Y);
            }
        }

        public abstract bool UsesFunction(Code.Function f);

        /// <summary>
        /// Gets the code identifier of the entity, i.e. the name of the variable containing the entity in the generated code.
        /// </summary>
        /// <value>The code identifier.</value>
        public abstract string CodeIdentifier {
            get;
        }

        string _name;
        Cairo.PointD _position;
    }
}

