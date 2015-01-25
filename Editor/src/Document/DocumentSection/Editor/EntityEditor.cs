using System;
using System.Collections.Generic;
using Gtk;

namespace Petri
{
	public abstract class EntityEditor : PaneEditor
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

		protected EntityEditor(Entity e, Document doc) : base(doc, doc.Window.EditorGui.Editor) {
			Entity = e;
			_document = doc;

			this.PostAction(ModifLocation.EntityChange, null);

			if(!_document.Window.EditorGui.View.MultipleSelection && e != null) {
				var label = CreateLabel(0, "ID de l'entité : " + e.ID.ToString());
				label.Markup = "<span color=\"grey\">" + label.Text + "</span>";
			}
		}

		protected enum ModifLocation {None, EntityChange, Name, RequiredTokens, Active, Function, Condition, Param0}; 

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

		ModifLocation _lastLocation = ModifLocation.None;
		List<GuiAction> _actions;
	}

	public class ActionEditor : EntityEditor {
		public ActionEditor(Action a, Document doc) : base(a, doc) {
			CreateLabel(0, "Nom de l'action :");
			var name = CreateWidget<Entry>(true, 0, a.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(a, (obj as Entry).Text));
			};

			var active = CreateWidget<CheckButton>(false, 0, "Active à t = 0 :");
			active.Active = a.Active;
			active.Toggled += (sender, e) => {
				this.PostAction(ModifLocation.Active, new ToggleActiveAction(a));
			};

			if(a.TransitionsBefore.Count > 0) {
				CreateLabel(0, "Jetons requis pour entrer dans l'action :");
				var list = new List<string>();
				for(int i = 0; i < a.TransitionsBefore.Count; ++i) {
					list.Add((i + 1).ToString());
				}

				ComboBox tokensChoice = ComboHelper(list[a.RequiredTokens - 1], list);
				this.AddWidget(tokensChoice, false, 0);

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

				CreateLabel(0, "Action associée :");

				var list = new List<string>();
				string defaultFunction = "Afficher ID + Nom action";
				string manual = "Manuel…";
				list.Add(defaultFunction);
				list.Add(manual);
				foreach(var func in _document.CppActions) {
					if(func.Name != "defaultAction" && func.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope())))
						list.Add(func.Signature);
				}
				string activeFunction = a.IsDefault() ? defaultFunction : (!a.Function.NeedsExpansion && list.Contains(a.Function.Function.Signature) ? a.Function.Function.Signature : manual);

				ComboBox funcList = ComboHelper(activeFunction, list);
				this.AddWidget(funcList, true, 0);
				funcList.Changed += (object sender, EventArgs e) => {
					ComboBox combo = sender as ComboBox;

					TreeIter iter;

					if(combo.GetActiveIter(out iter)) {
						var val = combo.Model.GetValue(iter, 0) as string;
						if(val == defaultFunction) {
							a.Function = a.DefaultAction();
						}
						else if(val == manual) {
							EditInvocation(a, true, editorFields);
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
								EditInvocation(a, false, editorFields);
							}
							else {
								EditInvocation(a, false, editorFields);
							}
						}
					}
				};

				EditInvocation(a, activeFunction == manual, editorFields);
			}
		}

		private void EditInvocation(Action a, bool manual, List<Widget> editorFields) {
			foreach(var e in editorFields) {
				_objectList.RemoveAll(((Tuple<Widget, int, bool> obj) => obj.Item1 == e));
			}
			editorFields.Clear();

			if(manual) {
				var label = CreateLabel(0, "Invocation de l'action :");
				editorFields.Add(label);
				string userReadable;
				if(a.Function.NeedsExpansion) {
					userReadable = a.Function.Unexpanded;
				}
				else {
					userReadable = a.Function.MakeUserReadable();
				}

				var invocation = CreateWidget<Entry>(true, 0, userReadable);
				editorFields.Add(invocation);
				invocation.FocusOutEvent += (obj, eventInfo) => {
					Cpp.FunctionInvocation funcInvocation = null;
					try {
						funcInvocation = Cpp.Expression.CreateFromString<Cpp.FunctionInvocation>((obj as Entry).Text, a, _document.AllFunctions, _document.CppMacros);
						if(!funcInvocation.Function.ReturnType.Equals(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()))) {
							throw new Exception("Type de retour de la fonction incorrect : ResultatAction attendu, " + funcInvocation.Function.ReturnType.ToString() + " trouvé.");
						}
						PostAction(ModifLocation.Function, new InvocationChangeAction(a, funcInvocation));
					}
					catch(Exception ex) {
						MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, MainClass.SafeMarkupFromString("L'expression spécifiée est invalide (" + ex.Message + ")."));
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
						var editorHeader = CreateLabel(20, "Objet *this de type " + method.Function.Enclosing.ToString() + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(true, 20, method.This.MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = 2; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, _document.AllFunctions, _document.CppMacros));
								}
							}
							this.PostAction(ModifLocation.Function, new InvocationChangeAction(a, new Cpp.MethodInvocation(method.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, _document.AllFunctions, _document.CppMacros), false, args.ToArray())));
						};
					}
					for(int i = 0; i < a.Function.Function.Parameters.Count; ++i) {
						var p = a.Function.Function.Parameters[i];
						var editorHeader = CreateLabel(20, "Paramètre " + p.Type + " " + p.Name + " :");
						editorFields.Add(editorHeader);

						var valueEditor = CreateWidget<Entry>(true, 20, a.Function.Arguments[i].MakeUserReadable());
						editorFields.Add(valueEditor);
						valueEditor.Changed += (obj, eventInfo) => {
							var args = new List<Cpp.Expression>();
							for(int j = (a.Function.Function is Cpp.Method) ? 2 : 0; j < editorFields.Count; ++j) {
								Widget w = editorFields[j];
								if(w.GetType() == typeof(Entry)) {
									if((w as Entry).Text == "this")
										args.Add(new Cpp.EntityExpression(a, "this"));
									else
										args.Add(Cpp.Expression.CreateFromString<Cpp.Expression>((w as Entry).Text, a, _document.AllFunctions, _document.CppMacros));
								}
							}
							Cpp.FunctionInvocation invocation;
							if(a.Function.Function is Cpp.Method) {
								invocation = new Cpp.MethodInvocation(a.Function.Function as Cpp.Method, Cpp.Expression.CreateFromString<Cpp.Expression>((editorFields[1] as Entry).Text, a, _document.AllFunctions, _document.CppMacros), false, args.ToArray());
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
			CreateLabel(0, "Couleur :");
			_colors = new List<Cairo.Color>();
			_colors.Add(new Cairo.Color(1, 1, 0.7));
			_colors.Add(new Cairo.Color(1, 0.7, 0.7));
			_colors.Add(new Cairo.Color(0.7, 1, 0.7));
			_colors.Add(new Cairo.Color(0.7, 0.7, 1));
			_colors.Add(new Cairo.Color(1, 0.7, 1));
			_colorNames = new List<String>();
			_colorNames.Add("Jaune");
			_colorNames.Add("Rouge");
			_colorNames.Add("Vert");
			_colorNames.Add("Bleu");
			_colorNames.Add("Rose");
			_colorNames.Add("Manuel…");

			int colorIndex = _colors.FindIndex(((Cairo.Color obj) => { return obj.R == c.Color.R && obj.G == c.Color.G && obj.B == c.Color.B; }));
			if(colorIndex == -1) {
				colorIndex = _colorNames.Count - 1;
			}

			var colorList = ComboHelper(_colorNames[colorIndex], _colorNames);
			colorList.Changed += (object sender, EventArgs e) => {
				ComboBox combo = sender as ComboBox;

				TreeIter iter;

				if(combo.GetActiveIter(out iter)) {
					var val = combo.Model.GetValue(iter, 0) as string;
					this.EditColor(c, val, true);
				}
			};
			this.AddWidget(colorList, false, 0);

			_button = new ColorButton();
			_button.ColorSet += (object sender, EventArgs e) => {
				var newColor = (sender as ColorButton).Color;
				this.PostAction(ModifLocation.None, new ChangeCommentColorAction(c, new Cairo.Color(newColor.Red / 65535.0, newColor.Green / 65535.0, newColor.Blue / 65535.0)));
			};

			this.EditColor(c, _colorNames[colorIndex], false);

			CreateLabel(0, "Commentaire :");

			var buf = new TextBuffer(new TextTagTable());
			buf.Text = c.Name;
			var comment = CreateWidget<TextView>(true, 0, buf);
			comment.SetSizeRequest(200, 400);
			comment.WrapMode = WrapMode.Word;

			comment.FocusOutEvent += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(c, (obj as TextView).Buffer.Text));
			};
		}

		protected void EditColor(Comment comment, string color, bool changed) {
			if(color == "Manuel…") {
				int index = _objectList.FindIndex(obj => { return obj.Item1 is Label && (obj.Item1 as Label).Text == "Couleur :"; });
				_objectList.Insert(index + 2, Tuple.Create(_button as Widget, 20, false));
				_button.Color = new Gdk.Color((byte)(comment.Color.R * 255), (byte)(comment.Color.G * 255), (byte)(comment.Color.B * 255));
			}
			else {
				int index = _objectList.FindIndex(obj => { return obj.Item1 == _button; });
				if(index != -1) {
					_objectList.RemoveAt(index);
				}

				this.PostAction(ModifLocation.None, new ChangeCommentColorAction(comment, _colors[_colorNames.IndexOf(color)]));
			}
			_document.Window.EditorGui.View.Redraw();
			this.FormatAndShow();
		}

		List<string> _colorNames;
		List<Cairo.Color> _colors;
		ColorButton _button;
	}

	public class TransitionEditor : EntityEditor {
		public TransitionEditor(Transition t, Document doc) : base(t, doc) {
			CreateLabel(0, "Nom de la transition :");
			var name = CreateWidget<Entry>(true, 0, t.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(t, (obj as Entry).Text));
			};

			CreateLabel(0, "Condition de la transition :");
			string userReadable;
			if(t.Condition is ExpressionCondition && ((ExpressionCondition)t.Condition).Expression.NeedsExpansion) {
				userReadable = ((ExpressionCondition)t.Condition).Expression.Unexpanded;
			}
			else {
				userReadable = t.Condition.MakeUserReadable();
			}
			var condition = CreateWidget<Entry>(true, 0, userReadable);
			condition.FocusOutEvent += (obj, eventInfo) => {
				try {
					var cond = new ConditionChangeAction(t, ConditionBase.ConditionFromString((obj as Entry).Text, t, _document.AllFunctions, _document.CppMacros));
					PostAction(ModifLocation.Condition, cond);
				}
				catch(Exception e) {
					MessageDialog d = new MessageDialog(_document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, MainClass.SafeMarkupFromString("La condition spécifiée est invalide (" + e.Message + ")."));
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
			CreateLabel(0, "Nom du graphe :");
			var name = CreateWidget<Entry>(true, 0, i.Name);
			name.Changed += (obj, eventInfo) => {
				this.PostAction(ModifLocation.Name, new ChangeNameAction(i, (obj as Entry).Text));
			};

			var active = CreateWidget<CheckButton>(false, 0, "Actif à t = 0 :");
			active.Active = i.Active;
			active.Toggled += (sender, e) => {
				this.PostAction(ModifLocation.Active, new ToggleActiveAction(i));
			};
		}
	}

	public class ExitPointEditor : EntityEditor {
		public ExitPointEditor(ExitPoint e, Document doc) : base(e, doc) {
			CreateLabel(0, "Sortie du graphe");
		}
	}

	public class MultipleEditor : EntityEditor {
		public MultipleEditor(Document doc) : base(null, doc) {
			CreateLabel(0, "Sélectionnez un seul objet");
		}
	}

	public class EmptyEditor : EntityEditor {
		public EmptyEditor(Document doc) : base(null, doc) {
			CreateLabel(0, "Sélectionnez un objet à modifier");
		}
	}

}

