using System;
using Cairo;

namespace Petri
{
	public abstract class EntityDraw
	{
		public EntityDraw() {}

		public void Draw(State e, Context context) {
			this.InitContextForBackground(e, context);
			this.DrawBackground(e, context);

			this.InitContextForBorder(e, context);
			this.DrawBorder(e, context);

			this.InitContextForName(e, context);
			this.DrawName(e, context);
			this.InitContextForTokens(e, context);
			this.DrawTokens(e, context);
		}

		public void Draw(Transition e, Context context) {
			this.InitContextForLine(e, context);
			this.DrawLine(e, context);

			this.InitContextForBorder(e, context);
			this.DrawBorder(e, context);

			this.InitContextForBackground(e, context);
			this.DrawBackground(e, context);

			this.InitContextForText(e, context);
			this.DrawText(e, context);
		}

		protected virtual void InitContextForBackground(State s, Context context) {
			context.SetSourceRGBA(1, 1, 1, 1);
		}
		protected virtual void DrawBackground(State s, Context context) {
			context.Arc(s.Position.X, s.Position.Y, s.Radius, 0, 2 * Math.PI);

			context.FillPreserve();
		}

		protected virtual void InitContextForBorder(State s, Context context) {
			context.LineWidth = 3;
			context.SetSourceRGBA(0, 0, 0, 1);
		}
		protected virtual void DrawBorder(State s, Context context) {
			if(s.Active) {
				context.StrokePreserve();

				context.MoveTo(s.Position.X + s.Radius - 5, s.Position.Y);
				context.Arc(s.Position.X, s.Position.Y, s.Radius - 5, 0, 2 * Math.PI);
			}

			context.Stroke();
		}

		protected virtual void InitContextForName(State s, Context context) {
			context.SetSourceRGBA(0, 0, 0, 1);
			context.SelectFontFace("Lucida Grande", FontSlant.Normal, FontWeight.Normal);
			context.SetFontSize(12);
		}
		protected virtual void DrawName(State s, Context context) {
			int tokenShift = s.TransitionsBefore.Count > 0 ? -3 : 0;

			string val = s.Name;
			TextExtents te = context.TextExtents(val);
			context.MoveTo(s.Position.X - te.Width / 2 - te.XBearing, s.Position.Y - te.Height / 2 - te.YBearing + tokenShift);
			context.TextPath(val);
			context.Fill();
		}

		protected virtual void InitContextForTokens(State s, Context context) {
			context.SetFontSize(8);
		}
		protected virtual void DrawTokens(State s, Context context) {
			if(s.TransitionsBefore.Count > 0) {
				string tokNum = s.RequiredTokens.ToString() + " tok";
				TextExtents te = context.TextExtents(tokNum);
				context.MoveTo(s.Position.X - te.Width / 2 - te.XBearing, s.Position.Y - te.Height / 2 - te.YBearing + 5);
				context.TextPath(tokNum);
				context.Fill();
			}
		}

		protected virtual double GetArrowScale(Transition t) {
			return 12;
		}
		protected virtual void InitContextForLine(Transition t, Context context) {
			context.SetSourceRGBA(0.1, 0.6, 1, 1);
			context.LineWidth = 2;
		}
		protected virtual void DrawLine(Transition t, Context context) {
			double arrowScale = this.GetArrowScale(t);

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
				EntityDraw.DrawArrow(context, direction, destination, arrowScale);
			}
		}

		protected virtual void InitContextForBorder(Transition t, Context context) {

		}
		protected virtual void DrawBorder(Transition t, Context context) {
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
		}

		protected virtual void InitContextForBackground(Transition t, Context context) {
			context.SetSourceRGBA(1, 1, 1, 1);
		}
		protected virtual void DrawBackground(Transition t, Context context) {
			context.Fill();
		}

		protected virtual void InitContextForText(Transition t, Context context) {
			context.SetSourceRGBA(0.1, 0.6, 1, 1);
			context.SelectFontFace("Lucida Grande", FontSlant.Normal, FontWeight.Normal);
			context.SetFontSize(12);
		}
		protected virtual void DrawText(Transition t, Context context) {
			string val = t.Name.ToString();
			TextExtents te = context.TextExtents(val);
			context.MoveTo(t.Position.X - te.Width / 2 - te.XBearing, t.Position.Y - te.Height / 2 - te.YBearing);
			context.TextPath(val);
			context.Fill();
		}

		public static void DrawArrow(Context context, PointD direction, PointD position, double scaleAlongAxis)
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
	}
}

