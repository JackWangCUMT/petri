using System;
using System.Collections.Generic;
using Gtk;
using Cairo;
using System.Linq;

namespace Petri
{
	public class DebugView : PetriView
	{

		public DebugView(Document doc) : base(doc) {
		
		}

		public override void FocusIn() {
			base.FocusIn();
		}

		public override void FocusOut() {
			base.FocusOut();
		}

		protected override void ManageTwoButtonPress(Gdk.EventButton ev) {
			if(ev.Button == 1) {
				var entity = CurrentPetriNet.StateAtPosition(new PointD(ev.X, ev.Y));
				if(entity is InnerPetriNet) {
					this.CurrentPetriNet = entity as InnerPetriNet;
				}
			}
		}

		protected override void ManageOneButtonPress(Gdk.EventButton ev) {
			if(ev.Button == 1) {

			}
			else if(ev.Button == 3) {
			}
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton ev) {
			return base.OnButtonReleaseEvent(ev);
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion ev) {
			return base.OnMotionNotifyEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyPressEvent(Gdk.EventKey ev) {
			if(ev.Key == Gdk.Key.Escape) {
				if(this.CurrentPetriNet.Parent != null) {
					this.CurrentPetriNet = this.CurrentPetriNet.Parent;
				}
				this.Redraw();
			}

			return base.OnKeyPressEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyReleaseEvent(Gdk.EventKey ev) {
			return base.OnKeyReleaseEvent(ev);
		}
		protected override void UpdateContextToEntity(Cairo.Context context, Entity e, ref double arrowScale) {
			if(e is Transition) {
				Color c = new Color(0.1, 0.6, 1, 1);
				double lineWidth = 2;

				/*if(EntitySelected(e)) {
					c.R = 0.3;
					c.G = 0.8;
					lineWidth += 2;
					arrowScale = 18;
				}*/
				context.SetSourceRGBA(c.R, c.G, c.B, c.A);
				context.LineWidth = lineWidth;
			}
			else if(e is State) {
				Color color = new Color(0, 0, 0, 1);
				double lineWidth = 3;

				int enableCount;
				if(document.DebugController.ActiveStates.TryGetValue(e as State, out enableCount) == true && enableCount > 0) {
					color.R = 1;
				}
				else if(e is InnerPetriNet) {
					foreach(var s in document.DebugController.ActiveStates) {
						if((e as InnerPetriNet).ContainsEntity(s.Key.ID)) {
							color.R = 1;
							color.G = 0;
							color.B = 1;
							break;
						}
					}
				}

				context.LineWidth = lineWidth;
				context.SetSourceRGBA(color.R, color.G, color.B, color.A);

				context.Save();

				/*if(e == hoveredItem && currentAction == CurrentAction.CreatingTransition) {
					lineWidth += 2;
				}*/

				context.LineWidth = lineWidth;
			}
		}

		protected override void SpecializedDrawing(Cairo.Context context) {
		}
	}
}

