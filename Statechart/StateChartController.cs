using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;

namespace Statechart
{
	public class StateChartController
	{
		public StateChartController(Document doc) {
			document = doc;
			this.StateChart = null;
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
			if(StateChart.UsesHeader(header))
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

		public RootStateChart StateChart {
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
			foreach(var s in document.Window.Drawing.EditedStateChart.States) {
				selected.Add(s);
			}
			foreach(var t in document.Window.Drawing.EditedStateChart.Transitions) {
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
				return editedObject;
			}
			set {
				PostAction(ModifLocation.EntityChange, null);

				editedObject = value;

				this.UpdateMenuItems();

				var objectList = new List<Widget>();
				var objectIndentation = new List<int>();
				if(!document.Window.Drawing.MultipleSelection && value != null) {
					var label = CreateWidget<Label>(objectList, objectIndentation, 0, "ID de l'entité : " + value.ID.ToString());
					label.Markup = "<span color=\"grey\">" + label.Text + "</span>";
				}
				if(value is Action) {
					Action a = (Action)value;

					CreateWidget<Label>(objectList, objectIndentation, 0, "Nom de l'action :");
					var name = CreateWidget<Entry>(objectList, objectIndentation, 0, a.Name);
					name.Changed += (obj, eventInfo) => {
						PostAction(ModifLocation.Name, new ChangeNameAction(a, (obj as Entry).Text));
					};

					var active = CreateWidget<CheckButton>(objectList, objectIndentation, 0, "Active à t = 0 :");
					active.Active = a.Active;
					active.Toggled += (sender, e) => {
						PostAction(ModifLocation.Active, new ToggleActiveAction(a));
					};

					if(a.TransitionsBefore.Count > 0) {
						CreateWidget<Label>(objectList, objectIndentation, 0, "Jetons requis pour entrer dans l'action :");
						var list = new List<string>();
						for(int i = 0; i < a.TransitionsBefore.Count; ++i) {
							list.Add((i + 1).ToString());
						}

						ComboBox tokensChoice = ComboHelper(list[a.RequiredTokens - 1], list);
						objectList.Add(tokensChoice);
						objectIndentation.Add(0);

						tokensChoice.Changed += (object sender, EventArgs e) => {
							ComboBox combo = sender as ComboBox;

							TreeIter iter;

							if(combo.GetActiveIter(out iter)) {
								var val = combo.Model.GetValue(iter, 0) as string;
								int nbTok = int.Parse(val);
								PostAction(ModifLocation.RequiredTokens, new ChangeRequiredTokensAction(a, nbTok));
							}
						};
					}
					// Manage C++ function
					{
						var editorFields = new List<Widget>();

						CreateWidget<Label>(objectList, objectIndentation, 0, "Action associée :");

						var list = new List<string>();
						string defaultFunction = "Afficher ID + Nom action";
						string manual = ("Manuel…");
						list.Add(defaultFunction);
						list.Add(manual);
						foreach(var func in cppActions) {
							if(func.Name != "defaultAction" && func.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope())))
								list.Add(func.Signature);
						}
						string activeFunction = a.IsDefault() ? defaultFunction : (list.Contains(a.Function.Function.Signature) ? a.Function.Function.Signature : manual);


						ComboBox funcList = ComboHelper(activeFunction, list);
						objectList.Add(funcList);
						objectIndentation.Add(0);
						funcList.Changed += (object sender, EventArgs e) => {
							ComboBox combo = sender as ComboBox;

							TreeIter iter;

							if(combo.GetActiveIter(out iter)) {
								var val = combo.Model.GetValue(iter, 0) as string;
								if(val == defaultFunction) {
									a.Function = a.DefaultAction();
								}
								else if(val == manual) {
									EditInvocation(a, val == manual, objectList, objectIndentation, editorFields);
								}
								else {
									var f = cppActions.Find(delegate(Cpp.Function ff) {
										return ff.Signature == val;
									});

									if(a.Function.Function != f) {
										var pp = new List<Cpp.Expression>();
										for(int i = 0; i < f.Parameters.Count; ++i) {
											pp.Add(new Cpp.EmptyExpression());
										}
										Cpp.FunctionInvocation invocation;
										if(f is Cpp.Method) {
											invocation = new Cpp.MethodInvocation(f as Cpp.Method, new Cpp.EmptyExpression(), false, pp.ToArray());
										}
										else {
											invocation = new Cpp.FunctionInvocation(f, pp.ToArray());
										}
										PostAction(ModifLocation.Function, new InvocationChangeAction(a, invocation));
										EditInvocation(a, val == manual, objectList, objectIndentation, editorFields);
									}
								}
							}
						};

						EditInvocation(a, activeFunction == manual, objectList, objectIndentation, editorFields);
					}
				}
				else if(value is InnerStateChart) {
					InnerStateChart a = value as InnerStateChart;

					CreateWidget<Label>(objectList, objectIndentation, 0, "Nom du graphe :");
					var name = CreateWidget<Entry>(objectList, objectIndentation, 0, a.Name);
					name.Changed += (obj, eventInfo) => {
						PostAction(ModifLocation.Name, new ChangeNameAction(a, (obj as Entry).Text));
					};

					var active = CreateWidget<CheckButton>(objectList, objectIndentation, 0, "Actif à t = 0 :");
					active.Active = a.Active;
					active.Toggled += (sender, e) => {
						PostAction(ModifLocation.Active, new ToggleActiveAction(a));
					};
				}
				else if(value is ExitPoint) {
					CreateWidget<Label>(objectList, objectIndentation, 0, "Sortie du graphe");
				}
				else if(value is Transition) {
					Transition t = (Transition)value;

					CreateWidget<Label>(objectList, objectIndentation, 0, "Nom de la transition :");
					var name = CreateWidget<Entry>(objectList, objectIndentation, 0, t.Name);
					name.Changed += (obj, eventInfo) => {
						PostAction(ModifLocation.Name, new ChangeNameAction(t, (obj as Entry).Text));
					};

					CreateWidget<Label>(objectList, objectIndentation, 0, "Condition de la transition :");
					var condition = CreateWidget<Entry>(objectList, objectIndentation, 0, t.Condition.MakeUserReadable());
					condition.FocusOutEvent += (obj, eventInfo) => {
						try {
							var cond = new ConditionChangeAction(t, ConditionBase.ConditionFromString((obj as Entry).Text, t, AllFunctions));
							PostAction(ModifLocation.Condition, cond);
						}
						catch(Exception e) {
							MessageDialog d = new MessageDialog(document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "La condition spécifiée est invalide (" + e.Message + ").");
							d.AddButton("Annuler", ResponseType.Cancel);
							d.Run();
							d.Destroy();

							(obj as Entry).Text = t.Condition.MakeUserReadable();
						}
					};
				}
				else if(document.Window.Drawing.MultipleSelection) {
					CreateWidget<Label>(objectList, objectIndentation, 0, "Sélectionnez un seul objet");
				}
				else {
					CreateWidget<Label>(objectList, objectIndentation, 0, "Sélectionnez un objet à modifier");
				}

				FormatAndShow(objectList, objectIndentation);

				document.Window.Drawing.Redraw();
			}
		}

