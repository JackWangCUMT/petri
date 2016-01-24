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

namespace Petri.Editor
{
    public class Find
    {
        public enum FindType
        {
            All,
            Action,
            Transition,
            Comment,
        }

        public void Show()
        {
            _window.Title = Configuration.GetLocalized("Find in the document {0}",
                                                       _document.Window.Title);
            _window.ShowAll();
            _window.Present();
            _document.AssociatedWindows.Add(_window);
            _what.GrabFocus();
        }

        public void Hide()
        {
            _document.AssociatedWindows.Remove(_window);
            _window.Hide();
        }

        protected void OnFind(object sender, EventArgs e)
        {
            _document.Window.EditorGui.PerformFind(_what.Text, Type);
        }

        protected void OnDeleteEvent(object sender, DeleteEventArgs a)
        {
            _window.Hide();
            // We do not close the window so that there is no need to recreate it upon reopening
            a.RetVal = true;
        }

        public FindType Type {
            get;
            protected set;
        }

        public Find(Document doc)
        {
            _document = doc;

            _window = new Window(WindowType.Toplevel);
            _window.Title = Configuration.GetLocalized("Find in the document");

            _window.DefaultWidth = 400;
            _window.DefaultHeight = 100;

            _window.SetPosition(WindowPosition.Center);
            int x, y;
            _window.GetPosition(out x, out y);
            _window.Move(x, 2 * y / 3);
            _window.BorderWidth = 15;

            var vbox = new VBox(false, 5);

            _window.Add(vbox);

            var hbox = new HBox();
            var label = new Label(Configuration.GetLocalized("Find among the entities of kind:"));
            hbox.PackStart(label, false, false, 0);
            vbox.PackStart(hbox, false, false, 0);

            ComboBox combo = ComboBox.NewText();

            combo.AppendText(Configuration.GetLocalized("All entities"));
            combo.AppendText(Configuration.GetLocalized("States"));
            combo.AppendText(Configuration.GetLocalized("Transitions"));
            combo.AppendText(Configuration.GetLocalized("Comments"));
            TreeIter iter;
            combo.Model.GetIterFirst(out iter);
            combo.SetActiveIter(iter);

            combo.Changed += (object sender, EventArgs e) => {
                TreeIter it;

                if(combo.GetActiveIter(out it)) {
                    Type = (FindType)int.Parse(combo.Model.GetStringFromIter(it));
                }
            };

            hbox = new HBox();
            hbox.PackStart(combo, false, false, 0);
            vbox.PackStart(hbox, false, false, 0);

            _what = new Entry();
            _what.Activated += OnFind;

            vbox.PackStart(_what, true, true, 0);

            hbox = new HBox(false, 5);
            var cancel = new Button(new Label(Configuration.GetLocalized("Cancel")));
            var find = new Button(new Label(Configuration.GetLocalized("Find")));
            cancel.Clicked += (sender, e) => {
                Hide();
            };
            find.Clicked += OnFind;

            hbox.PackStart(cancel, false, false, 0);
            hbox.PackStart(find, false, false, 0);
            vbox.PackStart(hbox, false, false, 0);

            _window.DeleteEvent += OnDeleteEvent;
        }

        Entry _what;
        Document _document;
        Window _window;
    }
}

