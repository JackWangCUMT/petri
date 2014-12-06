using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	[System.ComponentModel.ToolboxItem(true)]
	public abstract class PetriView : Gtk.DrawingArea
	{
		public PetriView(Document doc) {
			document = doc;
			needsRedraw = false;
			deltaClick = new PointD(0, 0);
			originalPosition = new PointD();
			lastClickDate = DateTime.Now;
			lastClickPosition = new PointD(0, 0);

			this.ButtonPressEvent += (object o, ButtonPressEventArgs args) => {
				this.HasFocus = true;
			};
		}

		public void Redraw() {
			if(document.Window.Gui == null || document.Window.Gui.BaseView == this) {
				if(needsRedraw == false) {
					needsRedraw = true;
					this.QueueDraw();
				}
			}
		}

		public virtual void FocusIn() {
			this.Redraw();
		}

		public virtual void FocusOut() {
			this.Redraw();
		}

		protected virtual void ManageTwoButtonPress(Gdk.EventButton ev) {}
		protected virtual void ManageOneButtonPress(Gdk.EventButton ev) {}

		protected override bool OnButtonPressEvent(Gdk.EventButton ev) {
			if(ev.Type == Gdk.EventType.ButtonPress) {
				// The Windows version of GTK# currently doesn't detect TwoButtonPress events, so here is a lame simulation of it.
				if(/*ev.Type == Gdk.EventType.TwoButtonPress || */(lastClickPosition.X == ev.X && lastClickPosition.Y == ev.Y && (DateTime.Now - lastClickDate).TotalMilliseconds < 500)) {
					lastClickPosition.X = -12345;

					this.ManageTwoButtonPress(ev);
				}
				else {
					lastClickDate = DateTime.Now;
					lastClickPosition.X = ev.X;
					lastClickPosition.Y = ev.Y;

					this.ManageOneButtonPress(ev);
				}
			}

			this.Redraw();

			return base.OnButtonPressEvent(ev);
		}
			
		public void KeyPress(Gdk.EventKey ev)
		{
			this.OnKeyPressEvent(ev);
		}

		protected abstract EntityDraw EntityDraw {
			get;
			set;
		}

		protected override bool OnExposeEvent(Gdk.EventExpose ev)
		{
			base.OnExposeEvent(ev);
			needsRedraw = false;

			double minX = 0, minY = 0;

			using(Cairo.Context context = Gdk.CairoHelper.Create(this.GdkWindow)) {
				context.LineWidth = 4;
				context.MoveTo(0, 0);
				context.LineTo(this.Allocation.Width, 0);
				context.LineTo(this.Allocation.Width, this.Allocation.Height);
				context.LineTo(0, this.Allocation.Height);
				context.LineTo(0, 0);
				context.SetSourceRGBA(1, 1, 1, 1);
				context.Fill();

				{
					context.SetSourceRGBA(0.0, 0.6, 0.2, 1);
					context.SelectFontFace("Lucida Grande", FontSlant.Normal, FontWeight.Normal);
					context.SetFontSize(16);
					string val = CurrentPetriNet.Name;
					TextExtents te = context.TextExtents(val);
					context.MoveTo(15 - te.XBearing, 15 - te.YBearing);
					context.TextPath(val);
					context.Fill();
				}

				foreach(var t in CurrentPetriNet.Transitions) {
					if(t.Position.X > minX)
						minX = t.Position.X;
					if(t.Position.Y > minY)
						minY = t.Position.Y;

					this.EntityDraw.Draw(t, context);
				}

				foreach(var s in CurrentPetriNet.States) {
					if(s.Position.X > minX)
						minX = s.Position.X;
					if(s.Position.Y > minY)
						minY = s.Position.Y;

					this.EntityDraw.Draw(s, context);
				}

				this.SpecializedDrawing(context);

				context.LineWidth = 4;
				context.MoveTo(0, 0);
				context.LineTo(this.Allocation.Width, 0);
				context.LineTo(this.Allocation.Width, this.Allocation.Height);
				context.LineTo(0, this.Allocation.Height);
				context.LineTo(0, 0);
				context.SetSourceRGBA(0.7, 0.7, 0.7, 1);
				context.Stroke();
			}

			minX += 50;
			minY += 50;

			int prevX, prevY;
			this.GetSizeRequest(out prevX, out prevY);
			this.SetSizeRequest((int)minX, (int)minY);
			if(Math.Abs(minX - prevX) > 10 || Math.Abs(minY - prevY) > 10)
				this.Redraw();

			return true;
		}

		protected abstract void SpecializedDrawing(Cairo.Context context);

		public RootPetriNet RootPetriNet {
			get {
				return document.PetriNet;
			}
		}

		public virtual PetriNet CurrentPetriNet {
			get {
				return editedPetriNet;
			}
			set {
				document.EditorController.EditedObject = null;
				editedPetriNet = value;
			}
		}

		public static double Norm(PointD vec)
		{
			return Math.Sqrt(Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2));
		}

		public static double Norm(double x, double y)
		{
			return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
		}

		public static PointD Normalized(PointD vec)
		{
			double norm = PetriView.Norm(vec);
			if(norm < 1e-3) {
				return new PointD(0, 0);
			}

			return new PointD(vec.X / norm, vec.Y / norm);
		}

		public static PointD Normalized(double x, double y)
		{
			return PetriView.Normalized(new PointD(x, y));
		}

		protected Document document;

		protected PetriNet editedPetriNet;
		bool needsRedraw;

		protected PointD deltaClick;
		protected PointD originalPosition;

		PointD lastClickPosition;
		System.DateTime lastClickDate;
	}
}
