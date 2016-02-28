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
using System.Collections.Generic;
using Gtk;
using Cairo;
using System.Linq;

namespace Petri.Editor.GUI.Debugger
{
    public class DebugView : PetriView
    {
        public DebugView(Document doc) : base(doc)
        {
            this.EntityDraw = new DebugEntityDraw(Document);
            DrawingArea.KeyPressEvent += OnKeyPressEvent;
        }

        public Entity SelectedEntity {
            get {
                return _selection;
            }
            set {
                _selection = value;
                Document.DebugController.DebugEditor = new DebugEditor(Document, value);
            }
        }

        Document Document {
            get {
                return (Document)_document;
            }
        }

        protected override void ManageTwoButtonPress(uint button, double x, double y)
        {
            if(button == 1) {
                var entity = CurrentPetriNet.StateAtPosition(new PointD(x, y));
                if(entity is InnerPetriNet) {
                    this.CurrentPetriNet = entity as InnerPetriNet;
                    this.Redraw();
                }
                else if(entity is Action) {
                    var a = entity as Action;
                    if(Document.DebugController.Breakpoints.Contains(a)) {
                        Document.DebugController.RemoveBreakpoint(a);
                    }
                    else {
                        Document.DebugController.AddBreakpoint(a);
                    }

                    SelectedEntity = entity;

                    this.Redraw();
                }
            }
        }

        protected override void ManageOneButtonPress(uint button, double x, double y)
        {
            if(button == 1) {
                _deltaClick.X = x;
                _deltaClick.Y = y;

                Entity hovered = CurrentPetriNet.StateAtPosition(_deltaClick);

                if(hovered == null) {
                    hovered = CurrentPetriNet.TransitionAtPosition(_deltaClick);

                    if(hovered == null) {
                        hovered = CurrentPetriNet.CommentAtPosition(_deltaClick);
                    }
                }

                SelectedEntity = hovered;
                Redraw();
            }
            else if(button == 3) {

            }
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs ev)
        {
            if(ev.Event.Key == Gdk.Key.Escape) {
                if(this.CurrentPetriNet.Parent != null) {
                    this.CurrentPetriNet = this.CurrentPetriNet.Parent;
                }
                this.Redraw();
            }

            //return base.OnKeyPressEvent(ev);
        }

        protected override EntityDraw EntityDraw {
            get;
            set;
        }

        Entity _selection;
    }
}

