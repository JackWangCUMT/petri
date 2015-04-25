using System;
using Gtk;

namespace Petri
{
	public class DebugEditor : PaneEditor {
		public DebugEditor(Document doc) : base(doc, doc.Window.DebugGui.Editor) {
			CreateLabel(0, "Évaluer l'expression :");
			Entry entry = CreateWidget<Entry>(true, 20, "Expression");
			Evaluate = CreateWidget<Button>(false, 20, "Évaluer");

			CreateLabel(0, "Résultat :");

			_buf = new TextBuffer(new TextTagTable());
			_buf.Text = "";
			var result = CreateWidget<TextView>(true, 0, _buf);
			result.Editable = false;
			result.SetSizeRequest(200, 400);
			result.WrapMode = WrapMode.Word;

			Evaluate.Clicked += (sender, ev) => {
				if(!_document.DebugController.Server.PetriRunning || _document.DebugController.Server.Pause) {
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

		private TextBuffer _buf;
	}
}

