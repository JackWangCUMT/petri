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
    [System.ComponentModel.ToolboxItem(true)]
    public abstract class PetriView : Gtk.DrawingArea
    {
        public PetriView(Document doc)
        {
            _document = doc;
            _needsRedraw = false;
            _deltaClick = new PointD(0, 0);
            _originalPosition = new PointD();
            _lastClickDate = DateTime.Now;
            _lastClickPosition = new PointD(0, 0);

            Zoom = 1.0f;

            this.ButtonPressEvent += (object o, ButtonPressEventArgs args) => {
                this.HasFocus = true;
            };
        }

        public void Redraw()
        {
            if(_document.Window.Gui == null || _document.Window.Gui.BaseView == this) {
                if(_needsRedraw == false) {
                    _needsRedraw = true;
                    this.QueueDraw();
                }
            }
        }

        public virtual void FocusIn()
        {
            this.Redraw();
        }

        public virtual void FocusOut()
        {
            this.Redraw();
        }

        protected virtual void ManageTwoButtonPress(uint button, double x, double y)
        {
        }

        protected virtual void ManageOneButtonPress(uint button, double x, double y)
        {
        }

        protected virtual void ManageButtonRelease(uint button, double x, double y)
        {
        }

        protected virtual void ManageMotion(double x, double y)
        {
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton ev)
        {
            if(ev.Type == Gdk.EventType.ButtonPress) {
                // The Windows version of GTK# currently doesn't detect TwoButtonPress events, so here is a lame simulation of it.
                if(/*ev.Type == Gdk.EventType.TwoButtonPress || */(_lastClickPosition.X == ev.X && _lastClickPosition.Y == ev.Y && (DateTime.Now - _lastClickDate).TotalMilliseconds < 500)) {
                    _lastClickPosition.X = -12345;

                    this.ManageTwoButtonPress(ev.Button, ev.X / Zoom, ev.Y / Zoom);
                }
                else {
                    _lastClickDate = DateTime.Now;
                    _lastClickPosition.X = ev.X;
                    _lastClickPosition.Y = ev.Y;

                    var scrolled = _document.Window.Gui.ScrolledWindow;

                    if(ev.X >= 15 + scrolled.Hadjustment.Value && ev.X < _parentHierarchy[_parentHierarchy.Count - 1].extents.Width + 15 + scrolled.Hadjustment.Value
                    && ev.Y >= 15 + scrolled.Vadjustment.Value && ev.Y < _parentHierarchy[_parentHierarchy.Count - 1].extents.Height + 15 + scrolled.Vadjustment.Value) {
                        double currX = 15;
                        foreach(var item in _parentHierarchy) {
                            if(item.petriNet != null && ev.X - currX < item.extents.Width + pathSeparatorLenth + scrolled.Hadjustment.Value) {
                                _nextPetriNet = item.petriNet;
                                break;
                            }
                            currX += item.extents.Width + pathSeparatorLenth;
                        }
                    }
                    else {
                        this.ManageOneButtonPress(ev.Button, ev.X / Zoom, ev.Y / Zoom);
                    }
                }
            }

            return base.OnButtonPressEvent(ev);
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton ev)
        {
            if(_nextPetriNet != null) {
                CurrentPetriNet = _nextPetriNet;
                this.Redraw();
            }
            else {
                this.ManageButtonRelease(ev.Button, ev.X / Zoom, ev.Y / Zoom);
            }
            return base.OnButtonReleaseEvent(ev);
        }

        protected override bool OnMotionNotifyEvent(Gdk.EventMotion ev)
        {
            _nextPetriNet = null;
            this.ManageMotion(ev.X / Zoom, ev.Y / Zoom);
            return base.OnMotionNotifyEvent(ev);
        }

        public void KeyPress(Gdk.EventKey ev)
        {
            this.OnKeyPressEvent(ev);
        }

        protected abstract EntityDraw EntityDraw {
            get;
            set;
        }

        protected override bool OnExposeEvent(Gdk.EventExpose ev)
        {
            base.OnExposeEvent(ev);

            using(Cairo.Context context = Gdk.CairoHelper.Create(this.GdkWindow)) {
                context.Scale(this.Zoom, this.Zoom);
                this.RenderInternal(context, CurrentPetriNet);
            }

            return true;
        }

        protected void RenderInternal(Context context, PetriNet petriNet)
        {
            _needsRedraw = false;

            var scrolled = _document.Window.Gui.ScrolledWindow;

            var extents = new PointD();
            extents.X = Math.Max(petriNet.Size.X, Allocation.Size.Width / Zoom);
            extents.Y = Math.Max(petriNet.Size.Y, Allocation.Size.Height / Zoom);

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
                context.Rectangle((scrolled.Hadjustment.Value + 10) / Zoom, (scrolled.Vadjustment.Value + 10) / Zoom, (ext.Width + 10) / Zoom, (ext.Height + 10) / Zoom);
                context.Fill();

                context.MoveTo((scrolled.Hadjustment.Value + 15 - ext.XBearing) / Zoom, (scrolled.Vadjustment.Value + 15 - ext.YBearing) / Zoom);

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

            minX += scrolled.Hadjustment.PageSize / 2;
            minY += scrolled.Vadjustment.PageSize / 2;

            int prevX, prevY;
            this.GetSizeRequest(out prevX, out prevY);
            this.SetSizeRequest((int)minX, (int)minY);
            petriNet.Size = new PointD(minX, minY);
            if(Math.Abs(minX - prevX) > 10 || Math.Abs(minY - prevY) > 10)
                this.RenderInternal(context, petriNet);
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
                return _editedPetriNet;
            }
            set {
                _document.EditorController.EditedObject = null;
                _editedPetriNet = value;
                _parentHierarchy.Clear();
                _nextPetriNet = null;
                this.Redraw();
            }
        }

        public static double Norm(PointD vec)
        {
            return Math.Sqrt(Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2));
        }

        public static double Norm(double x, double y)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        public static PointD Normalized(PointD vec)
        {
            double norm = PetriView.Norm(vec);
            if(norm < 1e-3) {
                return new PointD(0, 0);
            }

            return new PointD(vec.X / norm, vec.Y / norm);
        }

        public static PointD Normalized(double x, double y)
        {
            return PetriView.Normalized(new PointD(x, y));
        }

        private struct ParentStruct
        {
            public PetriNet petriNet;
            public TextExtents extents;
        }

        protected Document _document;

        protected PetriNet _editedPetriNet;
        bool _needsRedraw;

        protected PointD _deltaClick;
        protected PointD _originalPosition;

        PointD _lastClickPosition;
        System.DateTime _lastClickDate;

        private PetriNet _nextPetriNet;
        private double pathSeparatorLenth;
        private List<ParentStruct> _parentHierarchy = new List<ParentStruct>();
    }
}
