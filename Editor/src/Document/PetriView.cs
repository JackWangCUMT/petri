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

		public void Redraw()
		{
			if(needsRedraw == false) {
				needsRedraw = true;
				this.QueueDraw();
			}
		}

		public virtual void FocusIn() {
			this.Redraw();
		}

		public virtual void FocusOut() {
			this.Redraw();
		}

		protected virtual void ManageTwoButtonPress(Gdk.EventButton ev) {

		}

		protected virtual void ManageOneButtonPress(Gdk.EventButton ev) {

		}

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

		protected virtual void UpdateContextToEntity(Cairo.Context context, Entity e, ref double arrowScale) {
			if(e is Transition) {
				Color c = new Color(0.1, 0.6, 1, 1);
				double lineWidth = 2;

				context.SetSourceRGBA(c.R, c.G, c.B, c.A);
				context.LineWidth = lineWidth;
			}
			else if(e is Action) {
				Color color = new Color(0, 0, 0, 1);
				double lineWidth = 3;

				context.LineWidth = lineWidth;
				context.SetSourceRGBA(color.R, color.G, color.B, color.A);

				context.Save();

				context.LineWidth = lineWidth;
			}
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

					double arrowScale = 12;
					this.UpdateContextToEntity(context, t, ref arrowScale);

					context.Save();

					PointD direction = new PointD(t.After.Position.X - t.Position.X, t.After.Position.Y - t.Position.Y);

					double radB = t.Before.Radius;
					double radA = t.After.Radius;

					if(PetriView.Norm(direction) > radB) {
						direction = PetriView.Normalized(direction);
						PointD destination = new PointD(t.After.Position.X - direction.X * radA, t.After.Position.Y - direction.Y * radA);

						direction = PetriView.Normalized(t.Position.X - t.Before.Position.X, t.Position.Y - t.Before.Position.Y);
						PointD origin = new PointD(t.Before.Position.X + direction.X * radB, t.Before.Position.Y + direction.Y * radB);

						context.MoveTo(origin);
						
						PointD c1 = new PointD(t.Position.X, t.Position.Y);
						PointD c2 = new PointD(t.Position.X, t.Position.Y);

						PointD direction2 = new PointD(destination.X - t.Position.X, destination.Y - t.Position.Y);
						direction2 = PetriView.Normalized(direction2);

						context.CurveTo(c1, c2, new PointD(destination.X - 0.99 * direction2.X * arrowScale, destination.Y - 0.99 * direction2.Y * arrowScale));

						context.Stroke();

						direction = PetriView.Normalized(destination.X - t.Position.X, destination.Y - t.Position.Y);
						PetriView.DrawArrow(context, direction, destination, arrowScale);
					}

					PointD point = new PointD(t.Position.X, t.Position.Y);
					point.X -= t.Width / 2 + context.LineWidth / 2;
					point.Y -= t.Height / 2;
					context.MoveTo(point);
					point.X += t.Width;
					context.LineTo(point);
					point.Y += t.Height;
					context.LineTo(point);
					point.X -= t.Width;
					context.LineTo(point);
					point.Y -= t.Height + context.LineWidth / 2;
					context.LineTo(point);

					context.StrokePreserve();

					context.SetSourceRGBA(1, 1, 1, 1);
					context.Fill();

					context.Restore();

					context.SelectFontFace("Lucida Grande", FontSlant.Normal, FontWeight.Normal);
					context.SetFontSize(12);
					string val = t.Name.ToString();
					TextExtents te = context.TextExtents(val);
					context.MoveTo(t.Position.X - te.Width / 2 - te.XBearing, t.Position.Y - te.Height / 2 - te.YBearing);
					context.TextPath(val);
					context.Fill();
				}

				foreach(var a in CurrentPetriNet.States) {
					if(a.Position.X > minX)
						minX = a.Position.X;
					if(a.Position.Y > minY)
						minY = a.Position.Y;

					double dummy = 0;
					this.UpdateContextToEntity(context, a, ref dummy);

					context.Arc(a.Position.X, a.Position.Y, a.Radius, 0, 2 * Math.PI);

					context.StrokePreserve();

					context.SetSourceRGBA(1, 1, 1, 1);
					context.FillPreserve();

					context.Restore();

					if(a.Active) {
						context.MoveTo(a.Position.X + a.Radius - 5, a.Position.Y);
						context.Arc(a.Position.X, a.Position.Y, a.Radius - 5, 0, 2 * Math.PI);
					}

					context.Stroke();

					int tokenShift = a.TransitionsBefore.Count > 0 ? -3 : 0;

					context.SelectFontFace("Lucida Grande", FontSlant.Normal, FontWeight.Normal);
					context.SetFontSize(12);
					string val = a.Name;
					TextExtents te = context.TextExtents(val);
					context.MoveTo(a.Position.X - te.Width / 2 - te.XBearing, a.Position.Y - te.Height / 2 - te.YBearing + tokenShift);
					context.TextPath(val);
					context.Fill();

					if(a.TransitionsBefore.Count > 0) {
						context.SetFontSize(8);
						string tokNum = a.RequiredTokens.ToString() + " tok";
						te = context.TextExtents(tokNum);
						context.MoveTo(a.Position.X - te.Width / 2 - te.XBearing, a.Position.Y - te.Height / 2 - te.YBearing + 5);
						context.TextPath(tokNum);
						context.Fill();
					}
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

		protected static void DrawArrow(Context context, PointD direction, PointD position, double scaleAlongAxis)
		{
			double angle = 20 * Math.PI / 180;

			double sin = Math.Sin(angle);
			PointD normal = new PointD(-direction.Y * sin, direction.X * sin);

			direction.X *= scaleAlongAxis;
			direction.Y *= scaleAlongAxis;

			normal.X *= scaleAlongAxis;
			normal.Y *= scaleAlongAxis;
			
			context.MoveTo(position);
			context.LineTo(position.X - direction.X + normal.X, position.Y - direction.Y + normal.Y);
			context.LineTo(position.X - direction.X - normal.X, position.Y - direction.Y - normal.Y);

			context.Fill();
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
