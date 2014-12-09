﻿using System;
using System.Collections.Generic;
using Gtk;

namespace Petri
{
	public abstract class EntityEditor
	{
		public static EntityEditor GetEditor(Entity e, Document doc) {
			EntityEditor editor;

			if(e is Action) {
				editor = new ActionEditor(e as Action, doc);
			}
			else if(e is Transition) {
				editor = new TransitionEditor(e as Transition, doc);
			}
			else if(e is Comment) {
				editor = new CommentEditor(e as Comment, doc);
			}
			else if(e is InnerPetriNet) {
				editor = new InnerPetriNetEditor(e as InnerPetriNet, doc);
			}
			else if(e is ExitPoint) {
				editor = new ExitPointEditor(e as ExitPoint, doc);
			}
			else if(doc.Window.EditorGui.View.MultipleSelection) {
				editor = new MultipleEditor(doc);
			}
			else {
				editor = new EmptyEditor(doc);
			}

			editor.FormatAndShow();

			return editor;
		}

		public Entity Entity {
			get;
			private set;
		}

		protected EntityEditor(Entity e, Document doc) {
			Entity = e;
			_document = doc;

			this.PostAction(ModifLocation.EntityChange, null);

			if(!_document.Window.EditorGui.View.MultipleSelection && e != null) {
				var label = CreateWidget<Label>(_objectList, _objectIndentation, 0, "ID de l'entité : " + e.ID.ToString());
				label.Markup = "<span color=\"grey\">" + label.Text + "</span>";
			}
		}

		protected enum ModifLocation {None, EntityChange, Name, RequiredTokens, Active, Function, Condition, Param0}; 
		
		protected ComboBox ComboHelper(string selectedItem, List<string> items)
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

		protected WidgetType CreateWidget<WidgetType>(List<Widget> list, List<int> indentList, int indentation, params object[] widgetConstructionArgs) where WidgetType : Widget
		{
			WidgetType w = (WidgetType)Activator.CreateInstance(typeof(WidgetType), widgetConstructionArgs);
			list.Add(w);
			indentList.Add(indentation);

			return w;
		}

		protected void FormatAndShow()
		{
			foreach(Widget w in _document.Window.EditorGui.Editor.AllChildren) {
				_document.Window.EditorGui.Editor.Remove(w);
			}

			int lastX = 20, lastY = 0;
			for(int i = 0; i < _objectList.Count; ++i) {
				Widget w = _objectList[i];
				_document.Window.EditorGui.Editor.Add(w);
				if(w.GetType() == typeof(Label)) {
					lastY += 20;
				}
				if(w.GetType() == typeof(CheckButton)) {
					lastY += 10;
				}

				Fixed.FixedChild w2 = ((Fixed.FixedChild)(_document.Window.EditorGui.Editor[w]));
				w2.X = lastX + _objectIndentation[i];
				w2.Y = lastY;

				lastY += w.Allocation.Height + 20;
				w.Show();
			}

			_document.Window.EditorGui.View.Redraw();
		}

		// Here's a way to signal that we want all the previous GuiActions stored before to be posted as one.
		// The signal is done either when we edit another Entity, or when we edit another field of the current one.
		protected void PostAction(ModifLocation location, GuiAction guiAction) {
			if(_lastLocation == ModifLocation.None) {
				_actions = new List<GuiAction>();
			}
			// If we already started adding some actions
			else if((location != _lastLocation) && _actions.Count > 0) {
				if(_lastLocation != ModifLocation.EntityChange) {
					this.PostActions();
				}
			}
			_lastLocation = location;

			if(guiAction == null) {
				if(location != ModifLocation.EntityChange)
					throw new ArgumentNullException("guiAction", "This parameter should be null only when location == ModifLocation.EntityChange!");
			}
			else {
				_actions.Add(guiAction);
			}

			// FIXME: (maybe) causes a lot of undo/redo
			if(_actions.Count > 0) {//location == ModifLocation.Active) {
				this.PostActions();
			}
		}

		protected void PostActions() {
			var wrapper = new GuiActionList(_actions, _actions[0].Description);
			_document.PostAction(wrapper);
			_actions.Clear();
		}

		protected Document _document;
		ModifLocation _lastLocation = ModifLocation.None;
		List<GuiAction> _actions;
		protected List<Widget> _objectList = new List<Widget>();
		protected List<int> _objectIndentation = new List<int>();
	}

	public class ActionEditor : EntityEditor {
		public ActionEditor(Action a, Document doc) : base(a, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Nom de l'action :");
			var name = CreateWidget<Entry>(_objectList, _objectIndentation, 0, a.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(a, (obj as Entry).Text));
			};

