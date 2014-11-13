using System;
using System.Collections.Generic;

namespace Statechart
{
	public class UndoManager
	{
		public UndoManager()
		{
			undoStack = new Stack<ActionDescription>();
			redoStack = new Stack<ActionDescription>();
		}

		public void PostAction(GuiAction a) {
			redoStack.Clear();
			undoStack.Push(new ActionDescription(a, a.Description));
			a.Apply();
		}

		public void Undo() {
			this.SwapAndApply(undoStack, redoStack);
		}

		public void Redo() {
			this.SwapAndApply(redoStack, undoStack);
		}

		public GuiAction NextUndo {
			get {
				if(undoStack.Count > 0)
					return undoStack.Peek().action;
				else
					return null;
			}
		}

		public string NextUndoDescription {
			get {
				return undoStack.Peek().description;
			}
		}

		public GuiAction NextRedo {
			get {
				if(redoStack.Count > 0)
					return redoStack.Peek().action;
				else
					return null;
			}
		}

		public string NextRedoDescription {
			get {
				return redoStack.Peek().description;
			}
		}

		private struct ActionDescription {
			public ActionDescription(GuiAction a, string d) {
				action = a;
				description = d;
			}

			public GuiAction action;
			public string description;
		}

		private void SwapAndApply(Stack<ActionDescription> toPop, Stack<ActionDescription> toPush) {
			if(toPop.Count > 0) {
				var actionDescription = toPop.Pop();
				actionDescription.action = actionDescription.action.Reverse();
				toPush.Push(new ActionDescription(actionDescription.action, actionDescription.description));
				actionDescription.action.Apply();
			}
		}

		Stack<ActionDescription> undoStack;
		Stack<ActionDescription> redoStack;
	}

	public abstract class GuiAction {
		public abstract void Apply();
		public abstract GuiAction Reverse();
		public abstract object Focus {
			get;
		}
		public abstract string Description {
			get;
		}
	}

	public class GuiActionWrapper : GuiAction {
		public GuiActionWrapper(GuiAction a, string description) {
			action = a;
			this.description = description;
		}

		public override void Apply() {
			action.Apply();
		}

		public override GuiAction Reverse() {
			return new GuiActionWrapper(action.Reverse(), description);
		}

		public override object Focus {
			get {
				return action.Focus;
			}
		}

		public override string Description {
			get {
				return description;
			}
		}

		GuiAction action;
		string description;
	}

	public class GuiActionList : GuiAction {
		public GuiActionList(IEnumerable<GuiAction> a, string description) {
			actions = new List<GuiAction>(a);
			this.description = description;

			// Strange use case anyway
			if(actions.Count == 0) {
				throw new ArgumentException("The action list is empty!");
			}
		}

		public override void Apply() {
			foreach(var a in actions) {
				a.Apply();
			}
		}

		public override GuiAction Reverse() {
			var l = new List<GuiAction>();
			foreach(var a in actions) {
				l.Insert(0, a.Reverse());
			}

			return new GuiActionList(l, description);
		}

		public override object Focus {
			get {
				var l = new List<Entity>();
				foreach(var a in actions) {
					var f = a.Focus;
					if(f is List<Entity>)
						l.AddRange(f as List<Entity>);
					else
						l.Add(f as Entity);
				}

				return l;
			}
		}

		public override string Description {
			get {
				return description;
			}
		}

		List<GuiAction> actions;
		string description;
	}

	public class ChangeNameAction : GuiAction {
		public ChangeNameAction(Entity e, string newName) {
			entity = e;
			this.newName = newName;
			this.oldName = entity.Name;
		}

		public override void Apply() {
			entity.Name = newName;
		}

		public override GuiAction Reverse() {
			return new ChangeNameAction(entity, oldName); 
		}

		public override object Focus {
			get {
				return entity;
			}
		}

		public override string Description {
			get {
				return "Changer le nom";
			}
		}

		Entity entity;
		string newName;
		string oldName;
	}

	public class ChangeRequiredTokensAction : GuiAction {
		public ChangeRequiredTokensAction(State s, int newCount) {
			state = s;
			this.newCount = newCount;
			this.oldCount = s.RequiredTokens;
		}

		public override void Apply() {
			state.RequiredTokens = newCount;
		}

		public override GuiAction Reverse() {
			return new ChangeRequiredTokensAction(state, oldCount); 
		}

		public override object Focus {
			get {
				return state;
			}
		}

		public override string Description {
			get {
				return "Changer le nom";
			}
		}

		State state;
		int newCount;
		int oldCount;
	}

	public class MoveAction : GuiAction {
		public MoveAction(Entity e, Cairo.PointD delta) {
			entity = e;
			this.delta = new Cairo.PointD(delta.X, delta.Y);
		}

		public override void Apply() {
			entity.Position = new Cairo.PointD(entity.Position.X + delta.X, entity.Position.Y + delta.Y);
		}

