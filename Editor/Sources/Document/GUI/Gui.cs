﻿/*
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

namespace Petri.Editor.GUI
{
    public abstract class Gui : VBox
    {
        public Gui()
        {
            _status = new Label();
            var hbox = new HBox(false, 0);
            hbox.PackStart(_status, false, true, 5);
            PackEnd(hbox, false, true, 5);
        }

        public abstract void FocusIn();

        public abstract void FocusOut();

        public abstract void Redraw();

        public abstract void UpdateToolbar();

        public abstract ScrolledWindow ScrolledWindow {
            get;
        }

        public abstract PetriView BaseView {
            get;
        }

        public abstract Fixed Editor {
            get;
        }

        public HPaned Paned {
            get {
                return _paned;
            }
        }

        public string Status {
            get {
                return _status.Text;
            }
            set {
                _status.Text = value;
            }
        }

        protected HPaned _paned;
        protected Label _status;
    }
}