		private enum ModifLocation {None, EntityChange, Name, RequiredTokens, Active, Function, Condition, Param0}; 
	
		private void EditInvocation(Action a, bool manual, List<Widget> objectList, List<int> objectIndentation, List<Widget> editorFields) {
			foreach(var e in editorFields) {
				int i = objectList.IndexOf(e);
				objectList.Remove(e);
				objectIndentation.RemoveAt(i);
			}
			editorFields.Clear();

			if(!a.IsDefault()) {
				if(manual) {
					CreateWidget<Label>(objectList, objectIndentation, 0, "Invocation de l'action :");
					var invocation = CreateWidget<Entry>(objectList, objectIndentation, 0, a.Function.MakeUserReadable());
					editorFields.Add(invocation);
					invocation.FocusOutEvent += (obj, eventInfo) => {
						Cpp.FunctionInvocation funcInvocation = null;
						try {
							funcInvocation = Cpp.Expression.CreateFromString<Cpp.FunctionInvocation>((obj as Entry).Text, a, this.AllFunctions);
							if(!funcInvocation.Function.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()))) {
								throw new Exception("Type de retour de la fonction incorrect : ResultatAction attendu, " + funcInvocation.Function.ReturnType.ToString() + " trouvé.");
							}
							PostAction(ModifLocation.Function, new InvocationChangeAction(a, funcInvocation));
						}
						catch(Exception ex) {
							MessageDialog d = new MessageDialog(document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "L'expression spécifiée est invalide (" + ex.Message + ").");
							d.AddButton("Annuler", ResponseType.Cancel);
							d.Run();
							d.Destroy();

							(obj as Entry).Text = a.Function.MakeUserReadable();
						}
					};
				}
				else {
					if(a.Function.Function is Cpp.Method) {
						var method = a.Function as Cpp.MethodInvocation;
						var editorHeader = CreateWidget<Label>(objectList, objectIndentation, 20, "Objet *this de type " + method.Function.Enclosing.ToString() + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(objectList, objectIndentation, 20, method.This.MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = 2; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, this.AllFunctions));
								}
							}
							PostAction(ModifLocation.Function, new InvocationChangeAction(a, new Cpp.MethodInvocation(method.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, this.AllFunctions), false, args.ToArray())));
						};
					}
					for(int i = 0; i < a.Function.Function.Parameters.Count; ++i) {
						var p = a.Function.Function.Parameters[i];
						var editorHeader = CreateWidget<Label>(objectList, objectIndentation, 20, "Paramètre " + p.Type + " " + p.Name + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(objectList, objectIndentation, 20, a.Function.Arguments[i].MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = (a.Function.Function is Cpp.Method) ? 2 : 0; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, this.AllFunctions));
								}
							}
							Cpp.FunctionInvocation invocation;
							if(a.Function.Function is Cpp.Method) {
								invocation = new Cpp.MethodInvocation(a.Function.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, this.AllFunctions), false, args.ToArray());
							}
							else {
								invocation = new Cpp.FunctionInvocation(a.Function.Function, args.ToArray());
							}
							PostAction(ModifLocation.Function, new InvocationChangeAction(a, invocation));
						};
					}
				}
			}

			this.FormatAndShow(objectList, objectIndentation);
		}

		private ComboBox ComboHelper(string selectedItem, List<string> items)
		{
			var funcList = ComboBox.NewText();

			foreach(string func in items) {
				funcList.AppendText(func);
			}

			{
				TreeIter iter;
				funcList.Model.GetIterFirst(out iter);
				do {
					GLib.Value thisRow = new GLib.Value();
					funcList.Model.GetValue(iter, 0, ref thisRow);
					if((thisRow.Val as string).Equals(selectedItem)) {
						funcList.SetActiveIter(iter);
						break;
					}
				} while (funcList.Model.IterNext(ref iter));
			}

			return funcList;
		}

		private WidgetType CreateWidget<WidgetType>(List<Widget> list, List<int> indentList, int indentation, params object[] widgetConstructionArgs) where WidgetType : Widget
		{
			WidgetType w = (WidgetType)Activator.CreateInstance(typeof(WidgetType), widgetConstructionArgs);
			list.Add(w);
			indentList.Add(indentation);

			return w;
		}

		private void FormatAndShow(List<Widget> widgets, List<int> indent)
		{
			foreach(Widget w in document.Window.Editor.AllChildren) {
				document.Window.Editor.Remove(w);
			}

			int lastX = 20, lastY = 0;
			for(int i = 0; i < widgets.Count; ++i) {
				Widget w = widgets[i];
				document.Window.Editor.Add(w);
				if(w.GetType() == typeof(Label)) {
					lastY += 20;
				}
				if(w.GetType() == typeof(CheckButton)) {
					lastY += 10;
				}

				Fixed.FixedChild w2 = ((global::Gtk.Fixed.FixedChild)(document.Window.Editor[w]));
				w2.X = lastX + indent[i];
				w2.Y = lastY;

				lastY += w.Allocation.Height + 20;
				w.Show();
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

		// Here's a way to signal that we want all the previous GuiActions stored before to be posted as one.
		// The signal is done either when we edit another Entity, or when we edit another field of the current one.
		private void PostAction(ModifLocation location, GuiAction guiAction) {
			if(lastLocation == ModifLocation.None) {
				actions = new List<GuiAction>();
			}
			// If we already started adding some actions
			else if((location != lastLocation) && actions.Count > 0) {
				if(lastLocation != ModifLocation.EntityChange) {
					this.PostActions();
				}
			}
			lastLocation = location;

			if(guiAction == null) {
				if(location != ModifLocation.EntityChange)
					throw new ArgumentNullException("guiAction", "This parameter should be null only when location == ModifLocation.EntityChange!");
			}
			else {
				actions.Add(guiAction);
			}

			// FIXME: (maybe) causes a lot of undo/redo
			if(actions.Count > 0) {//location == ModifLocation.Active) {
				this.PostActions();
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
				s.Parent = this.document.Window.Drawing.EditedStateChart;
				s.Name = s.Name + " 2";
				s.Position = new Cairo.PointD(s.Position.X + 20, s.Position.Y + 20);
				actionList.Add(new AddStateAction(s));
			}

			foreach(Transition t in transitions) {
				// Change entity's owner
				t.Parent = this.document.Window.Drawing.EditedStateChart;
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
				var newState = Entity.EntityFromXml(document, xml, document.Window.Drawing.EditedStateChart, null) as State;
				statesTable.Add(newState.ID, newState);
			}

			foreach(Transition t in transitions) {
				var xml = t.GetXml();
				var newTransition = Entity.EntityFromXml(destination, xml, document.Window.Drawing.EditedStateChart, statesTable);

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

		private void PostActions() {
			var wrapper = new GuiActionList(actions, actions[0].Description);
			this.PostAction(wrapper);
			actions.Clear();
		}

		private static void UpdateID(State s, Document d) {
			s.Document = d;
			s.ID = s.Document.LastEntityID++;
			var ss = s as StateChart;
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
		Entity editedObject;
		List<Cpp.Function> allFunctions;
		List<Cpp.Function> cppActions;
		List<Cpp.Function> cppConditions;
		List<string> headers;
		UndoManager undoManager;
		GuiAction guiActionToMatchSave = null;
		bool modified = false;
		ModifLocation lastLocation = ModifLocation.None;
		List<GuiAction> actions;
	}
}

