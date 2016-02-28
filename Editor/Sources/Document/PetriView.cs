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
using Gtk;
using Cairo;
using System.Collections.Generic;
using System.Linq;

namespace Petri.Editor
{
    public abstract class PetriView
    {
        public PetriView(HeadlessDocument doc)
        {
            _document = doc;

            Zoom = 1.0f;
        }

        protected abstract EntityDraw EntityDraw {
            get;
            set;
        }

        protected abstract PointD PathPosition {
            get;
        }

        protected abstract PointD GetExtents(PetriNet petriNet);

        protected virtual PointD RenderInternal(Context context, PetriNet petriNet)
        {
            var extents = GetExtents(petriNet);

            context.LineWidth = 4;
            context.MoveTo(0, 0);
            context.LineTo(extents.X, 0);
            context.LineTo(extents.X, extents.Y);
            context.LineTo(0, extents.Y);
            context.LineTo(0, 0);

            context.SetSourceRGBA(1, 1, 1, 1);
            context.Fill();

            double minX = 0, minY = 0;

            foreach(var t in petriNet.Transitions) {
                if(t.Position.X + t.Width / 2 > minX)
                    minX = t.Position.X + t.Width / 2;
                if(t.Position.Y > minY + t.Height / 2)
                    minY = t.Position.Y + t.Height / 2;

                this.EntityDraw.Draw(t, context);
            }

            foreach(var s in petriNet.States) {
                if(s.Position.X + s.Radius / 2 > minX)
                    minX = s.Position.X + s.Radius / 2;
                if(s.Position.Y + s.Radius / 2 > minY)
                    minY = s.Position.Y + s.Radius / 2;

                this.EntityDraw.Draw(s, context);
            }

            foreach(var c in petriNet.Comments) {
                if(c.Position.X + c.Size.X / 2 > minX)
                    minX = c.Position.X + c.Size.X / 2;
                if(c.Position.Y + c.Size.Y / 2 > minY)
                    minY = c.Position.Y + c.Size.Y / 2;

                this.EntityDraw.Draw(c, context);
            }

            {
                context.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                context.SetFontSize(16);

                string val = "";
                PetriNet petri = CurrentPetriNet;
                if(_parentHierarchy.Count == 0) {
                    pathSeparatorLenth = context.TextExtents(" / ").Width;
                    do {
                        ParentStruct pStruct = new ParentStruct();
                        pStruct.petriNet = petri;
                        string sep = petri.Parent == null ? "" : " / ";
                        pStruct.extents = context.TextExtents(sep + petri.Name);
                        val = sep + petri.Name + val;
                        _parentHierarchy.Insert(0, pStruct);
                        petri = petri.Parent;
                    } while(petri != null);

                    ParentStruct path = new ParentStruct();
                    path.petriNet = null;
                    path.extents = context.TextExtents(val);
                    _parentHierarchy.Add(path);
                }
                else {
                    do {
                        ParentStruct pStruct = new ParentStruct();
                        pStruct.petriNet = petri;
                        string sep = petri.Parent == null ? "" : " / ";
                        val = sep + petri.Name + val;
                        petri = petri.Parent;
                    } while(petri != null);
                }

                context.SetSourceRGBA(0.9, 0.9, 0.9, 1);
                TextExtents ext = _parentHierarchy[_parentHierarchy.Count - 1].extents;
                var pathPosition = PathPosition;
                context.Rectangle((pathPosition.X + 10) / Zoom,
                                  (pathPosition.Y + 10) / Zoom,
                                  (ext.Width + 10) / Zoom,
                                  (ext.Height + 10) / Zoom);
                context.Fill();

                context.MoveTo((pathPosition.X + 15 - ext.XBearing) / Zoom,
                               (pathPosition.Y + 15 - ext.YBearing) / Zoom);

                context.SetFontSize(16 / Zoom);
                context.SetSourceRGBA(0.0, 0.6, 0.2, 1);

                context.TextPath(val);
                context.Fill();
            }

            this.SpecializedDrawing(context);

            context.LineWidth = 4 / Zoom;
            context.MoveTo(0, 0);
            context.LineTo(extents.X, 0);
            context.LineTo(extents.X, extents.Y);
            context.LineTo(0, extents.Y);
            context.LineTo(0, 0);
            context.SetSourceRGBA(0.7, 0.7, 0.7, 1);
            context.Stroke();

            minX += 50;
            minY += 50;

            minX *= Zoom;
            minY *= Zoom;

            petriNet.Size = new PointD(minX, minY);

            return new PointD(minX, minY);
        }

        protected virtual void SpecializedDrawing(Cairo.Context context)
        {

        }

        public float Zoom {
            get;
            set;
        }

        public RootPetriNet RootPetriNet {
            get {
                return _document.PetriNet;
            }
        }

        public virtual PetriNet CurrentPetriNet {
            get {
                return _currentPetriNet;
            }
            set {
                _currentPetriNet = value;
                _parentHierarchy.Clear();
            }
        }

        protected struct ParentStruct
        {
            public PetriNet petriNet;
            public TextExtents extents;
        }

        protected HeadlessDocument _document;

        protected PetriNet _currentPetriNet;

        protected double pathSeparatorLenth;
        protected List<ParentStruct> _parentHierarchy = new List<ParentStruct>();
    }
}