			var active = CreateWidget<CheckButton>(_objectList, _objectIndentation, 0, "Active à t = 0 :");
			active.Active = a.Active;
			active.Toggled += (sender, e) => {
				this.PostAction(ModifLocation.Active, new ToggleActiveAction(a));
			};

			if(a.TransitionsBefore.Count > 0) {
				CreateWidget<Label>(_objectList, _objectIndentation, 0, "Jetons requis pour entrer dans l'action :");
				var list = new List<string>();
				for(int i = 0; i < a.TransitionsBefore.Count; ++i) {
					list.Add((i + 1).ToString());
				}

				ComboBox tokensChoice = ComboHelper(list[a.RequiredTokens - 1], list);
				_objectList.Add(tokensChoice);
				_objectIndentation.Add(0);

				tokensChoice.Changed += (object sender, EventArgs e) => {
					ComboBox combo = sender as ComboBox;

					TreeIter iter;

					if(combo.GetActiveIter(out iter)) {
						var val = combo.Model.GetValue(iter, 0) as string;
						int nbTok = int.Parse(val);
						this.PostAction(ModifLocation.RequiredTokens, new ChangeRequiredTokensAction(a, nbTok));
					}
				};
			}
			// Manage C++ function
			{
				var editorFields = new List<Widget>();

				CreateWidget<Label>(_objectList, _objectIndentation, 0, "Action associée :");

				var list = new List<string>();
				string defaultFunction = "Afficher ID + Nom action";
				string manual = "Manuel…";
				list.Add(defaultFunction);
				list.Add(manual);
				foreach(var func in _document.CppActions) {
					if(func.Name != "defaultAction" && func.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope())))
						list.Add(func.Signature);
				}
				string activeFunction = a.IsDefault() ? defaultFunction : (list.Contains(a.Function.Function.Signature) ? a.Function.Function.Signature : manual);


