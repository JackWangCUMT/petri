using System;
using System.Collections.Generic;
using Gtk;
using Cairo;
using System.Linq;

namespace Petri
{
	public class DebugView : PetriView {
		public DebugView(Document doc) : base(doc) {
			this.EntityDraw = new DebugEntityDraw(_document);
		}

		protected override void ManageTwoButtonPress(uint button, double x, double y) {
			if(button == 1) {
				var entity = CurrentPetriNet.StateAtPosition(new PointD(x, y));
				if(entity is InnerPetriNet) {
					this.CurrentPetriNet = entity as InnerPetriNet;
					this.Redraw();
				}
				else if(entity is Action) {
					var a = entity as Action;
					if(_document.DebugController.Breakpoints.Contains(a)) {
						_document.DebugController.RemoveBreakpoint(a);
					}
					else {
						_document.DebugController.AddBreakpoint(a);
					}

					this.Redraw();
				}
			}
		}

		protected override void ManageOneButtonPress(uint button, double x, double y) {
			if(button == 1) {

			}
			else if(button == 3) {

			}
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
	}
}

