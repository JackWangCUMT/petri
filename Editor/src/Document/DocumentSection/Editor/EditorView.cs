using System;
using System.Collections.Generic;
using Gtk;
using Cairo;
using System.Linq;

namespace Petri
{
	public class EditorView : PetriView
	{
		public enum EditorAction {
			None,
			MovingAction,
			MovingComment,
			MovingTransition,
			CreatingTransition,
			SelectionRect,
			ResizingComment
		}

		public EditorView(Document doc) : base(doc) {
			currentAction = EditorAction.None;
			this.EntityDraw = new EditorEntityDraw(this);
		}

		public EditorAction CurrentAction {
			get {
				return currentAction;
			}
		}

		public Entity HoveredItem {
			get {
				return hoveredItem;
			}
		}

		public override void FocusIn() {
			shiftDown = true;
			shiftDown = false;
			ctrlDown = false;
			currentAction = EditorAction.None;
			base.FocusIn();
			hoveredItem = null;
		}

		public override void FocusOut() {
			shiftDown = false;
			ctrlDown = false;
			currentAction = EditorAction.None;
			base.FocusOut();
		}

		protected override void ManageTwoButtonPress(Gdk.EventButton ev) {
			if(ev.Button == 1) {
				// Add new action
				if(this.selectedEntities.Count == 0) {
					document.PostAction(new AddStateAction(new Action(this.CurrentPetriNet.Document, CurrentPetriNet, false, new PointD(ev.X, ev.Y))));
					hoveredItem = SelectedEntity;
				}
				else if(this.selectedEntities.Count == 1) {
					this.currentAction = EditorAction.None;

					var selected = this.SelectedEntity as State;

					// Change type from Action to InnerPetriNet
					if(selected != null && selected is Action) {
						MessageDialog d = new MessageDialog(document.Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, "Souhaitez-vous vraiment transformer l'action sélectionnée en macro ?");
						d.AddButton("Non", ResponseType.Cancel);
						d.AddButton("Oui", ResponseType.Accept);
						d.DefaultResponse = ResponseType.Accept;

						ResponseType result = (ResponseType)d.Run();

						if(result == ResponseType.Accept) {
							this.ResetSelection();
							var inner = new InnerPetriNet(this.CurrentPetriNet.Document, this.CurrentPetriNet, false, selected.Position);

							var guiActionList = new List<GuiAction>();

							var statesTable = new Dictionary<UInt64, State>();
							foreach(Transition t in selected.TransitionsAfter) {
								statesTable[t.After.ID] = t.After;
								statesTable[t.Before.ID] = inner;
								guiActionList.Add(new RemoveTransitionAction(t, t.After.RequiredTokens == t.After.TransitionsBefore.Count));

								var newTransition = Entity.EntityFromXml(document, t.GetXml(), document.Window.EditorGui.View.CurrentPetriNet, statesTable) as Transition;
								newTransition.ID = document.LastEntityID++;
								guiActionList.Add(new AddTransitionAction(newTransition, newTransition.After.RequiredTokens == newTransition.After.TransitionsBefore.Count));
							}
							foreach(Transition t in selected.TransitionsBefore) {
								statesTable[t.Before.ID] = t.Before;
								statesTable[t.After.ID] = inner;
								guiActionList.Add(new RemoveTransitionAction(t, t.After.RequiredTokens == t.After.TransitionsBefore.Count));

								var newTransition = Entity.EntityFromXml(document, t.GetXml(), document.Window.EditorGui.View.CurrentPetriNet, statesTable) as Transition;
								newTransition.ID = document.LastEntityID++;
								guiActionList.Add(new AddTransitionAction(newTransition, newTransition.After.RequiredTokens == newTransition.After.TransitionsBefore.Count));
							}

							guiActionList.Add(new RemoveStateAction(selected));
							guiActionList.Add(new AddStateAction(inner));
							var guiAction = new GuiActionList(guiActionList, "Transformer l'entité en macro");
							document.PostAction(guiAction);
							selected = inner;
						}
						d.Destroy();
					}

					if(selected is InnerPetriNet) {
						this.CurrentPetriNet = selected as InnerPetriNet;
					}
				}
			}
			else if(ev.Button == 3) {
				if(this.selectedEntities.Count == 0) {
					document.PostAction(new AddCommentAction(new Comment(this.CurrentPetriNet.Document, CurrentPetriNet, new PointD(ev.X, ev.Y))));
					hoveredItem = SelectedEntity;
				}
			}
		}

