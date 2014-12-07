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
			this.EntityDraw = new DebugEntityDraw(document);
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
				else if(entity is Action) {
					var a = entity as Action;
					if(document.DebugController.Breakpoints.Contains(a)) {
						document.DebugController.RemoveBreakpoint(a);
					}
					else {
						document.DebugController.AddBreakpoint(a);
					}
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

		protected override EntityDraw EntityDraw {
			get;
			set;
		}
		
		protected override void SpecializedDrawing(Cairo.Context context) {

		}
	}
}

