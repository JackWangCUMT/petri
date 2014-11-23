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

			allFunctions = new List<Cpp.Function>();
			cppActions = new List<Cpp.Function>();
			cppConditions = new List<Cpp.Function>();

			var timeout = new Cpp.Function(new Cpp.Type("Timeout", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "Timeout", false);
			timeout.AddParam(new Cpp.Param(new Cpp.Type("std::chrono::duration<Rep, Period>", Cpp.Scope.EmptyScope()), "timeout"));
			cppConditions.Add(timeout);

			var defaultAction = Action.DefaultFunction();
			cppActions.Insert(0, defaultAction);
			allFunctions.Insert(0, defaultAction);

			editor = EntityEditor.GetEditor(null, doc);
			this.UpdateMenuItems();
		}

		public override void Copy() {
			if(document.Window.PetriView.SelectedEntities.Count > 0) {
				MainClass.Clipboard = new HashSet<Entity>(CloneEntities(document.Window.PetriView.SelectedEntities, document));

				this.UpdateMenuItems();
			}
		}

		public override void Paste() {
			if(MainClass.Clipboard.Count > 0) {
				var action = PasteAction();
				document.PostAction(action);

				var pasted = action.Focus as List<Entity>;
				document.Window.PetriView.SelectedEntities.Clear();
				document.Window.PetriView.SelectedEntities.UnionWith(pasted);
				this.UpdateSelection();
			}
		}

		public override void Cut() {
			if(document.Window.PetriView.SelectedEntities.Count > 0) {
				Copy();
				document.PostAction(new GuiActionWrapper(this.RemoveSelection(), "Couper les entités"));
			}
		}

		public GuiAction RemoveSelection() {
			var states = new List<State>();
			var transitions = new HashSet<Transition>();
			foreach(var e in document.Window.PetriView.SelectedEntities) {
				if(e is State) {
					if(!(e is ExitPoint)) {// Do not erase exit point!
						states.Add(e as State);
					}
					foreach(var t in (e as State).TransitionsAfter) {
						transitions.Add(t);
					}
					foreach(var t in (e as State).TransitionsBefore) {
						transitions.Add(t);
					}
				}
				else if(e is Transition)
					transitions.Add(e as Transition);
			}

			var deleteEntities = new List<GuiAction>();
			foreach(var t in transitions) {
				deleteEntities.Add(new RemoveTransitionAction(t, t.After.RequiredTokens == t.After.TransitionsBefore.Count));
			}
			foreach(State s in states) {
				deleteEntities.Add(new RemoveStateAction(s));
			}


			document.Window.PetriView.ResetSelection();

			return new GuiActionList(deleteEntities, "Supprimer les entités");
		}

		public override void SelectAll() {
			var selected = document.Window.PetriView.SelectedEntities;
			selected.Clear();
			foreach(var s in document.Window.PetriView.EditedPetriNet.States) {
				selected.Add(s);
			}
			foreach(var t in document.Window.PetriView.EditedPetriNet.Transitions) {
				selected.Add(t);
			}

			UpdateSelection();
		}

		public void UpdateSelection() {
			if(document.Window.PetriView.SelectedEntities.Count == 1) {
				this.EditedObject = document.Window.PetriView.SelectedEntity;
			}
			else {
				this.EditedObject = null;
			}
		}

		public override void UpdateMenuItems() {
			document.Window.CopyItem.Sensitive = document.Window.PetriView.SelectedEntities.Count > 0;
			document.Window.CutItem.Sensitive = document.Window.PetriView.SelectedEntities.Count > 0;
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

			foreach(State s in states) {
				// Change entity's owner
				s.Parent = this.document.Window.PetriView.EditedPetriNet;
				s.Name = s.Name + " 2";
				s.Position = new Cairo.PointD(s.Position.X + 20, s.Position.Y + 20);
				actionList.Add(new AddStateAction(s));
			}

			foreach(Transition t in transitions) {
				// Change entity's owner
				t.Parent = this.document.Window.PetriView.EditedPetriNet;
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
			             where e is State
			             select (e as State);
			var transitions = new List<Transition>(from e in entities
			                                       where e is Transition
			                                       select (e as Transition));

			// We cannot clone a transition without its 2 ends being cloned too
			transitions.RemoveAll(t => !states.Contains(t.After) || !states.Contains(t.Before));

			// Basic cloning strategy: serialization/deserialization to XElement, with the save/restore mechanism
			var statesTable = new Dictionary<UInt64, State>();
			foreach(State s in states) {
				var xml = s.GetXml();
				var newState = Entity.EntityFromXml(document, xml, document.Window.PetriView.EditedPetriNet, null) as State;
				statesTable.Add(newState.ID, newState);
			}

			foreach(Transition t in transitions) {
				var xml = t.GetXml();
				var newTransition = Entity.EntityFromXml(destination, xml, document.Window.PetriView.EditedPetriNet, statesTable);

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
				document.Window.PetriView.SelectedEntities.Clear();
				document.Window.PetriView.SelectedEntities.UnionWith(focus as List<Entity>);
				this.UpdateSelection();
			}
			else
				document.Window.PetriView.SelectedEntity = focus as Entity;
		}

		private static void UpdateID(State s, Document d) {
			s.Document = d;
			s.ID = s.Document.LastEntityID++;
			var ss = s as PetriNet;
			if(ss != null) {
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
		List<Cpp.Function> allFunctions;
		List<Cpp.Function> cppActions;
		List<Cpp.Function> cppConditions;
		bool modified = false;
	}
}

