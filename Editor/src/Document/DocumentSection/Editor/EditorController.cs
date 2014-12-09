using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	public class EditorController : Controller
	{
		public EditorController(Document doc) {
			document = doc;

			editor = EntityEditor.GetEditor(null, doc);
			this.UpdateMenuItems();
		}

		public override void Copy() {
			if(document.Window.EditorGui.View.SelectedEntities.Count > 0) {
				MainClass.Clipboard = new HashSet<Entity>(CloneEntities(document.Window.EditorGui.View.SelectedEntities, document));

				this.UpdateMenuItems();
			}
		}

		public override void Paste() {
			if(MainClass.Clipboard.Count > 0) {
				var action = PasteAction();
				document.PostAction(action);

				var pasted = action.Focus as List<Entity>;
				document.Window.EditorGui.View.SelectedEntities.Clear();
				document.Window.EditorGui.View.SelectedEntities.UnionWith(pasted);
				this.UpdateSelection();
			}
		}

		public override void Cut() {
			if(document.Window.EditorGui.View.SelectedEntities.Count > 0) {
				Copy();
				document.PostAction(new GuiActionWrapper(this.RemoveSelection(), "Couper les entités"));
			}
		}

		public GuiAction RemoveSelection() {
			var states = new List<State>();
			var comments = new List<Comment>();
			var transitions = new HashSet<Transition>();
			foreach(var e in document.Window.EditorGui.View.SelectedEntities) {
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

			document.Window.EditorGui.View.ResetSelection();

			return new GuiActionList(deleteEntities, "Supprimer les entités");
		}

		public override void SelectAll() {
			var selected = document.Window.EditorGui.View.SelectedEntities;
			selected.Clear();
			foreach(var s in document.Window.EditorGui.View.CurrentPetriNet.States) {
				selected.Add(s);
			}
			foreach(var t in document.Window.EditorGui.View.CurrentPetriNet.Transitions) {
				selected.Add(t);
			}
			foreach(var c in document.Window.EditorGui.View.CurrentPetriNet.Comments) {
				selected.Add(c);
			}

			UpdateSelection();
		}

		public void UpdateSelection() {
			document.UpdateMenuItems();
			if(document.Window.EditorGui.View.SelectedEntities.Count == 1) {
				this.EditedObject = document.Window.EditorGui.View.SelectedEntity;
			}
			else {
				this.EditedObject = null;
			}
		}

		public override void UpdateMenuItems() {
			document.Window.CopyItem.Sensitive = document.Window.EditorGui.View.SelectedEntities.Count > 0;
			document.Window.CutItem.Sensitive = document.Window.EditorGui.View.SelectedEntities.Count > 0;
			document.Window.PasteItem.Sensitive = MainClass.Clipboard.Count > 0;
		}

		public Entity EditedObject {
			get {
				return editor.Entity;
			}
			set {
				editor = EntityEditor.GetEditor(value, document);
			}
		}
					
		private GuiAction PasteAction() {
			var actionList = new List<GuiAction>();

			var newEntities = this.CloneEntities(MainClass.Clipboard, this.document);
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
				s.Parent = this.document.Window.EditorGui.View.CurrentPetriNet;
				s.Name = s.Name + " 2";
				s.Position = new Cairo.PointD(s.Position.X + 20, s.Position.Y + 20);
				actionList.Add(new AddStateAction(s));
			}
			foreach(Comment c in comments) {
				// Change entity's owner
				c.Parent = this.document.Window.EditorGui.View.CurrentPetriNet;
				c.Position = new Cairo.PointD(c.Position.X + 20, c.Position.Y + 20);
				actionList.Add(new AddCommentAction(c));
			}

			foreach(Transition t in transitions) {
				// Change entity's owner
				t.Parent = this.document.Window.EditorGui.View.CurrentPetriNet;
				t.Name = t.Name + " 2";
				actionList.Add(new DoNothingAction(t)); // To select the newly pasted transitions
			}

			foreach(Transition t in transitions) {
				//t.Position = new Cairo.PointD(t.Position.X + 20, t.Position.Y + 20);
				actionList.Add(new AddTransitionAction(t, false));
			}

			return new GuiActionList(actionList, "Coller les entités");
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
				var newState = Entity.EntityFromXml(document, xml, document.Window.EditorGui.View.CurrentPetriNet, null) as State;
				statesTable.Add(newState.ID, newState);
			}
			foreach(Comment c in comments) {
				var xml = c.GetXml();
				var newComment = Entity.EntityFromXml(document, xml, document.Window.EditorGui.View.CurrentPetriNet, null) as Comment;
				newComment.Document = destination;
				newComment.ID = destination.LastEntityID++;
				cloned.Add(newComment);
			}

			foreach(Transition t in transitions) {
				var xml = t.GetXml();
				var newTransition = Entity.EntityFromXml(destination, xml, document.Window.EditorGui.View.CurrentPetriNet, statesTable);

				// Reassigning an ID to the transitions to keep a unique one for each entity
				newTransition.ID = document.LastEntityID++;
				cloned.Add(newTransition);
			}

			foreach(State s in statesTable.Values) {
				// Same as with the transitions. Could not do that before, as we needed the ID to remain the same for the states for the deserialization to work
				UpdateID(s, destination);
				cloned.Add(s);
			}

			return cloned;
		}

		public override void ManageFocus(object focus) {
			if(focus is List<Entity>) {
				document.Window.EditorGui.View.SelectedEntities.Clear();
				document.Window.EditorGui.View.SelectedEntities.UnionWith(focus as List<Entity>);
				this.UpdateSelection();
			}
			else
				document.Window.EditorGui.View.SelectedEntity = focus as Entity;
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

		Document document;
		EntityEditor editor;
	}
}

