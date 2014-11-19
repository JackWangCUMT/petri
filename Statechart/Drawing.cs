using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using System.Linq;

namespace Statechart
{
	[System.ComponentModel.ToolboxItem(true)]
	public class Drawing : Gtk.DrawingArea
	{

		public enum CurrentAction
		{
			None,
			MovingAction,
			MovingTransition,
			CreatingTransition,
			SelectionRect
		}

		public Drawing (Document doc)
		{
			document = doc;
			currentAction = CurrentAction.None;
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

		public void FocusIn() {
			shiftDown = true;
			shiftDown = false;
			ctrlDown = false;
			currentAction = CurrentAction.None;
			this.Redraw();
			hoveredItem = null;
		}

		public void FocusOut() {
			shiftDown = false;
			ctrlDown = false;
			currentAction = CurrentAction.None;
			this.Redraw();
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton ev)
		{
			if(ev.Type == Gdk.EventType.ButtonPress) {
				// The Windows version of GTK# currently doesn't detect TwoButtonPress events, so here is a lame simulation of it.
				if(/*ev.Type == Gdk.EventType.TwoButtonPress || */(lastClickPosition.X == ev.X && lastClickPosition.Y == ev.Y && (DateTime.Now - lastClickDate).TotalMilliseconds < 500)) {
					lastClickPosition.X = -12345;
					if(ev.Button == 1) {
						// Add new action
						if(this.selectedEntities.Count == 0) {
							document.Controller.PostAction(new AddStateAction(new Action(this.EditedStateChart.Document, EditedStateChart, false, new PointD(ev.X, ev.Y))/*, new List<Transition>()*/));
							hoveredItem = SelectedEntity;
						}
						else if(this.selectedEntities.Count == 1) {
							this.currentAction = CurrentAction.None;

							var selected = this.SelectedEntity as State;

							// Change type from Action to InnerStateChart
							if(selected != null && selected is Action) {
								MessageDialog d = new MessageDialog(document.Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, "Souhaitez-vous vraiment transformer l'action sélectionnée en macro ?");
								d.AddButton("Non", ResponseType.Cancel);
								d.AddButton("Oui", ResponseType.Accept);
								d.DefaultResponse = ResponseType.Accept;

								ResponseType result = (ResponseType)d.Run();

								if(result == ResponseType.Accept) {
									this.ResetSelection();
									var inner = new InnerStateChart(this.EditedStateChart.Document, this.EditedStateChart, false, selected.Position);
									foreach(var t in selected.TransitionsAfter) {
										t.Before = inner;
									}
									foreach(var t in selected.TransitionsBefore) {
										t.After = inner;
									}
									selected.TransitionsAfter.Clear();
									selected.TransitionsBefore.Clear();
									EditedStateChart.RemoveState(selected);
									EditedStateChart.AddState(inner);
									selected = inner;
								}
								d.Destroy();
							}

							if(selected is InnerStateChart) {
								this.EditedStateChart = selected as InnerStateChart;
							}
						}
					}
				}
				else {
					lastClickDate = DateTime.Now;
					lastClickPosition.X = ev.X;
					lastClickPosition.Y = ev.Y;

					if(ev.Button == 1) {
						if(currentAction == CurrentAction.None) {
							deltaClick.X = ev.X;
							deltaClick.Y = ev.Y;

							hoveredItem = EditedStateChart.StateAtPosition(deltaClick);

							if(hoveredItem == null) {
								hoveredItem = EditedStateChart.TransitionAtPosition(deltaClick);
							}

							if(hoveredItem != null) {
								if(shiftDown || ctrlDown) {
									if(EntitySelected(hoveredItem))
										RemoveFromSelection(hoveredItem);
									else
										AddToSelection(hoveredItem);
								}
								else if(!EntitySelected(hoveredItem)) {
									this.SelectedEntity = hoveredItem;
								}
																
								motionReference = hoveredItem;
								originalPosition.X = motionReference.Position.X;
								originalPosition.Y = motionReference.Position.Y;

								if(motionReference is State) {
									currentAction = CurrentAction.MovingAction;
								}
								else if(motionReference is Transition) {
									currentAction = CurrentAction.MovingTransition;
								}
								deltaClick.X = ev.X - originalPosition.X;
								deltaClick.Y = ev.Y - originalPosition.Y;
							}
							else {
								if(!(ctrlDown || shiftDown))
									this.ResetSelection();
								else
									selectedFromRect = new HashSet<Entity>(selectedEntities);
								currentAction = CurrentAction.SelectionRect;
								originalPosition.X = ev.X;
								originalPosition.Y = ev.Y;
							}
						}
					}
					else if(ev.Button == 3) {
						if(currentAction == CurrentAction.None && hoveredItem != null && hoveredItem is State) {
							SelectedEntity = hoveredItem;
							currentAction = CurrentAction.CreatingTransition;
						}
					}
				}
			}
			//else if(ev.Type == Gdk.EventType.TwoButtonPress) {
			//}

			this.Redraw();

			return base.OnButtonPressEvent(ev);
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton ev)
		{
			if(currentAction == CurrentAction.MovingAction || currentAction == CurrentAction.MovingTransition) {
				if(shouldUnselect) {
					SelectedEntity = hoveredItem;
				}
				else {
					var backToPrevious = new PointD(originalPosition.X - motionReference.Position.X, originalPosition.Y - motionReference.Position.Y);
					if(backToPrevious.X != 0 || backToPrevious.Y != 0) {
						var actions = new List<GuiAction>();
						foreach(var e in selectedEntities) {
							e.Position = new PointD(e.Position.X + backToPrevious.X, e.Position.Y + backToPrevious.Y);
							actions.Add(new MoveAction(e, new PointD(-backToPrevious.X, -backToPrevious.Y)));
						}
						document.Controller.PostAction(new GuiActionList(actions, actions.Count > 1 ? "Déplacer les entités" : "Déplacer l'entité"));
					}
				}
				currentAction = CurrentAction.None;
			}
			else if(currentAction == CurrentAction.CreatingTransition && ev.Button == 1) {
				currentAction = CurrentAction.None;
				if(hoveredItem != null && hoveredItem is State) {
					document.Controller.PostAction(new AddTransitionAction(new Transition(EditedStateChart.Document, EditedStateChart, SelectedEntity as State, hoveredItem as State), true));
				}

				this.Redraw();
			}
			else if(currentAction == CurrentAction.SelectionRect) {
				currentAction = CurrentAction.None;

				this.ResetSelection();
				foreach(var e in selectedFromRect)
					selectedEntities.Add(e);
				document.Controller.UpdateSelection();

				selectedFromRect.Clear();
			}

			return base.OnButtonReleaseEvent(ev);
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion ev)
		{
			shouldUnselect = false;

			if(currentAction == CurrentAction.MovingAction || currentAction == CurrentAction.MovingTransition) {
				if(currentAction == CurrentAction.MovingAction) {
					selectedEntities.RemoveWhere(item => item is Transition);
					document.Controller.UpdateSelection();
				}
				else {
					SelectedEntity = motionReference;
				}
				var delta = new PointD(ev.X - deltaClick.X - motionReference.Position.X, ev.Y - deltaClick.Y - motionReference.Position.Y);
				foreach(var e in selectedEntities) {
					e.Position = new PointD(e.Position.X + delta.X, e.Position.Y + delta.Y);
				}
				this.Redraw();
			}
			else if(currentAction == CurrentAction.SelectionRect) {
				deltaClick.X = ev.X;
				deltaClick.Y = ev.Y;

				var oldSet = new HashSet<Entity>(selectedEntities);
				selectedFromRect = new HashSet<Entity>();

				double xm = Math.Min(deltaClick.X, originalPosition.X);
				double ym = Math.Min(deltaClick.Y, originalPosition.Y);
				double xM = Math.Max(deltaClick.X, originalPosition.X);
				double yM = Math.Max(deltaClick.Y, originalPosition.Y);

				foreach(State s in EditedStateChart.States) {
					if(xm < s.Position.X + s.Radius && xM > s.Position.X - s.Radius && ym < s.Position.Y + s.Radius && yM > s.Position.Y - s.Radius)
						selectedFromRect.Add(s);
				}

				foreach(Transition t in EditedStateChart.Transitions) {
					if(xm < t.Position.X + t.Width / 2 && xM > t.Position.X - t.Width / 2 && ym < t.Position.Y + t.Width / 2 && yM > t.Position.Y - t.Width / 2)
						selectedFromRect.Add(t);
				}

				selectedFromRect.SymmetricExceptWith(oldSet);

				this.Redraw();
			}
			else {
				deltaClick.X = ev.X;
				deltaClick.Y = ev.Y;

				hoveredItem = EditedStateChart.StateAtPosition(deltaClick);

				if(hoveredItem == null) {
					hoveredItem = EditedStateChart.TransitionAtPosition(deltaClick);
				}

				this.Redraw();
			}

			return base.OnMotionNotifyEvent(ev);
		}

		public void KeyPress(Gdk.EventKey ev)
		{
			this.OnKeyPressEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyPressEvent(Gdk.EventKey ev)
		{
			if(ev.Key == Gdk.Key.Escape) {
				if(currentAction == CurrentAction.CreatingTransition) {
					currentAction = CurrentAction.None;
					this.Redraw();
				}
				else if(currentAction == CurrentAction.None) {
					if(selectedEntities.Count > 0) {
						this.ResetSelection();
					}
					else if(this.EditedStateChart.Parent != null) {
						this.EditedStateChart = this.EditedStateChart.Parent;
					}
					this.Redraw();
				}
				else if(currentAction == CurrentAction.SelectionRect) {
					currentAction = CurrentAction.None;
					this.Redraw();
				}
			}
			else if(selectedEntities.Count > 0 && currentAction == CurrentAction.None && (ev.Key == Gdk.Key.Delete || ev.Key == Gdk.Key.BackSpace)) {
				document.Controller.PostAction(document.Controller.RemoveSelection());
			}
			else if(ev.Key == Gdk.Key.Shift_L || ev.Key == Gdk.Key.Shift_R) {
				shiftDown = true;
			}
			else if(((Configuration.RunningPlatform == Platform.Mac) && (ev.Key == Gdk.Key.Meta_L || ev.Key == Gdk.Key.Meta_R)) || ((Configuration.RunningPlatform != Platform.Mac) && (ev.Key == Gdk.Key.Control_L || ev.Key == Gdk.Key.Control_L))) {
				ctrlDown = true;
			}

			return base.OnKeyPressEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyReleaseEvent(Gdk.EventKey ev) {
			if(ev.Key == Gdk.Key.Shift_L || ev.Key == Gdk.Key.Shift_R) {
				shiftDown = false;
			}
			else if(((Configuration.RunningPlatform == Platform.Mac) && (ev.Key == Gdk.Key.Meta_L || ev.Key == Gdk.Key.Meta_R)) || ((Configuration.RunningPlatform != Platform.Mac) && (ev.Key == Gdk.Key.Control_L || ev.Key == Gdk.Key.Control_L))) {
				ctrlDown = false;
			}

			return base.OnKeyPressEvent(ev);
		}

		protected override bool OnExposeEvent(Gdk.EventExpose ev)
		{
			base.OnExposeEvent(ev);

			needsRedraw = false;

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
					string val = EditedStateChart.Name;
					TextExtents te = context.TextExtents(val);
					context.MoveTo(15 - te.XBearing, 15 - te.YBearing);
					context.TextPath(val);
					context.Fill();
				}

				foreach(var t in EditedStateChart.Transitions) {
					Color c = new Color(0.1, 0.6, 1, 1);
					double lineWidth = 2;
					double arrowScale = 12;

					if(EntitySelected(t)) {
						c.R = 0.3;
						c.G = 0.8;
						lineWidth += 2;
						arrowScale = 18;
					}

					context.SetSourceRGBA(c.R, c.G, c.B, c.A);
					context.LineWidth = lineWidth;

					context.Save();

					PointD direction = new PointD(t.After.Position.X - t.Position.X, t.After.Position.Y - t.Position.Y);

					double radB = t.Before.Radius;
					double radA = t.After.Radius;

					if(Drawing.Norm(direction) > radB) {
						direction = Drawing.Normalized(direction);
						PointD destination = new PointD(t.After.Position.X - direction.X * radA, t.After.Position.Y - direction.Y * radA);

						direction = Drawing.Normalized(t.Position.X - t.Before.Position.X, t.Position.Y - t.Before.Position.Y);
						PointD origin = new PointD(t.Before.Position.X + direction.X * radB, t.Before.Position.Y + direction.Y * radB);

						context.MoveTo(origin);
						
						PointD c1 = new PointD(t.Position.X, t.Position.Y);
						PointD c2 = new PointD(t.Position.X, t.Position.Y);

						PointD direction2 = new PointD(destination.X - t.Position.X, destination.Y - t.Position.Y);
						direction2 = Drawing.Normalized(direction2);

						context.CurveTo(c1, c2, new PointD(destination.X - 0.99 * direction2.X * arrowScale, destination.Y - 0.99 * direction2.Y * arrowScale));

						context.Stroke();

						direction = Drawing.Normalized(destination.X - t.Position.X, destination.Y - t.Position.Y);
						Drawing.DrawArrow(context, direction, destination, arrowScale);
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

				foreach(var a in EditedStateChart.States) {
					Color color = new Color(0, 0, 0, 1);
					double lineWidth = 3;

					if(EntitySelected(a)) {
						color.R = 1;
					}
					context.LineWidth = lineWidth;
					context.SetSourceRGBA(color.R, color.G, color.B, color.A);

					context.Save();

					if(a == hoveredItem && currentAction == CurrentAction.CreatingTransition) {
						lineWidth += 2;
					}

					context.LineWidth = lineWidth;

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

				if(currentAction == CurrentAction.CreatingTransition) {
					Color color = new Color(1, 0, 0, 1);
					double lineWidth = 2;

					if(hoveredItem != null && hoveredItem is State) {
						color.R = 0;
						color.G = 1;
					}

					PointD direction = new PointD(deltaClick.X - SelectedEntity.Position.X, deltaClick.Y - SelectedEntity.Position.Y);
					if(Drawing.Norm(direction) > (SelectedEntity as State).Radius) {
						direction = Drawing.Normalized(direction);

						PointD origin = new PointD(SelectedEntity.Position.X + direction.X * (SelectedEntity as State).Radius, SelectedEntity.Position.Y + direction.Y * (SelectedEntity as State).Radius);
						PointD destination = deltaClick;

						context.LineWidth = lineWidth;
						context.SetSourceRGBA(color.R, color.G, color.B, color.A);

						double arrowLength = 12;

						context.MoveTo(origin);
						context.LineTo(new PointD(destination.X - 0.99 * direction.X * arrowLength, destination.Y - 0.99 * direction.Y * arrowLength));
						context.Stroke();
						Drawing.DrawArrow(context, direction, destination, arrowLength);
					}
				}
				else if(currentAction == CurrentAction.SelectionRect) {
					double xm = Math.Min(deltaClick.X, originalPosition.X);
					double ym = Math.Min(deltaClick.Y, originalPosition.Y);
					double xM = Math.Max(deltaClick.X, originalPosition.X);
					double yM = Math.Max(deltaClick.Y, originalPosition.Y);

					context.LineWidth = 1;
					context.MoveTo(xm, ym);
					context.SetSourceRGBA(0.4, 0.4, 0.4, 0.6);
					context.Rectangle(xm, ym, xM - xm, yM - ym);
					context.StrokePreserve();
					context.SetSourceRGBA(0.8, 0.8, 0.8, 0.3);
					context.Fill();
				}

				context.LineWidth = 4;
				context.MoveTo(0, 0);
				context.LineTo(this.Allocation.Width, 0);
				context.LineTo(this.Allocation.Width, this.Allocation.Height);
				context.LineTo(0, this.Allocation.Height);
				context.LineTo(0, 0);
				context.SetSourceRGBA(0.7, 0.7, 0.7, 1);
				context.Stroke();
			}

			return true;
		}

		public RootStateChart RootStateChart {
			get {
				return document.Controller.StateChart;
			}
		}

		public StateChart EditedStateChart {
			get {
				return editedStateChart;
			}
			set {
				this.ResetSelection();
				document.Controller.EditedObject = null;
				editedStateChart = value;
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
			double norm = Drawing.Norm(vec);
			if(norm < 1e-3) {
				return new PointD(0, 0);
			}

			return new PointD(vec.X / norm, vec.Y / norm);
		}

		public static PointD Normalized(double x, double y)
		{
			return Drawing.Normalized(new PointD(x, y));
		}

		private static void DrawArrow(Context context, PointD direction, PointD position, double scaleAlongAxis)
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

		public Entity SelectedEntity {
			get {
				if(selectedEntities.Count == 1) {
					foreach(Entity e in selectedEntities) // Just to compensate the strange absence of an Any() method which would return an object in the set
						return e;
					return null;
				}
				else
					return null;
			}
			set {
				if(value != null && EditedStateChart != value.Parent) {
					if(value is RootStateChart)
						this.ResetSelection();
					else {
						EditedStateChart = value.Parent;
						SelectedEntity = value;
					}
				}
				else {
					selectedEntities.Clear();
					if(value != null)
						selectedEntities.Add(value);
					document.Controller.UpdateSelection();
				}
			}
		}

		public HashSet<Entity> SelectedEntities {
			get {
				return selectedEntities;
			}
		}

		public bool MultipleSelection {
			get {
				return selectedEntities.Count > 1;
			}
		}

		bool EntitySelected(Entity e) {
			if(currentAction == CurrentAction.SelectionRect) {
				return selectedFromRect.Contains(e);
			}
			return selectedEntities.Contains(e);
		}

		void AddToSelection(Entity e) {
			selectedEntities.Add(e);
			document.Controller.UpdateSelection();
		}

		void RemoveFromSelection(Entity e) {
			selectedEntities.Remove(e);
			document.Controller.UpdateSelection();
		}

		public void ResetSelection() {
			SelectedEntity = null;
			hoveredItem = null;
			selectedEntities.Clear();
		}

		Document document;

		StateChart editedStateChart;
		bool needsRedraw;

		bool shiftDown;
		bool ctrlDown;

		bool shouldUnselect = false;
		Entity motionReference;
		HashSet<Entity> selectedEntities = new HashSet<Entity>();
		HashSet<Entity> selectedFromRect = new HashSet<Entity>();
		Entity hoveredItem;

		CurrentAction currentAction;
		PointD deltaClick;
		PointD originalPosition;

		PointD lastClickPosition;
		System.DateTime lastClickDate;
	}
}
