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

namespace Petri.Editor.Debugger
{
    public abstract class DebugController : Controller
    {
        public DebugController(HeadlessDocument doc, DebugClient client)
        {
            Document = doc;
            Client = client;
            ActiveStates = new Dictionary<State, int>();
            Breakpoints = new HashSet<Action>();
        }

        protected HeadlessDocument Document {
            get;
            set;
        }

        public DebugClient Client {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the last evaluated expressions of the debug editor.
        /// </summary>
        /// <value>The last evaluation.</value>
        public List<string> LastEvaluations {
            get;
        } = new List<string>();

        /// <summary>
        /// Gets the active states.
        /// </summary>
        /// <value>The currently active states of a running petri net.</value>
        public Dictionary<State, int> ActiveStates {
            get;
            private set;
        }

        /// <summary>
        /// The list of breakpoints installed in the currently running petri net.
        /// </summary>
        /// <value>The breakpoints.</value>
        public HashSet<Action> Breakpoints {
            get;
            private set;
        }

        /// <summary>
        /// Attach a breakpoint to the specified petri net state.
        /// </summary>
        /// <param name="action">The state.</param>
        public void AddBreakpoint(Action action)
        {
            Breakpoints.Add(action);
            Client.UpdateBreakpoints();
        }

        /// <summary>
        /// Remove a breakpoint from the specified petri net state.
        /// </summary>
        /// <param name="action">The state.</param>
        public void RemoveBreakpoint(Action action)
        {
            Breakpoints.Remove(action);
            Client.UpdateBreakpoints();
        }

        public override void UpdateMenuItems()
        {
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

