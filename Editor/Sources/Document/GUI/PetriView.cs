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

namespace Petri.Editor.GUI
{
    public abstract class PetriView : Petri.Editor.PetriView
    {
        public PetriView(Document document) : base(document)
        {
            DrawingArea = new DrawingArea();
            DrawingArea.ButtonPressEvent += OnButtonPressEvent;
            DrawingArea.ButtonReleaseEvent += OnButtonReleaseEvent;
            DrawingArea.MotionNotifyEvent += OnMotionNotifyEvent;
            DrawingArea.ExposeEvent += OnExposeEvent;

            DrawingArea.CanFocus = true;
            DrawingArea.CanDefault = true;
            DrawingArea.AddEvents((int)
                                        (Gdk.EventMask.ButtonPressMask
            | Gdk.EventMask.ButtonReleaseMask
            | Gdk.EventMask.KeyPressMask
            | Gdk.EventMask.PointerMotionMask));
            
            _deltaClick = new PointD(0, 0);
            _originalPosition = new PointD();
            _lastClickDate = DateTime.Now;
            _lastClickPosition = new PointD(0, 0);
        }

        public Gtk.DrawingArea DrawingArea {
            get;
            private set;
        }

        public void Redraw()
        {
            if(Document.Window.Gui == null || Document.Window.Gui.BaseView == this) {
                if(_needsRedraw == false) {
                    _needsRedraw = true;
                    DrawingArea.QueueDraw();
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

        Document Document {
            get {
                return (Document)_document;
            }
        }

        public override PetriNet CurrentPetriNet {
            get {
                return base.CurrentPetriNet;
            }
            set {
                base.CurrentPetriNet = value;

                Document.EditorController.EditedObject = null;
                _nextPetriNet = null;
                this.Redraw();
            }
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

        protected void OnButtonPressEvent(object o, ButtonPressEventArgs ev)
        {
            DrawingArea.HasFocus = true;

            if(ev.Event.Type == Gdk.EventType.ButtonPress) {
                // The Windows version of GTK# currently doesn't detect TwoButtonPress events, so here is a lame simulation of it.
                if(/*ev.Type == Gdk.EventType.TwoButtonPress || */(_lastClickPosition.X == ev.Event.X && _lastClickPosition.Y == ev.Event.Y && (DateTime.Now - _lastClickDate).TotalMilliseconds < 500)) {
                    _lastClickPosition.X = -12345;

                    this.ManageTwoButtonPress(ev.Event.Button,
                                              ev.Event.X / Zoom,
                                              ev.Event.Y / Zoom);
                }
                else {
                    _lastClickDate = DateTime.Now;
                    _lastClickPosition.X = ev.Event.X;
                    _lastClickPosition.Y = ev.Event.Y;

                    var scrolled = Document.Window.Gui.ScrolledWindow;

                    if(ev.Event.X >= 15 + scrolled.Hadjustment.Value && ev.Event.X < _parentHierarchy[_parentHierarchy.Count - 1].extents.Width + 15 + scrolled.Hadjustment.Value
                       && ev.Event.Y >= 15 + scrolled.Vadjustment.Value && ev.Event.Y < _parentHierarchy[_parentHierarchy.Count - 1].extents.Height + 15 + scrolled.Vadjustment.Value) {
                        double currX = 15;
                        foreach(var item in _parentHierarchy) {
                            if(item.petriNet != null && ev.Event.X - currX < item.extents.Width + pathSeparatorLenth + scrolled.Hadjustment.Value) {
                                _nextPetriNet = item.petriNet;
                                break;
                            }
                            currX += item.extents.Width + pathSeparatorLenth;
                        }
                    }
                    else {
                        this.ManageOneButtonPress(ev.Event.Button,
                                                  ev.Event.X / Zoom,
                                                  ev.Event.Y / Zoom);
                    }
                }
            }
        }

        protected void OnButtonReleaseEvent(object o, ButtonReleaseEventArgs ev)
        {
            if(_nextPetriNet != null) {
                CurrentPetriNet = _nextPetriNet;
                this.Redraw();
            }
            else {
                this.ManageButtonRelease(ev.Event.Button, ev.Event.X / Zoom, ev.Event.Y / Zoom);
            }
        }

        protected void ResetDoubleClick()
        {
            _lastClickPosition.X = Double.MinValue;
        }

        protected void OnMotionNotifyEvent(object o, MotionNotifyEventArgs ev)
        {
            _nextPetriNet = null;
            this.ManageMotion(ev.Event.X / Zoom, ev.Event.Y / Zoom);
        }

        protected override PointD PathPosition {
            get {
                return new PointD(Document.Window.Gui.ScrolledWindow.Hadjustment.Value,
                                  Document.Window.Gui.ScrolledWindow.Vadjustment.Value);
            }
        }

        protected override PointD GetExtents(PetriNet petriNet)
        {
            var extents = new PointD();
            extents.X = Math.Max(petriNet.Size.X, DrawingArea.Allocation.Size.Width / Zoom);
            extents.Y = Math.Max(petriNet.Size.Y, DrawingArea.Allocation.Size.Height / Zoom);

            return extents;
        }


        protected override PointD RenderInternal(Context context, PetriNet petriNet)
        {
            var scrolled = Document.Window.Gui.ScrolledWindow;

            var min = base.RenderInternal(context, petriNet);

            min.X += scrolled.Hadjustment.PageSize / 2;
            min.Y += scrolled.Vadjustment.PageSize / 2;

            int prevX, prevY;
            DrawingArea.GetSizeRequest(out prevX, out prevY);
            DrawingArea.SetSizeRequest((int)min.X, (int)min.Y);
            petriNet.Size = new PointD(min.X, min.Y);
            if(Math.Abs(min.X - prevX) > 10 || Math.Abs(min.Y - prevY) > 10)
                this.RenderInternal(context, petriNet);

            _needsRedraw = false;

            return min;
        }

        protected void OnExposeEvent(object o, ExposeEventArgs ev)
        {
            using(Cairo.Context context = Gdk.CairoHelper.Create(DrawingArea.GdkWindow)) {
                context.Scale(this.Zoom, this.Zoom);
                this.RenderInternal(context, CurrentPetriNet);
            }
        }

        protected PointD _deltaClick;
        protected PointD _originalPosition;

        PointD _lastClickPosition;
        DateTime _lastClickDate;

        bool _needsRedraw;
        PetriNet _nextPetriNet;
    }
}

