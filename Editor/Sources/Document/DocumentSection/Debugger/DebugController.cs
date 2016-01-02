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

namespace Petri.Editor
{
    public class DebugController : Controller
    {
        public DebugController(Document doc)
        {
            Document = doc;
            Client = new DebugClient(doc);
            ActiveStates = new Dictionary<State, int>();
            Breakpoints = new HashSet<Action>();
            DebugEditor = new DebugEditor(doc, null);
        }

        public Document Document {
            get;
            private set;
        }

        public DebugClient Client {
            get;
            private set;
        }

        public DebugEditor DebugEditor {
            get;
            set;
        }

        public Dictionary<State, int> ActiveStates {
            get;
            private set;
        }

        public HashSet<Action> Breakpoints {
            get;
            private set;
        }

        public void AddBreakpoint(Action a)
        {
            Breakpoints.Add(a);
            Client.UpdateBreakpoints();
        }

        public void RemoveBreakpoint(Action a)
        {
            Breakpoints.Remove(a);
            Client.UpdateBreakpoints();
        }

        public override void ManageFocus(object focus)
        {

        }

        public override void UpdateMenuItems()
        {
            Document.Window.EmbedItem.Sensitive = false;
        }

        public override void Copy()
        {

        }

        public override void Cut()
        {

        }

        public override void Paste()
        {

        }

        public override void SelectAll()
        {

        }
    }
}

