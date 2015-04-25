using System;
using Gtk;

namespace Petri
{
	public class DebugEditor : PaneEditor {
		public DebugEditor(Document doc, Entity selected) : base(doc, doc.Window.DebugGui.Editor) {
			if(selected != null) {
				var label = CreateLabel(0, "ID de l'entité : " + selected.ID.ToString());
				label.Markup = "<span color=\"grey\">" + label.Text + "</span>";
			}
			if(selected is Transition) {
				CreateLabel(0, "Condition de la transition :");
				Entry e = CreateWidget<Entry>(true, 0, ((Transition)selected).Condition.MakeUserReadable());
				e.IsEditable = false;
			}
			else if(selected is Action) {
				CreateLabel(0, "Action de l'état :");
				Entry ee = CreateWidget<Entry>(true, 0, ((Action)selected).Function.MakeUserReadable());
				ee.IsEditable = false;

				var active = CreateWidget<CheckButton>(false, 0, "Point d'arrêt sur l'état");
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

			CreateLabel(0, "Évaluer l'expression :");
			Entry entry = CreateWidget<Entry>(true, 0, "Expression");
			Evaluate = CreateWidget<Button>(false, 0, "Évaluer");
			Evaluate.Sensitive = _document.DebugController != null &&_document.DebugController.Server.SessionRunning && (!_document.DebugController.Server.PetriRunning || _document.DebugController.Server.Pause);

			CreateLabel(0, "Résultat :");

			_buf = new TextBuffer(new TextTagTable());
			_buf.Text = "";
			var result = CreateWidget<TextView>(true, 0, _buf);
			result.Editable = false;
			result.WrapMode = WrapMode.Word;

			Evaluate.Clicked += (sender, ev) => {
				if(_document.DebugController.Server.SessionRunning && (!_document.DebugController.Server.PetriRunning || _document.DebugController.Server.Pause)) {
					string str = entry.Text;
					try {
						Cpp.Expression expr = Cpp.Expression.CreateFromString<Cpp.Expression>(str, null, _document.AllFunctions, _document.CppMacros);
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

