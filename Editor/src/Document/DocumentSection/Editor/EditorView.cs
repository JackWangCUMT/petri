using System;
using System.Collections.Generic;
using Gtk;
using Cairo;
using System.Linq;

namespace Petri
{
	public class EditorView : PetriView
	{
		public enum EditorTool {
			Arrow,
			Action,
			Transition,
			Comment
		}

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
			CurrentAction = EditorAction.None;
			CurrentTool = EditorTool.Arrow;
			this.EntityDraw = new EditorEntityDraw(this);
		}

		public EditorAction CurrentAction {
			get;
			private set;
		}

		public EditorTool CurrentTool {
			get;
			set;
		}

		public Entity HoveredItem {
			get {
				return _hoveredItem;
			}
		}

		public override void FocusIn() {
			_shiftDown = false;
			_ctrlDown = false;
			CurrentAction = EditorAction.None;
			base.FocusIn();
			_hoveredItem = null;
			_document.ReloadHeaders();
		}

		public override void FocusOut() {
			_shiftDown = false;
			_ctrlDown = false;
			CurrentAction = EditorAction.None;
			base.FocusOut();
		}

		protected override void ManageTwoButtonPress(uint button, double x, double y) {
			if(button == 1 && CurrentTool == EditorTool.Arrow) {
				// Add new action
				if(_selectedEntities.Count == 0) {
					_document.PostAction(new AddStateAction(new Action(this.CurrentPetriNet.Document, CurrentPetriNet, false, new PointD(x, y))));
					_hoveredItem = SelectedEntity;
				}
				else if(_selectedEntities.Count == 1) {
					CurrentAction = EditorAction.None;

					var selected = this.SelectedEntity as State;

					// Change type from Action to InnerPetriNet
					if(selected != null && selected is Action) {
						MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None, "Souhaitez-vous vraiment transformer l'action sélectionnée en macro ?");
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

								var newTransition = Entity.EntityFromXml(_document, t.GetXml(), _document.Window.EditorGui.View.CurrentPetriNet, statesTable) as Transition;
								newTransition.ID = _document.LastEntityID++;
								guiActionList.Add(new AddTransitionAction(newTransition, newTransition.After.RequiredTokens == newTransition.After.TransitionsBefore.Count));
							}
							foreach(Transition t in selected.TransitionsBefore) {
								statesTable[t.Before.ID] = t.Before;
								statesTable[t.After.ID] = inner;
								guiActionList.Add(new RemoveTransitionAction(t, t.After.RequiredTokens == t.After.TransitionsBefore.Count));

								var newTransition = Entity.EntityFromXml(_document, t.GetXml(), _document.Window.EditorGui.View.CurrentPetriNet, statesTable) as Transition;
								newTransition.ID = _document.LastEntityID++;
								guiActionList.Add(new AddTransitionAction(newTransition, newTransition.After.RequiredTokens == newTransition.After.TransitionsBefore.Count));
							}

							guiActionList.Add(new RemoveStateAction(selected));
							guiActionList.Add(new AddStateAction(inner));
							var guiAction = new GuiActionList(guiActionList, "Transformer l'entité en macro");
							_document.PostAction(guiAction);
							selected = inner;
						}
						d.Destroy();
					}

