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
using Gtk;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	public class EditorController : Controller
	{
		public EditorController(Document doc) {
			_document = doc;

			EntityEditor = EntityEditor.GetEditor(null, doc);
			this.UpdateMenuItems();
		}

		public override void Copy() {
			if(_document.Window.EditorGui.View.SelectedEntities.Count > 0) {
				MainClass.Clipboard = new HashSet<Entity>(CloneEntities(_document.Window.EditorGui.View.SelectedEntities, _document));
				MainClass.PasteCount = 0;

				this.UpdateMenuItems();
			}
		}

		public override void Paste() {
			if(MainClass.Clipboard.Count > 0) {
				++MainClass.PasteCount;
				var action = PasteAction();
				_document.PostAction(action);

				var pasted = action.Focus as List<Entity>;
				_document.Window.EditorGui.View.SelectedEntities.Clear();
				_document.Window.EditorGui.View.SelectedEntities.UnionWith(pasted);
				this.UpdateSelection();
			}
		}

		public override void Cut() {
			if(_document.Window.EditorGui.View.SelectedEntities.Count > 0) {
				Copy();
				_document.PostAction(new GuiActionWrapper(this.RemoveSelection(), Configuration.GetLocalized("Couper les entités")));
			}
		}

		public void EmbedInMacro() {
			var selected = _document.Window.EditorGui.View.SelectedEntities;
			if(selected.Count > 0) {
				var toRemove = new List<Entity>();
				foreach(var e in selected) {
					if(e is Transition) {
						var t = (Transition)e;
						if(!selected.Contains(t.After) || !selected.Contains(t.After))
							toRemove.Add(t);
					}
					else if(e is State) {
						var s = (State)e;
						foreach(var t in s.TransitionsBefore) {
							if(!selected.Contains(t)) {
								toRemove.Add(t);
							}
						}
						foreach(var t in s.TransitionsAfter) {
							if(!selected.Contains(t)) {
								toRemove.Add(t);
							}
						}
					}
				}

				if(toRemove.Count > 0) {
					MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Error, ButtonsType.None, MainClass.SafeMarkupFromString(Configuration.GetLocalized("Impossible d'encapsuler la sélection dans une macro : des entités qui sont reliées à la sélection ne sont pas sélectionnées.")));
					d.AddButton("OK", ResponseType.Cancel);
					d.Run();
					d.Destroy();
					return;
				}


				var actions = new List<GuiAction>();

				var pos = new Cairo.PointD(double.MaxValue, double.MaxValue);
				foreach(var e in selected) {
					if(e is State) {
						pos.X = Math.Min(pos.X, e.Position.X);
						pos.Y = Math.Min(pos.Y, e.Position.Y);
					}
				}
				var macro = new InnerPetriNet(_document, _document.Window.EditorGui.View.CurrentPetriNet, false, pos);

				foreach(var e in selected) {
					if(e is Comment) {
						actions.Add(new RemoveCommentAction(e as Comment));
					}
					else if(e is State) {
						actions.Add(new RemoveStateAction(e as State));
					}
					else if(e is Transition) {
						actions.Add(new RemoveTransitionAction(e as Transition, false));
					}
				}

				foreach(var e in selected) {
					actions.Add(new ChangeParentAction(e, macro));

					if(e is Comment) {
						actions.Add(new AddCommentAction(e as Comment));
					}
					else if(e is State) {
						actions.Add(new AddStateAction(e as State));
					}
					else if(e is Transition) {
						actions.Add(new AddTransitionAction(e as Transition, false));
					}

					if(!(e is Transition)) {
						actions.Add(new MoveAction(e, new Cairo.PointD(-pos.X + 50, -pos.Y + 50), true));
					}
				}

				actions.Add(new AddStateAction(macro));

				_document.PostAction(new GuiActionList(actions, Configuration.GetLocalized("Regrouper dans une macro")));
			}
		}

		public GuiAction RemoveSelection() {
			var states = new List<State>();
			var comments = new List<Comment>();
			var transitions = new HashSet<Transition>();
			foreach(var e in _document.Window.EditorGui.View.SelectedEntities) {
				if(e is State) {
					if(!(e is ExitPoint)) { // Do not erase exit point!
						states.Add(e as State);
					}

					// Removes all transitions attached to the deleted states
					foreach(var t in (e as State).TransitionsAfter) {
						transitions.Add(t);
					}
					foreach(var t in (e as State).TransitionsBefore) {
						transitions.Add(t);
					}
				}
				else if(e is Transition) {
					transitions.Add(e as Transition);
				}
				else if(e is Comment) {
					comments.Add(e as Comment);
				}
			}

			var deleteEntities = new List<GuiAction>();
			foreach(var t in transitions) {
				deleteEntities.Add(new RemoveTransitionAction(t, t.After.RequiredTokens == t.After.TransitionsBefore.Count));
			}
			foreach(State s in states) {
				deleteEntities.Add(new RemoveStateAction(s));
			}
			foreach(Comment c in comments) {
				deleteEntities.Add(new RemoveCommentAction(c));
			}

			_document.Window.EditorGui.View.ResetSelection();

			return new GuiActionList(deleteEntities, Configuration.GetLocalized("Supprimer les entités"));
		}

		public override void SelectAll() {
			var selected = _document.Window.EditorGui.View.SelectedEntities;
			selected.Clear();
			foreach(var s in _document.Window.EditorGui.View.CurrentPetriNet.States) {
				selected.Add(s);
			}
			foreach(var t in _document.Window.EditorGui.View.CurrentPetriNet.Transitions) {
				selected.Add(t);
			}
			foreach(var c in _document.Window.EditorGui.View.CurrentPetriNet.Comments) {
				selected.Add(c);
			}
			_document.Window.EditorGui.View.Redraw();

			UpdateSelection();
		}

		public void UpdateSelection() {
			_document.UpdateMenuItems();
			if(_document.Window.EditorGui.View.SelectedEntities.Count == 1) {
				this.EditedObject = _document.Window.EditorGui.View.SelectedEntity;
			}
			else {
				this.EditedObject = null;
			}
		}

		public override void UpdateMenuItems() {
			_document.Window.CopyItem.Sensitive = _document.Window.EditorGui.View.SelectedEntities.Count > 0;
			_document.Window.CutItem.Sensitive = _document.Window.EditorGui.View.SelectedEntities.Count > 0;
			_document.Window.PasteItem.Sensitive = MainClass.Clipboard.Count > 0;
			_document.Window.EmbedItem.Sensitive = _document.Window.EditorGui.View.SelectedEntities.Count > 0;
		}

		public EntityEditor EntityEditor {
			get;
			private set;
		}

		public Entity EditedObject {
			get {
				return EntityEditor.Entity;
			}
			set {
				if(EntityEditor.Entity != value) {
					EntityEditor = EntityEditor.GetEditor(value, _document);
				}
			}
		}
					
		private GuiAction PasteAction() {
			var actionList = new List<GuiAction>();

			var newEntities = this.CloneEntities(MainClass.Clipboard, _document);
			var states = from e in newEntities
						 where e is State
						 select (e as State);
			var transitions = new HashSet<Transition>(from e in newEntities
				                                      where e is Transition
				                                      select (e as Transition));
			var comments = from e in newEntities
						   where e is Comment
				           select (e as Comment);

			foreach(State s in states) {
				// Change entity's owner
				s.Parent = _document.Window.EditorGui.View.CurrentPetriNet;
				s.Name = s.Name;
				s.Position = new Cairo.PointD(s.Position.X + 20 * MainClass.PasteCount, s.Position.Y + 20 * MainClass.PasteCount);
				actionList.Add(new AddStateAction(s));
			}
			foreach(Comment c in comments) {
				// Change entity's owner
				c.Parent = _document.Window.EditorGui.View.CurrentPetriNet;
				c.Position = new Cairo.PointD(c.Position.X + 20 * MainClass.PasteCount, c.Position.Y + 20 * MainClass.PasteCount);
				actionList.Add(new AddCommentAction(c));
			}

			foreach(Transition t in transitions) {
				// Change entity's owner
				t.Parent = _document.Window.EditorGui.View.CurrentPetriNet;
				t.Name = t.Name;
				actionList.Add(new AddTransitionAction(t, false));
			}

			return new GuiActionList(actionList, Configuration.GetLocalized("Coller les entités"));
		}

		private List<Entity> CloneEntities(IEnumerable<Entity> entities, Document destination) {
			var cloned = new List<Entity>();

			var states = from e in entities
						 where (e is State && !(e is ExitPoint))
			             select (e as State);
			var comments = from e in entities
				           where (e is Comment)
						   select (e as Comment);

			var transitions = new List<Transition>(from e in entities
			                                       where e is Transition
			                                       select (e as Transition));

			// We cannot clone a transition without its 2 ends being cloned too
			transitions.RemoveAll(t => !states.Contains(t.After) || !states.Contains(t.Before));

			// Basic cloning strategy: serialization/deserialization to XElement, with the save/restore mechanism
			var statesTable = new Dictionary<UInt64, State>();
			foreach(State s in states) {
				var xml = s.GetXml();
				var newState = Entity.EntityFromXml(_document, xml, _document.Window.EditorGui.View.CurrentPetriNet, null) as State;
				statesTable.Add(newState.ID, newState);
			}
			foreach(Comment c in comments) {
				var xml = c.GetXml();
				var newComment = Entity.EntityFromXml(_document, xml, _document.Window.EditorGui.View.CurrentPetriNet, null) as Comment;
				newComment.Document = destination;
				newComment.ID = destination.LastEntityID++;
				cloned.Add(newComment);
			}

			foreach(Transition t in transitions) {
				var xml = t.GetXml();
				Transition newTransition = (Transition)Entity.EntityFromXml(destination, xml, _document.Window.EditorGui.View.CurrentPetriNet, statesTable);

				// Reassigning an ID to the transitions to keep a unique one for each entity
				newTransition.ID = _document.LastEntityID++;
				cloned.Add(newTransition);

				newTransition.Before.AddTransitionAfter(t);
				newTransition.After.AddTransitionBefore(t);
			}

			foreach(State s in statesTable.Values) {
				// Same as with the transitions. Could not do that before, as we needed the ID to remain the same for the states for the deserialization to work
				UpdateID(s, destination);
				cloned.Add(s);

				// If some transitions were removed as they didn't fully belong to the cloned set, we have to take account of the posibly too big required tokens count.
				// Didn't think of a better way than this.
				if(s.RequiredTokens > s.TransitionsBefore.Count) {
					s.RequiredTokens = s.TransitionsBefore.Count;
				}
			}

			return cloned;
		}

		public override void ManageFocus(object focus) {
			if(focus is List<Entity>) {
				_document.Window.EditorGui.View.SelectedEntities.Clear();
				_document.Window.EditorGui.View.SelectedEntities.UnionWith(focus as List<Entity>);
				this.UpdateSelection();
			}
			else
				_document.Window.EditorGui.View.SelectedEntity = focus as Entity;
		}

		private static void UpdateID(State s, Document d) {
			s.Document = d;
			s.ID = s.Document.LastEntityID++;
			var ss = s as PetriNet;
			if(ss != null) {
				foreach(Comment c in ss.Comments) {
					c.Document = d;
					c.ID = c.Document.LastEntityID++;
				}
				foreach(Transition t in ss.Transitions) {
					t.Document = d;
					t.ID = t.Document.LastEntityID++;
				}
				foreach(State s2 in ss.States) {
					UpdateID(s2, d);
				}
			}
		}

		Document _document;
	}
}