		protected override void ManageOneButtonPress(Gdk.EventButton ev) {
			if(ev.Button == 1) {
				if(currentAction == EditorAction.None) {
					deltaClick.X = ev.X;
					deltaClick.Y = ev.Y;

					hoveredItem = CurrentPetriNet.StateAtPosition(deltaClick);

					if(hoveredItem == null) {
						hoveredItem = CurrentPetriNet.TransitionAtPosition(deltaClick);

						if(hoveredItem == null) {
							hoveredItem = CurrentPetriNet.CommentAtPosition(deltaClick);
						}
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
							currentAction = EditorAction.MovingAction;
						}
						else if(motionReference is Transition) {
							currentAction = EditorAction.MovingTransition;
						}
						else if(motionReference is Comment) {
							Comment c = motionReference as Comment;
							if(Math.Abs(ev.X - motionReference.Position.X - c.Size.X / 2) < 8 || Math.Abs(ev.X - motionReference.Position.X + (motionReference as Comment).Size.X / 2) < 8) {
								currentAction = EditorAction.ResizingComment;
							}
							else {
								currentAction = EditorAction.MovingComment;
							}
						}
						deltaClick.X = ev.X - originalPosition.X;
						deltaClick.Y = ev.Y - originalPosition.Y;
					}
					else {
						if(!(ctrlDown || shiftDown)) {
							this.ResetSelection();
						}
						else {
							selectedFromRect = new HashSet<Entity>(selectedEntities);
						}
						currentAction = EditorAction.SelectionRect;
						originalPosition.X = ev.X;
						originalPosition.Y = ev.Y;
					}
				}
			}
			else if(ev.Button == 3) {
				if(currentAction == EditorAction.None && hoveredItem != null && hoveredItem is State) {
					SelectedEntity = hoveredItem;
					currentAction = EditorAction.CreatingTransition;
				}
				else if(hoveredItem == null) {
					this.ResetSelection();
				}
			}
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton ev) {
			if(currentAction == EditorAction.MovingAction || currentAction == EditorAction.MovingTransition || currentAction == EditorAction.MovingComment) {
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
						document.PostAction(new GuiActionList(actions, actions.Count > 1 ? "Déplacer les entités" : "Déplacer l'entité"));
					}
				}
				currentAction = EditorAction.None;
			}
			else if(currentAction == EditorAction.CreatingTransition && ev.Button == 1) {
				currentAction = EditorAction.None;
				if(hoveredItem != null && hoveredItem is State) {
					document.PostAction(new AddTransitionAction(new Transition(CurrentPetriNet.Document, CurrentPetriNet, SelectedEntity as State, hoveredItem as State), true));
				}

				this.Redraw();
			}
			else if(currentAction == EditorAction.SelectionRect) {
				currentAction = EditorAction.None;

				this.ResetSelection();
				foreach(var e in selectedFromRect)
					selectedEntities.Add(e);
				document.EditorController.UpdateSelection();

				selectedFromRect.Clear();
			}
			else if(currentAction == EditorAction.ResizingComment) {
				currentAction = EditorAction.None;
			}

			return base.OnButtonReleaseEvent(ev);
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion ev)
		{
			shouldUnselect = false;

			if(currentAction == EditorAction.MovingAction || currentAction == EditorAction.MovingTransition || currentAction == EditorAction.MovingComment) {
				if(currentAction == EditorAction.MovingAction || currentAction == EditorAction.MovingComment) {
					selectedEntities.RemoveWhere(item => item is Transition);
					document.EditorController.UpdateSelection();
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
			else if(currentAction == EditorAction.SelectionRect) {
				deltaClick.X = ev.X;
				deltaClick.Y = ev.Y;

				var oldSet = new HashSet<Entity>(selectedEntities);
				selectedFromRect = new HashSet<Entity>();

				double xm = Math.Min(deltaClick.X, originalPosition.X);
				double ym = Math.Min(deltaClick.Y, originalPosition.Y);
				double xM = Math.Max(deltaClick.X, originalPosition.X);
				double yM = Math.Max(deltaClick.Y, originalPosition.Y);

				foreach(State s in CurrentPetriNet.States) {
					if(xm < s.Position.X + s.Radius && xM > s.Position.X - s.Radius && ym < s.Position.Y + s.Radius && yM > s.Position.Y - s.Radius)
						selectedFromRect.Add(s);
				}

				foreach(Transition t in CurrentPetriNet.Transitions) {
					if(xm < t.Position.X + t.Width / 2 && xM > t.Position.X - t.Width / 2 && ym < t.Position.Y + t.Height / 2 && yM > t.Position.Y - t.Height / 2)
						selectedFromRect.Add(t);
				}

				foreach(Comment c in CurrentPetriNet.Comments) {
					if(xm < c.Position.X + c.Size.X / 2 && xM > c.Position.X - c.Size.X / 2 && ym < c.Position.Y + c.Size.Y / 2 && yM > c.Position.Y - c.Size.Y / 2)
						selectedFromRect.Add(c);
				}

				selectedFromRect.SymmetricExceptWith(oldSet);

				this.Redraw();
			}
			else if(currentAction == EditorAction.ResizingComment) {
				Comment comment = hoveredItem as Comment;
				double w = Math.Abs(ev.X - comment.Position.X) * 2;
				comment.Size = new PointD(w, 0);
				this.Redraw();
			}
			else {
				deltaClick.X = ev.X;
				deltaClick.Y = ev.Y;

				hoveredItem = CurrentPetriNet.StateAtPosition(deltaClick);

				if(hoveredItem == null) {
					hoveredItem = CurrentPetriNet.TransitionAtPosition(deltaClick);

					if(hoveredItem == null) {
						hoveredItem = CurrentPetriNet.CommentAtPosition(deltaClick);
					}
				}

				this.Redraw();
			}

			return base.OnMotionNotifyEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyPressEvent(Gdk.EventKey ev)
		{
			if(ev.Key == Gdk.Key.Escape) {
				if(currentAction == EditorAction.CreatingTransition) {
					currentAction = EditorAction.None;
					this.Redraw();
				}
				else if(currentAction == EditorAction.None) {
					if(selectedEntities.Count > 0) {
						this.ResetSelection();
					}
					else if(this.CurrentPetriNet.Parent != null) {
						this.CurrentPetriNet = this.CurrentPetriNet.Parent;
					}
					this.Redraw();
				}
				else if(currentAction == EditorAction.SelectionRect) {
					currentAction = EditorAction.None;
					this.Redraw();
				}
			}
			else if(selectedEntities.Count > 0 && currentAction == EditorAction.None && (ev.Key == Gdk.Key.Delete || ev.Key == Gdk.Key.BackSpace)) {
				document.PostAction(document.EditorController.RemoveSelection());
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

			return base.OnKeyReleaseEvent(ev);
		}

		protected override EntityDraw EntityDraw {
			get;
			set;
		}

		protected override void SpecializedDrawing(Cairo.Context context) {
				if(currentAction == EditorAction.CreatingTransition) {
					Color color = new Color(1, 0, 0, 1);
					double lineWidth = 2;

					if(hoveredItem != null && hoveredItem is State) {
						color.R = 0;
						color.G = 1;
					}

					PointD direction = new PointD(deltaClick.X - SelectedEntity.Position.X, deltaClick.Y - SelectedEntity.Position.Y);
					if(PetriView.Norm(direction) > (SelectedEntity as State).Radius) {
						direction = PetriView.Normalized(direction);

						PointD origin = new PointD(SelectedEntity.Position.X + direction.X * (SelectedEntity as State).Radius, SelectedEntity.Position.Y + direction.Y * (SelectedEntity as State).Radius);
						PointD destination = deltaClick;

						context.LineWidth = lineWidth;
						context.SetSourceRGBA(color.R, color.G, color.B, color.A);

						double arrowLength = 12;

						context.MoveTo(origin);
						context.LineTo(new PointD(destination.X - 0.99 * direction.X * arrowLength, destination.Y - 0.99 * direction.Y * arrowLength));
						context.Stroke();
						EntityDraw.DrawArrow(context, direction, destination, arrowLength);
					}
				}
				else if(currentAction == EditorAction.SelectionRect) {
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
		}

		public override PetriNet CurrentPetriNet {
			get {
				return base.CurrentPetriNet;
			}
			set {
				this.ResetSelection();
				base.CurrentPetriNet = value;
			}
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
				if(value != null && CurrentPetriNet != value.Parent) {
					if(value is RootPetriNet)
						this.ResetSelection();
					else {
						CurrentPetriNet = value.Parent;
						SelectedEntity = value;
					}
				}
				else {
					selectedEntities.Clear();
					if(value != null)
						selectedEntities.Add(value);
					document.EditorController.UpdateSelection();
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

		public bool EntitySelected(Entity e) {
			if(currentAction == EditorAction.SelectionRect) {
				return selectedFromRect.Contains(e);
			}
			return selectedEntities.Contains(e);
		}

		void AddToSelection(Entity e) {
			selectedEntities.Add(e);
			document.EditorController.UpdateSelection();
		}

		void RemoveFromSelection(Entity e) {
			selectedEntities.Remove(e);
			document.EditorController.UpdateSelection();
		}

		public void ResetSelection() {
			SelectedEntity = null;
			hoveredItem = null;
			selectedEntities.Clear();
		}

		EditorAction currentAction;
		bool shouldUnselect = false;
		Entity motionReference;
		HashSet<Entity> selectedEntities = new HashSet<Entity>();
		HashSet<Entity> selectedFromRect = new HashSet<Entity>();
		Entity hoveredItem;
		bool shiftDown;
		bool ctrlDown;
	}
}