					if(selected is InnerPetriNet) {
						this.CurrentPetriNet = selected as InnerPetriNet;
						this.Redraw();
					}
				}
			}
			else if(button == 3) {
				if(_selectedEntities.Count == 0) {
					_document.PostAction(new AddCommentAction(new Comment(this.CurrentPetriNet.Document, CurrentPetriNet, new PointD(x, y))));
					_hoveredItem = SelectedEntity;
				}
			}
		}

		protected override void ManageOneButtonPress(uint button, double x, double y) {
			if(button == 1) {
				_deltaClick.X = x;
				_deltaClick.Y = y;

				if(CurrentTool != EditorTool.Arrow || CurrentAction == EditorAction.None) {
					_hoveredItem = CurrentPetriNet.StateAtPosition(_deltaClick);

					if(_hoveredItem == null) {
						_hoveredItem = CurrentPetriNet.TransitionAtPosition(_deltaClick);

						if(_hoveredItem == null) {
							_hoveredItem = CurrentPetriNet.CommentAtPosition(_deltaClick);
						}
					}

					if(_hoveredItem != null) {
						if(_shiftDown || _ctrlDown) {
							if(EntitySelected(_hoveredItem))
								RemoveFromSelection(_hoveredItem);
							else
								AddToSelection(_hoveredItem);
						}
						else if(!EntitySelected(_hoveredItem)) {
							this.SelectedEntity = _hoveredItem;
						}

						_motionReference = _hoveredItem;
						_originalPosition.X = _motionReference.Position.X;
						_originalPosition.Y = _motionReference.Position.Y;

						if(_motionReference is State) {
							CurrentAction = EditorAction.MovingAction;
						}
						else if(_motionReference is Transition) {
							CurrentAction = EditorAction.MovingTransition;
						}
						else if(_motionReference is Comment) {
							Comment c = _motionReference as Comment;
							if(Math.Abs(x - _motionReference.Position.X - c.Size.X / 2) < 8 || Math.Abs(x - _motionReference.Position.X + (_motionReference as Comment).Size.X / 2) < 8) {
								CurrentAction = EditorAction.ResizingComment;
								_beforeResize.X = c.Size.X;
								_beforeResize.Y = c.Size.Y;
							}
							else {
								CurrentAction = EditorAction.MovingComment;
							}
						}
						_deltaClick.X = x - _originalPosition.X;
						_deltaClick.Y = y - _originalPosition.Y;
					}
				}

				if(CurrentTool == EditorTool.Arrow) {
					if(CurrentAction == EditorAction.None) {
						if(_hoveredItem == null) {
							if(!(_ctrlDown || _shiftDown)) {
								this.ResetSelection();
							}
							else {
								_selectedFromRect = new HashSet<Entity>(_selectedEntities);
							}
							CurrentAction = EditorAction.SelectionRect;
							_originalPosition.X = x;
							_originalPosition.Y = y;
						}
					}
				}
				else if(CurrentTool == EditorTool.Action) {
					if(_hoveredItem == null) {
						_document.PostAction(new AddStateAction(new Action(this.CurrentPetriNet.Document, CurrentPetriNet, false, new PointD(x, y))));
					}
				}
				else if(CurrentTool == EditorTool.Transition) {
					if(_hoveredItem is State) {
						SelectedEntity = _hoveredItem;
						CurrentAction = EditorAction.CreatingTransition;
						_deltaClick.X = x;
						_deltaClick.Y = y;
					}
				}
				else if(CurrentTool == EditorTool.Comment) {
					if(_hoveredItem == null) {
						_document.PostAction(new AddCommentAction(new Comment(this.CurrentPetriNet.Document, CurrentPetriNet, new PointD(x, y))));
					}
				}
			}
			else if(button == 3 && CurrentTool == EditorTool.Arrow) {
				if(_hoveredItem is State) {
					SelectedEntity = _hoveredItem;
					CurrentAction = EditorAction.CreatingTransition;
					_deltaClick.X = x;
					_deltaClick.Y = y;
				}
				else if(_hoveredItem == null) {
					CurrentAction = EditorAction.None;
					this.ResetSelection();
				}
			}
		}

		protected override void ManageButtonRelease(uint button, double x, double y) {
			if(CurrentAction == EditorAction.MovingAction || CurrentAction == EditorAction.MovingTransition || CurrentAction == EditorAction.MovingComment) {
				if(_shouldUnselect) {
					SelectedEntity = _hoveredItem;
				}
				else {
					var backToPrevious = new PointD(_originalPosition.X - _motionReference.Position.X, _originalPosition.Y - _motionReference.Position.Y);
					if(backToPrevious.X != 0 || backToPrevious.Y != 0) {
						var actions = new List<GuiAction>();
						foreach(var e in _selectedEntities) {
							e.Position = new PointD(e.Position.X + backToPrevious.X, e.Position.Y + backToPrevious.Y);
							actions.Add(new MoveAction(e, new PointD(-backToPrevious.X, -backToPrevious.Y)));
						}
						_document.PostAction(new GuiActionList(actions, actions.Count > 1 ? "Déplacer les entités" : "Déplacer l'entité"));
					}
				}
				CurrentAction = EditorAction.None;
			}
			else if(CurrentAction == EditorAction.CreatingTransition && button == 1) {
				CurrentAction = EditorAction.None;
				if(_hoveredItem != null && _hoveredItem is State) {
					_document.PostAction(new AddTransitionAction(new Transition(CurrentPetriNet.Document, CurrentPetriNet, SelectedEntity as State, _hoveredItem as State), (_hoveredItem as State).RequiredTokens == 0));
				}
			}
			else if(CurrentAction == EditorAction.SelectionRect) {
				CurrentAction = EditorAction.None;

				this.ResetSelection();
				foreach(var e in _selectedFromRect)
					_selectedEntities.Add(e);
				_document.EditorController.UpdateSelection();

				_selectedFromRect.Clear();
			}
			else if(CurrentAction == EditorAction.ResizingComment) {
				CurrentAction = EditorAction.None;
				var newSize = new PointD((_hoveredItem as Comment).Size.X, (_hoveredItem as Comment).Size.Y);
				(_hoveredItem as Comment).Size = _beforeResize;
				_document.PostAction(new ResizeCommentAction(_hoveredItem as Comment, newSize));
			}

			this.Redraw();
		}

		protected override void ManageMotion(double x, double y) {
			_shouldUnselect = false;

			if(CurrentAction == EditorAction.MovingAction || CurrentAction == EditorAction.MovingTransition || CurrentAction == EditorAction.MovingComment) {
				if(CurrentAction == EditorAction.MovingAction || CurrentAction == EditorAction.MovingComment) {
					_selectedEntities.RemoveWhere(item => item is Transition);
					_document.EditorController.UpdateSelection();
				}
				else {
					SelectedEntity = _motionReference;
				}
				var delta = new PointD(x - _deltaClick.X - _motionReference.Position.X, y - _deltaClick.Y - _motionReference.Position.Y);
				foreach(var e in _selectedEntities) {
					e.Position = new PointD(e.Position.X + delta.X, e.Position.Y + delta.Y);
				}
				this.Redraw();
			}
			else if(CurrentAction == EditorAction.SelectionRect) {
				_deltaClick.X = x;
				_deltaClick.Y = y;

				_deltaUpdated = true;
				ManageScrolling();

				var oldSet = new HashSet<Entity>(_selectedEntities);
				_selectedFromRect = new HashSet<Entity>();

				double xm = Math.Min(_deltaClick.X, _originalPosition.X);
				double ym = Math.Min(_deltaClick.Y, _originalPosition.Y);
				double xM = Math.Max(_deltaClick.X, _originalPosition.X);
				double yM = Math.Max(_deltaClick.Y, _originalPosition.Y);

				foreach(State s in CurrentPetriNet.States) {
					if(xm < s.Position.X + s.Radius && xM > s.Position.X - s.Radius && ym < s.Position.Y + s.Radius && yM > s.Position.Y - s.Radius)
						_selectedFromRect.Add(s);
				}

				foreach(Transition t in CurrentPetriNet.Transitions) {
					if(xm < t.Position.X + t.Width / 2 && xM > t.Position.X - t.Width / 2 && ym < t.Position.Y + t.Height / 2 && yM > t.Position.Y - t.Height / 2)
						_selectedFromRect.Add(t);
				}

				foreach(Comment c in CurrentPetriNet.Comments) {
					if(xm < c.Position.X + c.Size.X / 2 && xM > c.Position.X - c.Size.X / 2 && ym < c.Position.Y + c.Size.Y / 2 && yM > c.Position.Y - c.Size.Y / 2)
						_selectedFromRect.Add(c);
				}

				_selectedFromRect.SymmetricExceptWith(oldSet);

				this.Redraw();
			}
			else if(CurrentAction == EditorAction.ResizingComment) {
				Comment comment = _hoveredItem as Comment;
				double w = Math.Abs(x - comment.Position.X) * 2;
				comment.Size = new PointD(w, 0);
				this.Redraw();
			}
			else {
				_deltaClick.X = x;
				_deltaClick.Y = y;

				_hoveredItem = CurrentPetriNet.StateAtPosition(_deltaClick);

				if(_hoveredItem == null) {
					_hoveredItem = CurrentPetriNet.TransitionAtPosition(_deltaClick);

					if(_hoveredItem == null) {
						_hoveredItem = CurrentPetriNet.CommentAtPosition(_deltaClick);
					}
				}

				this.Redraw();
			}
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyPressEvent(Gdk.EventKey ev)
		{
			if(ev.Key == Gdk.Key.Escape) {
				if(CurrentAction == EditorAction.CreatingTransition) {
					CurrentAction = EditorAction.None;
					this.Redraw();
				}
				else if(CurrentAction == EditorAction.None) {
					if(_selectedEntities.Count > 0) {
						this.ResetSelection();
					}
					else if(this.CurrentPetriNet.Parent != null) {
						this.CurrentPetriNet = this.CurrentPetriNet.Parent;
					}
					this.Redraw();
				}
				else if(CurrentAction == EditorAction.SelectionRect) {
					CurrentAction = EditorAction.None;
					this.Redraw();
				}
			}
			else if(_selectedEntities.Count > 0 && CurrentAction == EditorAction.None && (ev.Key == Gdk.Key.Delete || ev.Key == Gdk.Key.BackSpace)) {
				_document.PostAction(_document.EditorController.RemoveSelection());
			}
			else if(ev.Key == Gdk.Key.Shift_L || ev.Key == Gdk.Key.Shift_R) {
				_shiftDown = true;
			}
			else if(((Configuration.RunningPlatform == Platform.Mac) && (ev.Key == Gdk.Key.Meta_L || ev.Key == Gdk.Key.Meta_R)) || ((Configuration.RunningPlatform != Platform.Mac) && (ev.Key == Gdk.Key.Control_L || ev.Key == Gdk.Key.Control_L))) {
				_ctrlDown = true;
			}

			return base.OnKeyPressEvent(ev);
		}

		[GLib.ConnectBefore()]
		protected override bool OnKeyReleaseEvent(Gdk.EventKey ev) {
			if(ev.Key == Gdk.Key.Shift_L || ev.Key == Gdk.Key.Shift_R) {
				_shiftDown = false;
			}
			else if(((Configuration.RunningPlatform == Platform.Mac) && (ev.Key == Gdk.Key.Meta_L || ev.Key == Gdk.Key.Meta_R)) || ((Configuration.RunningPlatform != Platform.Mac) && (ev.Key == Gdk.Key.Control_L || ev.Key == Gdk.Key.Control_L))) {
				_ctrlDown = false;
			}

			return base.OnKeyReleaseEvent(ev);
		}

		protected void ManageScrolling() {
			if(!_scrolling) {
				GLib.Timeout.Add(100, () => {
					_scrolling = true;

					var scrolled = _document.Window.EditorGui.ScrolledWindow;
					const double margin = 30, powCoef= 0.6;

					if(_deltaUpdated) {
						_scrollingDelta.X = _deltaClick.X * Zoom - scrolled.Hadjustment.Value;
						_scrollingDelta.Y = _deltaClick.Y * Zoom - scrolled.Vadjustment.Value;
						_deltaUpdated = false;
					}

					if(_scrollingDelta.X > scrolled.Allocation.Width - margin) {
						scrolled.Hadjustment.Value += Math.Pow((_scrollingDelta.X - (scrolled.Allocation.Width - margin)), powCoef);
						scrolled.Hadjustment.Value = Math.Min(scrolled.Hadjustment.Upper - scrolled.Hadjustment.PageSize, scrolled.Hadjustment.Value);				
					}
					else if(_scrollingDelta.X < margin) {
						scrolled.Hadjustment.Value -= Math.Pow(margin - _scrollingDelta.X, powCoef);
						scrolled.Hadjustment.Value = Math.Max(scrolled.Hadjustment.Lower, scrolled.Hadjustment.Value);				
					}

					if(_scrollingDelta.Y > scrolled.Allocation.Height - margin) {
						scrolled.Vadjustment.Value += Math.Pow((_scrollingDelta.Y - (scrolled.Allocation.Height - margin)), powCoef);
						scrolled.Vadjustment.Value = Math.Min(scrolled.Vadjustment.Upper - scrolled.Vadjustment.PageSize, scrolled.Vadjustment.Value);				
					}
					else if(_scrollingDelta.Y < margin) {
						scrolled.Vadjustment.Value -= Math.Pow(margin - _scrollingDelta.Y, powCoef);
						scrolled.Vadjustment.Value = Math.Max(scrolled.Vadjustment.Lower, scrolled.Vadjustment.Value);				
					}

					if(CurrentAction != EditorAction.SelectionRect) {
						_scrolling = false;
					}

					return CurrentAction == EditorAction.SelectionRect;
				});
			}
		}

		protected override EntityDraw EntityDraw {
			get;
			set;
		}

		protected override void SpecializedDrawing(Cairo.Context context) {
			if(CurrentAction == EditorAction.CreatingTransition) {
				Color color = new Color(1, 0, 0, 1);
				double lineWidth = 2;

				if(_hoveredItem != null && _hoveredItem is State) {
					color.R = 0;
					color.G = 1;
				}

				PointD direction = new PointD(_deltaClick.X - SelectedEntity.Position.X, _deltaClick.Y - SelectedEntity.Position.Y);
				if(PetriView.Norm(direction) > (SelectedEntity as State).Radius) {
					direction = PetriView.Normalized(direction);

					PointD origin = new PointD(SelectedEntity.Position.X + direction.X * (SelectedEntity as State).Radius, SelectedEntity.Position.Y + direction.Y * (SelectedEntity as State).Radius);
					PointD destination = _deltaClick;

					context.LineWidth = lineWidth;
					context.SetSourceRGBA(color.R, color.G, color.B, color.A);

					double arrowLength = 12;

					context.MoveTo(origin);
					context.LineTo(new PointD(destination.X - 0.99 * direction.X * arrowLength, destination.Y - 0.99 * direction.Y * arrowLength));
					context.Stroke();
					EntityDraw.DrawArrow(context, direction, destination, arrowLength);
				}
			}
			else if(CurrentAction == EditorAction.SelectionRect) {
				double xm = Math.Min(_deltaClick.X, _originalPosition.X);
				double ym = Math.Min(_deltaClick.Y, _originalPosition.Y);
				double xM = Math.Max(_deltaClick.X, _originalPosition.X);
				double yM = Math.Max(_deltaClick.Y, _originalPosition.Y);

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
				if(_selectedEntities.Count == 1) {
					foreach(Entity e in _selectedEntities) // Just to compensate the strange absence of an Any() method which would return an object in the set
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
					_selectedEntities.Clear();
					if(value != null)
						_selectedEntities.Add(value);
					_document.EditorController.UpdateSelection();
				}
			}
		}

		public HashSet<Entity> SelectedEntities {
			get {
				return _selectedEntities;
			}
		}

		public bool MultipleSelection {
			get {
				return _selectedEntities.Count > 1;
			}
		}

		public bool EntitySelected(Entity e) {
			if(CurrentAction == EditorAction.SelectionRect) {
				return _selectedFromRect.Contains(e);
			}
			return _selectedEntities.Contains(e);
		}

		void AddToSelection(Entity e) {
			_selectedEntities.Add(e);
			_document.EditorController.UpdateSelection();
		}

		void RemoveFromSelection(Entity e) {
			_selectedEntities.Remove(e);
			_document.EditorController.UpdateSelection();
		}

		public void ResetSelection() {
			SelectedEntity = null;
			_hoveredItem = null;
			_selectedEntities.Clear();
		}

		bool _shouldUnselect = false;
		Entity _motionReference;
		HashSet<Entity> _selectedEntities = new HashSet<Entity>();
		HashSet<Entity> _selectedFromRect = new HashSet<Entity>();
		PointD _beforeResize = new PointD();
		Entity _hoveredItem;
		bool _shiftDown;
		bool _ctrlDown;

		bool _scrolling = false, _deltaUpdated = false;
		PointD _scrollingDelta = new PointD();
	}
}