				ComboBox funcList = ComboHelper(activeFunction, list);
				_objectList.Add(funcList);
				_objectIndentation.Add(0);
				funcList.Changed += (object sender, EventArgs e) => {
					ComboBox combo = sender as ComboBox;

					TreeIter iter;

					if(combo.GetActiveIter(out iter)) {
						var val = combo.Model.GetValue(iter, 0) as string;
						if(val == defaultFunction) {
							a.Function = a.DefaultAction();
						}
						else if(val == manual) {
							EditInvocation(a, true, _objectList, _objectIndentation, editorFields);
						}
						else {
							var f = _document.CppActions.Find(delegate(Cpp.Function ff) {
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
								this.PostAction(ModifLocation.Function, new InvocationChangeAction(a, invocation));
								EditInvocation(a, false, _objectList, _objectIndentation, editorFields);
							}
							else {
								EditInvocation(a, false, _objectList, _objectIndentation, editorFields);
							}
						}
					}
				};

				EditInvocation(a, activeFunction == manual, _objectList, _objectIndentation, editorFields);
			}
		}

		private void EditInvocation(Action a, bool manual, List<Widget> objectList, List<int> objectIndentation, List<Widget> editorFields) {
			foreach(var e in editorFields) {
				int i = objectList.IndexOf(e);
				objectList.Remove(e);
				objectIndentation.RemoveAt(i);
			}
			editorFields.Clear();

			if(manual) {
				var label = CreateWidget<Label>(_objectList, _objectIndentation, 0, "Invocation de l'action :");
				editorFields.Add(label);
				var invocation = CreateWidget<Entry>(_objectList, _objectIndentation, 0, a.Function.MakeUserReadable());
				editorFields.Add(invocation);
				invocation.FocusOutEvent += (obj, eventInfo) => {
					Cpp.FunctionInvocation funcInvocation = null;
					try {
						funcInvocation = Cpp.Expression.CreateFromString<Cpp.FunctionInvocation>((obj as Entry).Text, a, _document.AllFunctions);
						if(!funcInvocation.Function.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()))) {
							throw new Exception("Type de retour de la fonction incorrect : ResultatAction attendu, " + funcInvocation.Function.ReturnType.ToString().Replace("&", "&amp;") + " trouvé.");
						}
						PostAction(ModifLocation.Function, new InvocationChangeAction(a, funcInvocation));
					}
					catch(Exception ex) {
						MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "L'expression spécifiée est invalide (" + ex.Message + ").");
						d.AddButton("Annuler", ResponseType.Cancel);
						d.Run();
						d.Destroy();

						(obj as Entry).Text = a.Function.MakeUserReadable();
					}
				};
			}
			else {
				if(!a.IsDefault()) {
					if(a.Function.Function is Cpp.Method) {
						var method = a.Function as Cpp.MethodInvocation;
						var editorHeader = CreateWidget<Label>(_objectList, _objectIndentation, 20, "Objet *this de type " + method.Function.Enclosing.ToString() + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(_objectList, _objectIndentation, 20, method.This.MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = 2; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, _document.AllFunctions));
								}
							}
							this.PostAction(ModifLocation.Function, new InvocationChangeAction(a, new Cpp.MethodInvocation(method.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, _document.AllFunctions), false, args.ToArray())));
						};
					}
					for(int i = 0; i < a.Function.Function.Parameters.Count; ++i) {
						var p = a.Function.Function.Parameters[i];
						var editorHeader = CreateWidget<Label>(_objectList, _objectIndentation, 20, "Paramètre " + p.Type + " " + p.Name + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(_objectList, _objectIndentation, 20, a.Function.Arguments[i].MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = (a.Function.Function is Cpp.Method) ? 2 : 0; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, _document.AllFunctions));
								}
							}
							Cpp.FunctionInvocation invocation;
							if(a.Function.Function is Cpp.Method) {
								invocation = new Cpp.MethodInvocation(a.Function.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, _document.AllFunctions), false, args.ToArray());
							}
							else {
								invocation = new Cpp.FunctionInvocation(a.Function.Function, args.ToArray());
							}
							this.PostAction(ModifLocation.Function, new InvocationChangeAction(a, invocation));
						};
					}
				}
			}

			this.FormatAndShow();
		}
	}

	public class CommentEditor : EntityEditor {
		public CommentEditor(Comment c, Document doc) : base(c, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Commentaire :");
			var buf = new TextBuffer(new TextTagTable());
			buf.Text = c.Name;
			var comment = new TextView(buf);
			comment.SetSizeRequest(200, 400);
			/*HBox box = new HBox(true, 0);

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.SetPolicy(PolicyType.Never, PolicyType.Never);

			var view = new Viewport()
			view.Add(comment);

			view.SizeRequested += (o, args) => {
				view.WidthRequest = 100;
				view.HeightRequest = 200;
			};

			scrolledWindow.Add(view);

			box.PackStart(scrolledWindow, false, false, 0);*/

			_objectList.Add(comment);
			_objectIndentation.Add(0);

			comment.FocusOutEvent += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(c, (obj as TextView).Buffer.Text));
			};
		}
	}

	public class TransitionEditor : EntityEditor {
		public TransitionEditor(Transition t, Document doc) : base(t, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Nom de la transition :");
			var name = CreateWidget<Entry>(_objectList, _objectIndentation, 0, t.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(t, (obj as Entry).Text));
			};

			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Condition de la transition :");
			var condition = CreateWidget<Entry>(_objectList, _objectIndentation, 0, t.Condition.MakeUserReadable());
			condition.FocusOutEvent += (obj, eventInfo) => {
				try {
					var cond = new ConditionChangeAction(t, ConditionBase.ConditionFromString((obj as Entry).Text, t, _document.AllFunctions));
					PostAction(ModifLocation.Condition, cond);
				}
				catch(Exception e) {
					MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, "La condition spécifiée est invalide (" + e.Message + ").");
					d.AddButton("Annuler", ResponseType.Cancel);
					d.Run();
					d.Destroy();

					(obj as Entry).Text = t.Condition.MakeUserReadable();
				}
			};
		}
	}

	public class InnerPetriNetEditor : EntityEditor {
		public InnerPetriNetEditor(InnerPetriNet i, Document doc) : base(i, doc)
		{
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Nom du graphe :");
			var name = CreateWidget<Entry>(_objectList, _objectIndentation, 0, i.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(i, (obj as Entry).Text));
			};

			var active = CreateWidget<CheckButton>(_objectList, _objectIndentation, 0, "Actif à t = 0 :");
			active.Active = i.Active;
			active.Toggled += (sender, e) => {
				this.PostAction(ModifLocation.Active, new ToggleActiveAction(i));
			};
		}
	}

	public class ExitPointEditor : EntityEditor {
		public ExitPointEditor(ExitPoint e, Document doc) : base(e, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Sortie du graphe");
		}
	}

	public class MultipleEditor : EntityEditor {
		public MultipleEditor(Document doc) : base(null, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Sélectionnez un seul objet");
		}
	}

	public class EmptyEditor : EntityEditor {
		public EmptyEditor(Document doc) : base(null, doc) {
			CreateWidget<Label>(_objectList, _objectIndentation, 0, "Sélectionnez un objet à modifier");
		}
	}

}

