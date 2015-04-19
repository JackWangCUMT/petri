using System;
using Cairo;

namespace Petri
{
	public class DebugEntityDraw : EntityDraw
	{
		public DebugEntityDraw(Document document) {
			_document = document;
		}

		protected override void InitContextForBackground(State s, Context context) {
			Color color = new Color(1, 1, 1, 1);

			int enableCount;
			lock(_document.DebugController.ActiveStates) {
				if(_document.DebugController.ActiveStates.TryGetValue(s as State, out enableCount) == true && enableCount > 0) {
					if(_document.DebugController.Server.Pause) {
						color.R = 0.4;
						color.G = 0.7;
						color.B = 0.4;
					}
					else {
						color.R = 0.6;
						color.G = 1;
						color.B = 0.6;
					}
				}
				else if(s is InnerPetriNet) {
					foreach(var a in _document.DebugController.ActiveStates) {
						if((s as InnerPetriNet).EntityFromID(a.Key.ID) != null) {
							if(_document.DebugController.Server.Pause) {
								color.R = 0.7;
								color.G = 0.4;
								color.B = 0.7;
							}
							else {
								color.R = 1;
								color.G = 0.7;
								color.B = 1;
							}
							break;
						}
					}
				}
			}

			context.SetSourceRGBA(color.R, color.G, color.B, color.A);
		}

		protected override void InitContextForBorder(State s, Context context) {
			base.InitContextForBorder(s, context);

			if(s is Action) {
				if(_document.DebugController.Breakpoints.Contains(s as Action)) {
					context.SetSourceRGBA(1, 0, 0, 1);
					context.LineWidth = 4;
				}
			}
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

