using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	public class PetriController
	{
		public PetriController(Document doc) {
			document = doc;
			this.PetriNet = null;
			undoManager = new UndoManager();

			allFunctions = new List<Cpp.Function>();
			cppActions = new List<Cpp.Function>();
			cppConditions = new List<Cpp.Function>();

			var timeout = new Cpp.Function(new Cpp.Type("Timeout", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "Timeout", false);
			timeout.AddParam(new Cpp.Param(new Cpp.Type("std::chrono::duration<Rep, Period>", Cpp.Scope.EmptyScope()), "timeout"));
			cppConditions.Add(timeout);

			var defaultAction = new Cpp.Function(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "defaultAction", false);
			defaultAction.AddParam(new Cpp.Param(new Cpp.Type("Action *", Cpp.Scope.EmptyScope()), "action"));
			cppActions.Insert(0, defaultAction);

			this.headers = new List<string>();
			editor = EntityEditor.GetEditor(null, doc);
			this.UpdateMenuItems();
		}

		public void PostAction(GuiAction a) {
			Modified = true;
			undoManager.PostAction(a);
			UpdateUndo();
			document.Window.Drawing.Redraw();
		}

		public void Undo() {
			undoManager.Undo();
			var focus = undoManager.NextRedo.Focus;
			ManageFocus(focus);

			UpdateUndo();
			document.Window.Drawing.Redraw();
		}

		public void Redo() {
			undoManager.Redo();
			var focus = undoManager.NextUndo.Focus;
			ManageFocus(focus);

			UpdateUndo();
			document.Window.Drawing.Redraw();
		}

		public void AddHeader(string header) {
			if(header.Length == 0 || Headers.Contains(header))
				return;

			if(header.Length > 0) {
				var functions = Cpp.Parser.Parse(header);
				foreach(var func in functions) {
					if(func.ReturnType.Equals("ResultatAction")) {
						cppActions.Add(func);
					}
					else if(func.ReturnType.Equals("bool")) {
						cppConditions.Add(func);
					}
					allFunctions.Add(func);
				}
			}

			Headers.Add(header);

			Modified = true;
		}

		// Performs the removal if possible
		public bool RemoveHeader(string header) {
			if(PetriNet.UsesHeader(header))
				return false;

			CppActions.RemoveAll(a => a.Header == header);
			CppConditions.RemoveAll(c => c.Header == header);
			Headers.Remove(header);

			Modified = true;

			return true;
		}

		public List<Cpp.Function> CppConditions {
			get {
				return cppConditions;
			}
		}

		public List<Cpp.Function> AllFunctions {
			get {
				return allFunctions;
			}
		}

		public List<Cpp.Function> CppActions {
			get {
				return cppActions;
			}
		}

		public RootPetriNet PetriNet {
			get;
			set;
		}

		public bool Modified {
			get {
				return modified;
			}
			set {
				modified = value;
				if(value == true)
					this.document.Dirty = true;

				// We require the current undo stack to represent an unmodified state
				if(value == false) {
					guiActionToMatchSave = undoManager.NextUndo;
				}
			}
		}

		public List<string> Headers {
			get {
				return headers;
			}
		}

		public void Copy() {
			if(document.Window.Drawing.SelectedEntities.Count > 0) {
				MainClass.Clipboard = new HashSet<Entity>(CloneEntities(document.Window.Drawing.SelectedEntities, document));

				this.UpdateMenuItems();
			}
		}

		public void Paste() {
			if(MainClass.Clipboard.Count > 0) {
				var action = PasteAction();
				this.PostAction(action);

				var pasted = action.Focus as List<Entity>;
				document.Window.Drawing.SelectedEntities.Clear();
				document.Window.Drawing.SelectedEntities.UnionWith(pasted);
				this.UpdateSelection();
			}
		}

		public void Cut() {
			if(document.Window.Drawing.SelectedEntities.Count > 0) {
				Copy();
				this.PostAction(new GuiActionWrapper(this.RemoveSelection(), "Couper les entités"));
			}
		}

		public GuiAction RemoveSelection() {
			var states = new List<State>();
			var transitions = new HashSet<Transition>();
			foreach(var e in document.Window.Drawing.SelectedEntities) {
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


			document.Window.Drawing.ResetSelection();

			return new GuiActionList(deleteEntities, "Supprimer les entités");
		}

		public void SelectAll() {
			var selected = document.Window.Drawing.SelectedEntities;
			selected.Clear();
			foreach(var s in document.Window.Drawing.EditedPetriNet.States) {
				selected.Add(s);
			}
			foreach(var t in document.Window.Drawing.EditedPetriNet.Transitions) {
				selected.Add(t);
			}

			UpdateSelection();
		}

		public void UpdateSelection() {
			if(document.Window.Drawing.SelectedEntities.Count == 1) {
				this.EditedObject = document.Window.Drawing.SelectedEntity;
			}
			else {
				this.EditedObject = null;
			}
		}

		public void UpdateMenuItems() {
			document.Window.CopyItem.Sensitive = document.Window.Drawing.SelectedEntities.Count > 0;
			document.Window.CutItem.Sensitive = document.Window.Drawing.SelectedEntities.Count > 0;
			document.Window.PasteItem.Sensitive = MainClass.Clipboard.Count > 0;
			document.Window.UndoItem.Sensitive = undoManager.NextUndo != null;
			document.Window.RedoItem.Sensitive = undoManager.NextRedo != null;
		}

		public Entity EditedObject {
			get {
				return editor.Entity;
			}
			set {
				editor = EntityEditor.GetEditor(value, document);
			}
		}

		private void UpdateUndo() {
			// If we fall back to the state we consider unmodified, let it be considered so
			this.modified = this.undoManager.NextUndo != this.guiActionToMatchSave;

			document.Window.RevertItem.Sensitive = this.modified;

			this.UpdateMenuItems();

			(document.Window.UndoItem.Child as Label).Text = "Annuler" + (undoManager.NextUndo != null ? " " + undoManager.NextUndoDescription : "");
			(document.Window.RedoItem.Child as Label).Text = "Rétablir" + (undoManager.NextRedo != null ? " " + undoManager.NextRedoDescription : "");
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
				s.Parent = this.document.Window.Drawing.EditedPetriNet;
				s.Name = s.Name + " 2";
				s.Position = new Cairo.PointD(s.Position.X + 20, s.Position.Y + 20);
				actionList.Add(new AddStateAction(s));
			}

			foreach(Transition t in transitions) {
				// Change entity's owner
				t.Parent = this.document.Window.Drawing.EditedPetriNet;
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
				var newState = Entity.EntityFromXml(document, xml, document.Window.Drawing.EditedPetriNet, null) as State;
				statesTable.Add(newState.ID, newState);
			}

			foreach(Transition t in transitions) {
				var xml = t.GetXml();
				var newTransition = Entity.EntityFromXml(destination, xml, document.Window.Drawing.EditedPetriNet, statesTable);

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

		private void ManageFocus(object focus) {
			if(focus is List<Entity>) {
				document.Window.Drawing.SelectedEntities.Clear();
				document.Window.Drawing.SelectedEntities.UnionWith(focus as List<Entity>);
				this.UpdateSelection();
			}
			else
				document.Window.Drawing.SelectedEntity = focus as Entity;
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
		List<string> headers;
		UndoManager undoManager;
		GuiAction guiActionToMatchSave = null;
		bool modified = false;
	}
}