		public override GuiAction Reverse() {
			return new MoveAction(entity, new Cairo.PointD(-delta.X, -delta.Y)); 
		}

		public override object Focus {
			get {
				return entity;
			}
		}

		public override string Description {
			get {
				string desc = "Déplacer ";
				if(entity is State)
					desc += "l'état";
				else if(entity is Transition)
					desc += "la transition";
				else // Too lazy to search for a counter example but should probably never happen
					desc += "l'entité";

				return desc;
			}
		}

		Entity entity;
		Cairo.PointD delta;
	}

	public class ToggleActiveAction : GuiAction {
		public ToggleActiveAction(State e) {
			entity = e;
		}

		public override void Apply() {
			entity.Active = !entity.Active;
		}

		public override GuiAction Reverse() {
			return new ToggleActiveAction(entity); 
		}

		public override object Focus {
			get {
				return entity;
			}
		}

		public override string Description {
			get {
				return "Modifier l'état";
			}
		}

		State entity;
	}

	public class ConditionChangeAction : GuiAction {
		public ConditionChangeAction(Transition t, ConditionBase newCondition) {
			transition = t;
			oldCondition = t.Condition;
			this.newCondition = newCondition;
		}

		public override void Apply() {
			transition.Condition = newCondition;
		}

		public override GuiAction Reverse() {
			return new ConditionChangeAction(transition, oldCondition); 
		}

		public override object Focus {
			get {
				return transition;
			}
		}

		public override string Description {
			get {
				return "Modifier la condition";
			}
		}

		Transition transition;
		ConditionBase newCondition, oldCondition;
	}

	public class InvokationChangeAction : GuiAction {
		public InvokationChangeAction(Action a, Cpp.FunctionInvokation i) {
			action = a;
			oldInvokation = a.Function;
			this.newInvokation = i;
		}

		public override void Apply() {
			action.Function = newInvokation;
		}

		public override GuiAction Reverse() {
			return new InvokationChangeAction(action, oldInvokation); 
		}

		public override object Focus {
			get {
				return action;
			}
		}

		public override string Description {
			get {
				return "Modifier la fonction";
			}
		}

		Action action;
		Cpp.FunctionInvokation newInvokation, oldInvokation;
	}

	public class AddTransitionAction : GuiAction {
		public AddTransitionAction(Transition t, bool incrementTokenCount) {
			this.transition = t;
			this.incrementTokenCount = incrementTokenCount;
		}

		public override void Apply() {
			transition.Before.Parent.AddTransition(transition);

			transition.Before.AddTransitionAfter(transition);
			transition.After.AddTransitionBefore(transition);
			if(incrementTokenCount)
				++transition.After.RequiredTokens;
		}

		public override GuiAction Reverse() {
			return new RemoveTransitionAction(transition, incrementTokenCount); 
		}

		public override object Focus {
			get {
				return transition;
			}
		}

		public override string Description {
			get {
				return "Ajouter une transition";
			}
		}

		Transition transition;
		bool incrementTokenCount;
	}

	public class RemoveTransitionAction : GuiAction {
		public RemoveTransitionAction(Transition t, bool decrementTokenCount) {
			this.transition = t;
			this.decrementTokenCount = decrementTokenCount;
		}

		public override void Apply() {
			if(decrementTokenCount) {
				--transition.After.RequiredTokens;
			}
			transition.Before.Parent.RemoveTransition(transition);
		}

		public override GuiAction Reverse() {
			return new AddTransitionAction(transition, decrementTokenCount); 
		}

		public override object Focus {
			get {
				return transition.Parent;
			}
		}

		public override string Description {
			get {
				return "Supprimer la transition";
			}
		}

		Transition transition;
		bool decrementTokenCount;
	}

	public class AddStateAction : GuiAction {
		public AddStateAction(State s) {
			state = s;
		}

		public override void Apply() {
			state.Parent.AddState(state);
		}

		public override GuiAction Reverse() {
			return new RemoveStateAction(state); 
		}

		public override object Focus {
			get {
				return state;
			}
		}

		public override string Description {
			get {
				return "Ajouter un état";
			}
		}

		State state;
	}

	public class RemoveStateAction : GuiAction {
		public RemoveStateAction(State s) {
			state = s;
		}

		public override void Apply() {
			state.Parent.RemoveState(state);
		}

		public override GuiAction Reverse() {
			return new AddStateAction(state); 
		}

		public override object Focus {
			get {
				return state.Parent;
			}
		}

		public override string Description {
			get {
				return "Supprimer l'état";
			}
		}

		State state;
	}
		
	public class DoNothingAction : GuiAction {
		public DoNothingAction(Entity e) {
			entity = e;
		}

		public override void Apply() {}

		public override GuiAction Reverse() {
			return new DoNothingAction(entity); 
		}

		public override object Focus {
			get {
				return entity;
			}
		}

		public override string Description {
			get {
				return "";
			}
		}

		Entity entity;
	}
}

