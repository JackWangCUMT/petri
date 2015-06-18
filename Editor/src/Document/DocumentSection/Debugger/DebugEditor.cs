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

namespace Petri
{
	public class DebugEditor : PaneEditor {
		public DebugEditor(Document doc, Entity selected) : base(doc, doc.Window.DebugGui.Editor) {
			if(selected != null) {
				var label = CreateLabel(0, Configuration.GetLocalized("Entity's ID:") + " " + selected.ID.ToString());
				label.Markup = "<span color=\"grey\">" + label.Text + "</span>";
			}
			if(selected is Transition) {
				CreateLabel(0, Configuration.GetLocalized("Transition's condition:"));
				Entry e = CreateWidget<Entry>(true, 0, ((Transition)selected).Condition.MakeUserReadable());
				e.IsEditable = false;
			}
			else if(selected is Action) {
				CreateLabel(0, Configuration.GetLocalized("State's action:"));
				Entry ee = CreateWidget<Entry>(true, 0, ((Action)selected).Function.MakeUserReadable());
				ee.IsEditable = false;

				var active = CreateWidget<CheckButton>(false, 0, Configuration.GetLocalized("Breakpoint on the state"));
				active.Active = _document.DebugController.Breakpoints.Contains((Action)selected);
				active.Toggled += (sender, e) => {
					if(_document.DebugController.Breakpoints.Contains((Action)selected)) {
						_document.DebugController.RemoveBreakpoint((Action)selected);
					}
					else {
						_document.DebugController.AddBreakpoint((Action)selected);
					}

					_document.Window.DebugGui.View.Redraw();
				};
			}

			CreateLabel(0, Configuration.GetLocalized("Evaluate expression:"));
			Entry entry = CreateWidget<Entry>(true, 0, Configuration.GetLocalized("Expression"));
			Evaluate = CreateWidget<Button>(false, 0, Configuration.GetLocalized("Evaluate"));
			Evaluate.Sensitive = _document.DebugController != null &&_document.DebugController.Server.SessionRunning && (!_document.DebugController.Server.PetriRunning || _document.DebugController.Server.Pause);

			CreateLabel(0, Configuration.GetLocalized("Result:"));

			_buf = new TextBuffer(new TextTagTable());
			_buf.Text = "";
			var result = CreateWidget<TextView>(true, 0, _buf);
			result.Editable = false;
			result.WrapMode = WrapMode.Word;

			Evaluate.Clicked += (sender, ev) => {
				if(_document.DebugController.Server.SessionRunning && (!_document.DebugController.Server.PetriRunning || _document.DebugController.Server.Pause)) {
					string str = entry.Text;
					try {
						Cpp.Expression expr = Cpp.Expression.CreateFromString<Cpp.Expression>(str, _document.PetriNet);
						_document.DebugController.Server.Evaluate(expr);
					}
					catch(Exception e) {
						_buf.Text = e.Message;
					}
				}
			};

			this.FormatAndShow();
		}

		public void OnEvaluate(string result) {
			_buf.Text = result;
		}

		public Button Evaluate {
			get;
			private set;
		}

		TextBuffer _buf;
	}
}

