using System;
using Cairo;

namespace Petri
{
	public class DebugEntityDraw : EntityDraw
	{
		public DebugEntityDraw(Document document) {
			_document = document;
		}

		protected override void InitContextForBorder(State s, Context context) {
			Color color = new Color(0, 0, 0, 1);
			double lineWidth = 3;

			int enableCount;
			if(_document.DebugController.ActiveStates.TryGetValue(s as State, out enableCount) == true && enableCount > 0) {
				color.R = 1;
			}
			else if(s is InnerPetriNet) {
				foreach(var a in _document.DebugController.ActiveStates) {
					if((s as InnerPetriNet).ContainsEntity(a.Key.ID)) {
						color.R = 1;
						color.G = 0;
						color.B = 1;
						break;
					}
				}
			}

			context.LineWidth = lineWidth;
			context.SetSourceRGBA(color.R, color.G, color.B, color.A);

			context.LineWidth = lineWidth;
		}

		protected override void InitContextForLine(Transition t, Context context) {
			Color c = new Color(0.1, 0.6, 1, 1);
			double lineWidth = 2;

			context.SetSourceRGBA(c.R, c.G, c.B, c.A);
			context.LineWidth = lineWidth;
		}

		private Document _document;
	}
}

