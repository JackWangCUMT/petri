/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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

		protected override void DrawBorder(Comment c, Context context) {
			base.DrawBorder(c, context);
			if(_editor.EntitySelected(c)) {
				PointD point = new PointD(c.Position.X - c.Size.X / 2 - 2, c.Position.Y - 2);
				context.MoveTo(point);
				point.X += 6;
				context.LineTo(point);
				point.Y += 6;
				context.LineTo(point);
				point.X -= 6;
				context.LineTo(point);
				point.Y -= 6;
				context.LineTo(point);

				point.X = c.Position.X + c.Size.X / 2 - 7;
				context.MoveTo(point);
				point.X += 6;
				context.LineTo(point);
				point.Y += 6;
				context.LineTo(point);
				point.X -= 6;
				context.LineTo(point);
				point.Y -= 6;
				context.LineTo(point);
				context.Fill();
			}
		}

		protected override void InitContextForBackground(State s, Context context) {
			Color color = new Color(1, 1, 1, 1);

			if(_editor.RootPetriNet.Document.Conflicts(s)) {
				if(s is PetriNet) {
					color.R = 1;
					color.G = 0.7;
					color.B = 0.3;
				}
				else {
					color.R = 1;
					color.G = 0.6;
					color.B = 0.6;
				}
			}

			context.SetSourceRGBA(color.R, color.G, color.B, color.A);
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
				if(_editor.CurrentAction == EditorView.EditorAction.ChangingTransitionDestination || _editor.CurrentAction == EditorView.EditorAction.ChangingTransitionOrigin) {
					c.R = 0.6;
					c.G = 0.6;
					c.B = 0.6;
				}
				else {
					c.R = 0.3;
					c.G = 0.8;
					lineWidth += 2;
				}
			}
			context.SetSourceRGBA(c.R, c.G, c.B, c.A);
			context.LineWidth = lineWidth;
		}

		protected override void InitContextForBackground(Transition t, Context context) {
			Color color = new Color(1, 1, 1, 1);

			if(_editor.RootPetriNet.Document.Conflicts(t)) {
				color.R = 1;
				color.G = 0.6;
				color.B = 0.6;
			}

			context.SetSourceRGBA(color.R, color.G, color.B, color.A);
		}

		protected override void DrawLine(Transition t, Context context) {
			base.DrawLine(t, context);

			if(_editor.EntitySelected(t) && !_editor.MultipleSelection) {
				PointD direction = TransitionDirection(t);

				double radB = t.Before.Radius;
				double radA = t.After.Radius;

				if(PetriView.Norm(direction) > radB) {
					direction = PetriView.Normalized(direction);
					PointD destination = TransitionDestination(t, direction);

					PointD origin = TransitionOrigin(t);
					direction = PetriView.Normalized(t.Position.X - t.Before.Position.X, t.Position.Y - t.Before.Position.Y);

					context.Arc(origin.X + 5 * direction.X, origin.Y + 5 * direction.Y, 5, 0, 2 * Math.PI);

					PointD direction2 = new PointD(destination.X - t.Position.X, destination.Y - t.Position.Y);
					direction2 = PetriView.Normalized(direction2);
					context.Arc(destination.X - 5 * direction2.X, destination.Y - 5 * direction2.Y, 5, 0, 2 * Math.PI);

					context.Fill();
				}
			}
		}

		static public PointD GetOriginHandle(Transition t) {
			PointD origin = TransitionOrigin(t);
			PointD direction = PetriView.Normalized(t.Position.X - t.Before.Position.X, t.Position.Y - t.Before.Position.Y);

			return new PointD(origin.X + 5 * direction.X, origin.Y + 5 * direction.Y);
		}

		static public PointD GetDestinationHandle(Transition t) {
			PointD direction = PetriView.Normalized(TransitionDirection(t));
			PointD destination = TransitionDestination(t, direction);

			PointD direction2 = new PointD(destination.X - t.Position.X, destination.Y - t.Position.Y);
			direction2 = PetriView.Normalized(direction2);

			return new PointD(destination.X - 5 * direction2.X, destination.Y - 5 * direction2.Y);
		}

		static public bool IsOnOriginHandle(double x, double y, Entity e) {
			if(!(e is Transition))
				return false;

			var pos = GetOriginHandle((Transition)e);
			if(Math.Abs(x - pos.X) < 10.0 / 2 && Math.Abs(y - pos.Y) < 10.0 / 2) {
				return true;
			}

			return false;
		}

		static public bool IsOnDestinationHandle(double x, double y, Entity e) {
			if(!(e is Transition))
				return false;

			var pos = GetDestinationHandle((Transition)e);
			if(Math.Abs(x - pos.X) < 10.0 / 2 && Math.Abs(y - pos.Y) < 10.0 / 2) {
				return true;
			}

			return false;
		}

		protected override void InitContextForText(Transition t, Context context) {
			base.InitContextForText(t, context);
			if(_editor.EntitySelected(t) && (_editor.CurrentAction == EditorView.EditorAction.ChangingTransitionDestination || _editor.CurrentAction == EditorView.EditorAction.ChangingTransitionOrigin)) {
				context.SetSourceRGBA(0.6, 0.6, 0.6, 1);
			}
		}

		private EditorView _editor;
	}
}

