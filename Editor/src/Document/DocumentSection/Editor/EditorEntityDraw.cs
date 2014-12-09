using System;
using Cairo;

namespace Petri
{
	public class EditorEntityDraw : EntityDraw
	{
		public EditorEntityDraw(EditorView editor) {
			_editor = editor;
		}

		protected override void InitContextForBorder(Comment c, Context context) {
			base.InitContextForBorder(c, context);
			if(_editor.EntitySelected(c)) {
				context.LineWidth = 2;
			}
			context.SetSourceRGBA(0.8, 0.6, 0.4, 1);
		}

		protected override void InitContextForBorder(State s, Context context) {
			Color color = new Color(0, 0, 0, 1);
			double lineWidth = 3;

			if(_editor.EntitySelected(s)) {
				color.R = 1;
			}
			context.LineWidth = lineWidth;
			context.SetSourceRGBA(color.R, color.G, color.B, color.A);

			if(s == _editor.HoveredItem && _editor.CurrentAction == EditorView.EditorAction.CreatingTransition) {
				lineWidth += 2;
			}

			context.LineWidth = lineWidth;
		}
		protected override void InitContextForName(State s, Context context) {
			base.InitContextForName(s, context);
			if(_editor.EntitySelected(s)) {
				context.SetSourceRGBA(1, 0, 0, 1);
			}

		}

		protected override double GetArrowScale(Transition t) {
			if(_editor.EntitySelected(t)) {
				return 18;
			}
			else {
				return base.GetArrowScale(t);
			}
		}
		protected override void InitContextForLine(Transition t, Context context) {
			Color c = new Color(0.1, 0.6, 1, 1);
			double lineWidth = 2;

			if(_editor.EntitySelected(t)) {
				c.R = 0.3;
				c.G = 0.8;
				lineWidth += 2;
			}
			context.SetSourceRGBA(c.R, c.G, c.B, c.A);
			context.LineWidth = lineWidth;
		}


		private EditorView _editor;
	}
}

