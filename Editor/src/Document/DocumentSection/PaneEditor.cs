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

namespace Petri
{
    public abstract class PaneEditor
    {
        protected PaneEditor(Document doc, Fixed view)
        {
            _document = doc;
            _view = view;
        }

        protected ComboBox ComboHelper(string selectedItem, List<string> items)
        {
            var funcList = ComboBox.NewText();

            foreach(string func in items) {
                funcList.AppendText(func);
            }

            {
                TreeIter iter;
                funcList.Model.GetIterFirst(out iter);
                do {
                    GLib.Value thisRow = new GLib.Value();
                    funcList.Model.GetValue(iter, 0, ref thisRow);
                    if((thisRow.Val as string).Equals(selectedItem)) {
                        funcList.SetActiveIter(iter);
                        break;
                    }
                } while(funcList.Model.IterNext(ref iter));
            }

            return funcList;
        }

        protected Label CreateLabel(int indentation, string text)
        {
            var label = new Label(text);
            this.AddWidget(label, false, indentation);

            return label;
        }

        protected WidgetType CreateWidget<WidgetType>(bool resizeable, int indentation, params object[] widgetConstructionArgs) where WidgetType : Widget
        {
            WidgetType w = (WidgetType)Activator.CreateInstance(typeof(WidgetType), widgetConstructionArgs);
            this.AddWidget(w, resizeable, indentation);
            return w;
        }

        protected void AddWidget(Widget w, bool resizeable, int indentation)
        {
            _objectList.Add(Tuple.Create(w, indentation, resizeable));
        }

        public void Resize(int width)
        {
            foreach(var tuple in _objectList) {
                if(tuple.Item3) {
                    tuple.Item1.WidthRequest = width - 20 - tuple.Item2;
                }
            }
        }

        protected void FormatAndShow()
        {
            foreach(Widget w in _view.AllChildren) {
                _view.Remove(w);
            }

            int lastX = 20, lastY = 0;
            for(int i = 0; i < _objectList.Count; ++i) {
                Widget w = _objectList[i].Item1;
                _view.Add(w);
                if(w is Label) {
                    lastY += 20;
                }
                else if(w is Button) {
                    lastY += 10;
                }
                else if(w is CheckButton) {
                    lastY += 10;
                }
                else if(w is ColorButton) {
                    lastY += 10;
                }

                Fixed.FixedChild w2 = ((Fixed.FixedChild)(_view[w]));
                w2.X = lastX + _objectList[i].Item2;
                w2.Y = lastY;

                lastY += w.Allocation.Height + 20;
                w.Show();
            }

            if(_document.Window.Gui != null) {
                _document.Window.Gui.BaseView.Redraw();
            }
        }

        protected Fixed _view;
        protected Document _document;
        protected List<Tuple<Widget, int, bool>> _objectList = new List<Tuple<Widget, int, bool>>();
    }
}

