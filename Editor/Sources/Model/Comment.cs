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
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using Cairo;

namespace Petri.Editor
{
    /// <summary>
    /// A graphical entity containing a text comment. It does not interact in any way with the other Entity subclasses, other than being contained in PetriNets.
    /// Only for documentation/hints purpose.
    /// </summary>
    public class Comment : Entity
    {
        public Comment(HeadlessDocument doc, PetriNet parent, Cairo.PointD pos) : base(doc, parent)
        {
            this.Position = pos;
            this.Name = Configuration.GetLocalized("DefaultCommentName");
            this.SizeToFit();

            this.Color = new Color(1, 1, 0.7, 1);
        }

        public Comment(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor)
        {
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

        public override XElement GetXML()
        {
            var elem = new XElement("Comment");
            this.Serialize(elem);
            return elem;
        }

        protected override void Serialize(XElement elem)
        {
            base.Serialize(elem);
            elem.SetAttributeValue("Width", Size.X);
            elem.SetAttributeValue("Height", Size.Y);

            elem.SetAttributeValue("R", Color.R);
            elem.SetAttributeValue("G", Color.G);
            elem.SetAttributeValue("B", Color.B);
            elem.SetAttributeValue("A", Color.A);
        }

        /// <summary>
        /// Gets or sets the size of the comment's frame.
        /// </summary>
        /// <value>The size.</value>
        public PointD Size {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the color of the comment's background.
        /// </summary>
        /// <value>The color.</value>
        public Color Color {
            get;
            set;
        }

        /// <summary>
        /// Nothing to see here, as the Entity is not code generated at all.
        /// </summary>
        /// <returns><c>true</c>, if function was used, <c>false</c> otherwise.</returns>
        /// <param name="f">F.</param>
        public override bool UsesFunction(Code.Function f)
        {
            return false;
        }

        /// <summary>
        /// Nothing to see here, as the Entity is not code generated at all.
        /// </summary>
        /// <value>The code identifier.</value>
        public override string CodeIdentifier {
            get {
                return null;
            }
        }

        public void SizeToFit()
        {
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

