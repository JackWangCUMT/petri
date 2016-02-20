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
using System.Collections.Generic;

namespace Petri.Editor
{
    /// <summary>
    /// The undo manager of the application, one instance per document.
    /// </summary>
    public class UndoManager
    {
        public UndoManager()
        {
            _undoStack = new Stack<ActionDescription>();
            _redoStack = new Stack<ActionDescription>();
        }

        /// <summary>
        /// Registers the GUI action into the undo manager's stacks, and commit the action so that its effect are visible.
        /// </summary>
        /// <param name="action">Action.</param>
        public void CommitGuiAction(GuiAction action)
        {
            _redoStack.Clear();
            _undoStack.Push(new ActionDescription(action, action.Description));
            action.Apply();
        }

        /// <summary>
        /// Undoes the last committed GUI action and pushes it onto the redo stack.
        /// </summary>
        public void Undo()
        {
            this.SwapAndApply(_undoStack, _redoStack);
        }

        /// <summary>
        /// Undoes the last undone GUI action and pushes it onto the undo stack.
        /// </summary>
        public void Redo()
        {
            this.SwapAndApply(_redoStack, _undoStack);
        }

        /// <summary>
        /// Clear the undo/redo stacks, meaning that no action can be undone/redone after this call.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// Gets the GUI action on top of the undo stack.
        /// </summary>
        /// <value>The next undo action, or <c>null</c> if the stack is empty.</value>
        public GuiAction NextUndo {
            get {
                if(_undoStack.Count > 0)
                    return _undoStack.Peek()._action;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the description of the GUI action on top of the undo stack.
        /// Do not call when the stack is empty, or be prepared to get a <c>NullReferenceException</c>.
        /// </summary>
        /// <value>The description of the next undo action, or <c>null</c> if the stack is empty.</value>
        public string NextUndoDescription {
            get {
                return _undoStack.Peek()._description;
            }
        }

        /// <summary>
        /// Gets the GUI action on top of the redo stack.
        /// </summary>
        /// <value>The next redo action, or <c>null</c> if the stack is empty.</value>
        public GuiAction NextRedo {
            get {
                if(_redoStack.Count > 0)
                    return _redoStack.Peek()._action;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the description of the GUI action on top of the red stack.
        /// Do not call when the stack is empty, or be prepared to get a <c>NullReferenceException</c>.
        /// </summary>
        /// <value>The description of the next redo action, or <c>null</c> if the stack is empty.</value>
        public string NextRedoDescription {
            get {
                return _redoStack.Peek()._description;
            }
        }

        private struct ActionDescription
        {
            public ActionDescription(GuiAction a, string d)
            {
                _action = a;
                _description = d;
            }

            public GuiAction _action;
            public string _description;
        }

        /// <summary>
        /// Removes the top Gui action from <paramref name="toPop"/>, pushes its opposite to <paramref name="toPush"/> and commits this opposite action.
        /// </summary>
        /// <param name="toPop">To stack to pop from.</param>
        /// <param name="toPush">To stack to push to.</param>
        private void SwapAndApply(Stack<ActionDescription> toPop, Stack<ActionDescription> toPush)
        {
            if(toPop.Count > 0) {
                var actionDescription = toPop.Pop();
                actionDescription._action = actionDescription._action.Reverse();
                toPush.Push(new ActionDescription(actionDescription._action,
                                                  actionDescription._description));
                actionDescription._action.Apply();
            }
        }

        Stack<ActionDescription> _undoStack;
        Stack<ActionDescription> _redoStack;
    }

    /// <summary>
    /// GUI action.
    /// </summary>
    public abstract class GuiAction
    {
        /// <summary>
        /// Apply this instance, actually applying the Gui change it encapsulates.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Gets the opposite instance of <c>this</c>. The opposite of moving and entity 10px to the right is moving it 10px to the left.
        /// </summary>
        public abstract GuiAction Reverse();

        /// <summary>
        /// The object that is meant to gain the user's focus upon Apply()ing.
        /// </summary>
        /// <value>The focus.</value>
        public abstract IFocusable Focus {
            get;
        }

        /// <summary>
        /// Gets the description of the GUI action's effect.
        /// </summary>
        /// <value>The description.</value>
        public abstract string Description {
            get;
        }
    }

    /// <summary>
    /// A simple wrapper that allows to change the description of a GUI action but keep its effect.
    /// </summary>
    public class GuiActionWrapper : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.GuiActionWrapper"/> class.
        /// </summary>
        /// <param name="action">The action to wrap.</param>
        /// <param name="description">The description of the wrapper.</param>
        public GuiActionWrapper(GuiAction action, string description)
        {
            _action = action;
            _description = description;
        }

        public override void Apply()
        {
            _action.Apply();
        }

        public override GuiAction Reverse()
        {
            return new GuiActionWrapper(_action.Reverse(), _description);
        }

        public override IFocusable Focus {
            get {
                return _action.Focus;
            }
        }

        public override string Description {
            get {
                return _description;
            }
        }

        GuiAction _action;
        string _description;
    }

    /// <summary>
    /// A GUI action that wraps around a list of GUI actions, by applying then in the order they are given, and undoing them in the reverse order they are given.
    /// </summary>
    public class GuiActionList : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.GuiActionList"/> class.
        /// </summary>
        /// <param name="actions">The actions list, considered in the enumeration's natural order. Must not be empty.</param>
        /// <param name="description">The description for the wrapper action.</param>
        public GuiActionList(IEnumerable<GuiAction> actions, string description)
        {
            _actions = new List<GuiAction>(actions);
            _description = description;

            // Strange use case anyway
            if(_actions.Count == 0) {
                throw new ArgumentException(Configuration.GetLocalized("The action list is empty!"));
            }
        }

        public override void Apply()
        {
            foreach(var a in _actions) {
                a.Apply();
            }
        }

        /// <summary>
        /// The opposite action correctly reverses the order each GUI action's opposite has to be applied.
        /// </summary>
        public override GuiAction Reverse()
        {
            var l = new List<GuiAction>();
            foreach(var a in _actions) {
                l.Insert(0, a.Reverse());
            }

            return new GuiActionList(l, _description);
        }

        /// <summary>
        /// The object that is meant to gain the user's focus upon Apply()ing.
        /// Here, the object is a list of the inner action's Focus objects.
        /// When an inner object's Focus returns a List<object>, then the list is flattened into the return value.
        /// </summary>
        /// <value>The focus.</value>
        public override IFocusable Focus {
            get {
                var l = new List<IFocusable>();
                foreach(var a in _actions) {
                    var f = a.Focus;
                    if(f is IEnumerable<IFocusable>) {
                        l.AddRange(f as IEnumerable<IFocusable>);
                    }
                    else {
                        l.Add(f);
                    }
                }

                return new FocusableList(l);
            }
        }

        public override string Description {
            get {
                return _description;
            }
        }

        List<GuiAction> _actions;
        string _description;
    }

    /// <summary>
    /// Change the Entity's parent, removing it from its previous and adding it to the new.
    /// </summary>
    public class ChangeParentAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeParentAction"/> class.
        /// </summary>
        /// <param name="e">The entity</param>
        /// <param name="parent">The new parent.</param>
        public ChangeParentAction(Entity e, PetriNet parent)
        {
            _entity = e;
            _newParent = parent;
            _oldParent = _entity.Parent;
        }

        public override void Apply()
        {
            _entity.Parent = _newParent;
        }

        public override GuiAction Reverse()
        {
            return new ChangeParentAction(_entity, _oldParent); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_entity);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change Parent");
            }
        }

        Entity _entity;
        PetriNet _newParent;
        PetriNet _oldParent;
    }

    /// <summary>
    /// Change the name of an entity.
    /// </summary>
    public class ChangeNameAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeNameAction"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="newName">The new name.</param>
        public ChangeNameAction(Entity entity, string newName)
        {
            _entity = entity;
            _newName = newName;
            _oldName = _entity.Name;
        }

        public override void Apply()
        {
            _entity.Name = _newName;
        }

        public override GuiAction Reverse()
        {
            return new ChangeNameAction(_entity, _oldName); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_entity);
            }
        }

        public override string Description {
            get {
                if(_entity is Comment) {
                    return Configuration.GetLocalized("Change Comment");
                }
                else {
                    return Configuration.GetLocalized("Change Name");
                }
            }
        }

        Entity _entity;
        string _newName;
        string _oldName;
    }

    /// <summary>
    /// Change the required tokens count of a state.
    /// </summary>
    public class ChangeRequiredTokensAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeRequiredTokensAction"/> class.
        /// </summary>
        /// <param name="state">State.</param>
        /// <param name="newCount">The new token count.</param>
        public ChangeRequiredTokensAction(State state, int newCount)
        {
            _state = state;
            _newCount = newCount;
            _oldCount = state.RequiredTokens;
        }

        public override void Apply()
        {
            _state.RequiredTokens = _newCount;
        }

        public override GuiAction Reverse()
        {
            return new ChangeRequiredTokensAction(_state, _oldCount); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_state);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change required tokens count");
            }
        }

        State _state;
        int _newCount;
        int _oldCount;
    }

    /// <summary>
    /// Moves an petri net entity in the view from a specified amount.
    /// </summary>
    public class MoveAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.MoveAction"/> class.
        /// The opposite action of this instance will not put the entity back aligned to the grid.
        /// </summary>
        /// <param name="entity">The entity to move.</param>
        /// <param name="delta">The position delta to move the entity from.</param>
        /// <param name="grid">If set to <c>true</c>, the new position will be adapted to fit on the grid.</param>
        public MoveAction(Entity entity, Cairo.PointD delta, bool grid) : this(entity,
                                                                               delta,
                                                                               grid,
                                                                               false)
        {
			
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.MoveAction"/> class.
        /// </summary>
        /// <param name="entity">The entity to move.</param>
        /// <param name="delta">The position delta to move the entity from.</param>
        /// <param name="grid">If set to <c>true</c>, the new position will be adapted to fit on the grid.</param>
        /// <param name="oldGrid">Tells whether the opposite action will put the entity back on the grid or not.</param>
        private MoveAction(Entity entity, Cairo.PointD delta, bool grid, bool oldGrid)
        {
            _oldGrid = oldGrid;
            _grid = grid;
            _entity = entity;
            _delta = new Cairo.PointD(delta.X, delta.Y);

            if(_grid) {
                var pos = new Cairo.PointD(_entity.Position.X + _delta.X,
                                           _entity.Position.Y + _delta.Y);
                pos.X = Math.Round(pos.X / Entity.GridSize) * Entity.GridSize;
                pos.Y = Math.Round(pos.Y / Entity.GridSize) * Entity.GridSize;
                _delta.X = pos.X - _entity.Position.X;
                _delta.Y = pos.Y - _entity.Position.Y;
            }
        }

        public override void Apply()
        {
            _entity.Position = new Cairo.PointD(_entity.Position.X + _delta.X,
                                                _entity.Position.Y + _delta.Y);
        }

        public override GuiAction Reverse()
        {
            return new MoveAction(_entity, new Cairo.PointD(-_delta.X, -_delta.Y), _oldGrid, _grid); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_entity);
            }
        }

        public override string Description {
            get {
                string entity;
                if(_entity is State) {
                    entity = Configuration.GetLocalized("the state");
                }
                else if(_entity is Transition) {
                    entity = Configuration.GetLocalized("the transition");
                }
                else if(_entity is Comment) {
                    entity = Configuration.GetLocalized("the comment");
                }
                else {// Too lazy to search for a counter example but should probably never happen
                    entity = Configuration.GetLocalized("the entity");
                }

                return Configuration.GetLocalized("Move {0}", entity);
            }
        }

        bool _grid, _oldGrid;
        Entity _entity;
        Cairo.PointD _delta;
    }

    /// <summary>
    /// Toggles if the specified state is active upon the petri net's launch or not.
    /// </summary>
    public class ToggleActiveAction : GuiAction
    {
        public ToggleActiveAction(State e)
        {
            _entity = e;
        }

        public override void Apply()
        {
            _entity.Active = !_entity.Active;
        }

        public override GuiAction Reverse()
        {
            return new ToggleActiveAction(_entity); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_entity);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the state");
            }
        }

        State _entity;
    }

    /// <summary>
    /// Changes the expression associated to the condition of a transition.
    /// </summary>
    public class ConditionChangeAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ConditionChangeAction"/> class.
        /// </summary>
        /// <param name="transition">Transition.</param>
        /// <param name="newCondition">New condition.</param>
        public ConditionChangeAction(Transition transition, Code.Expression newCondition)
        {
            Transition = transition;
            _oldCondition = transition.Condition;
            _newCondition = newCondition;
        }

        public override void Apply()
        {
            Transition.Condition = _newCondition;
        }

        public override GuiAction Reverse()
        {
            return new ConditionChangeAction(Transition, _oldCondition); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(Transition);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the condition");
            }
        }

        public Transition Transition {
            get;
            private set;
        }

        Code.Expression _newCondition, _oldCondition;
    }

    /// <summary>
    /// Changes the expression associated to the invocation of a petri net action.
    /// </summary>
    public class InvocationChangeAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.InvocationChangeAction"/> class.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <param name="invocation">The new invocation.</param>
        public InvocationChangeAction(Action action, Code.FunctionInvocation invocation)
        {
            Action = action;
            _oldInvocation = action.Function;
            _newInvocation = invocation;
        }

        public override void Apply()
        {
            Action.Function = _newInvocation;
        }

        public override GuiAction Reverse()
        {
            return new InvocationChangeAction(Action, _oldInvocation); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(Action);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the function");
            }
        }

        public Action Action {
            get;
            private set;
        }

        Code.FunctionInvocation _newInvocation, _oldInvocation;
    }

    /// <summary>
    /// Add a comment action to the petri net.
    /// </summary>
    public class AddCommentAction : GuiAction
    {
        public AddCommentAction(Comment comment)
        {
            _comment = comment;
        }

        public override void Apply()
        {
            _comment.Parent.AddComment(_comment);
        }

        public override GuiAction Reverse()
        {
            return new RemoveCommentAction(_comment); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_comment);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a comment");
            }
        }

        Comment _comment;
    }

    /// <summary>
    /// Removes a comment from the petri net.
    /// </summary>
    public class RemoveCommentAction : GuiAction
    {
        public RemoveCommentAction(Comment comment)
        {
            _comment = comment;
        }

        public override void Apply()
        {
            _comment.Parent.RemoveComment(_comment);
        }

        public override GuiAction Reverse()
        {
            return new AddCommentAction(_comment); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_comment.Parent);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove the comment");
            }
        }

        Comment _comment;
    }

    /// <summary>
    /// Changes a petri net comment's background color.
    /// </summary>
    public class ChangeCommentColorAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeCommentColorAction"/> class.
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <param name="color">The new color.</param>
        public ChangeCommentColorAction(Comment comment, Cairo.Color color)
        {
            _comment = comment;
            _oldColor = new Cairo.Color(comment.Color.R,
                                        comment.Color.G,
                                        comment.Color.B,
                                        comment.Color.A);
            _newColor = color;
        }

        public override void Apply()
        {
            _comment.Color = new Cairo.Color(_newColor.R, _newColor.G, _newColor.B, _newColor.A);
        }

        public override GuiAction Reverse()
        {
            return new ChangeCommentColorAction(_comment, _oldColor); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_comment);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the comment's color");
            }
        }

        Comment _comment;
        Cairo.Color _oldColor, _newColor;
    }

    /// <summary>
    /// Resize a petri net comment.
    /// </summary>
    public class ResizeCommentAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ResizeCommentAction"/> class.
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <param name="size">The new size.</param>
        public ResizeCommentAction(Comment comment, Cairo.PointD size)
        {
            _comment = comment;
            _oldSize = new Cairo.PointD(comment.Size.X, comment.Size.Y);
            _newSize = size;
        }

        public override void Apply()
        {
            _comment.Size = new Cairo.PointD(_newSize.X, _newSize.Y);
        }

        public override GuiAction Reverse()
        {
            return new ResizeCommentAction(_comment, _oldSize); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_comment);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Resize the comment");
            }
        }

        Comment _comment;
        Cairo.PointD _oldSize, _newSize;
    }

    /// <summary>
    /// Adds a transition to the petri net.
    /// </summary>
    public class AddTransitionAction : GuiAction
    {
        public AddTransitionAction(Transition t, bool incrementTokenCount)
        {
            _transition = t;
            _incrementTokenCount = incrementTokenCount;
        }

        public override void Apply()
        {
            _transition.Before.Parent.AddTransition(_transition);

            _transition.Before.AddTransitionAfter(_transition);
            _transition.After.AddTransitionBefore(_transition);
            if(_incrementTokenCount)
                ++_transition.After.RequiredTokens;
        }

        public override GuiAction Reverse()
        {
            return new RemoveTransitionAction(_transition, _incrementTokenCount); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_transition);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a transition");
            }
        }

        Transition _transition;
        bool _incrementTokenCount;
    }

    /// <summary>
    /// Detach one of the transition's end and attach it to a new state (possibly the same).
    /// </summary>
    public class ChangeTransitionEndAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeTransitionEndAction"/> class.
        /// It sets <c>true</c> to the last but one parameter of the other constructor of this class iff:
        /// <list type="bullet">
        /// <item><description><paramref name="destination"/> is set to <c>true</c></description></item>
        /// <item><description><c>transition.After.RequiredTokens == transition.After.TransitionsBefore.Count</c></description></item>
        /// </list>
        /// It sets <c>true</c> to the last parameter of the other constructor of this class iff:
        /// <list type="bullet">
        /// <item><description><paramref name="destination"/> is set to <c>true</c></description></item>
        /// <item><description><c>newEnd.TransitionsBefore.Count == 0</c></description></item>
        /// </list>
        /// </summary>
        /// <param name="transition">The transition.</param>
        /// <param name="newEnd">The new state at which the transition's end will be attached</param>
        /// <param name="destination">If set to <c>true</c>, the transition's end which is moved is the destination end (pointed towards by the arrow). Otherwise, it is the start state of the transition.</param>
        public ChangeTransitionEndAction(Transition transition, State newEnd, bool destination) : this(transition,
                                                                                                       newEnd,
                                                                                                       destination,
                                                                                                       destination && transition.After.RequiredTokens == transition.After.TransitionsBefore.Count,
                                                                                                       destination && newEnd.TransitionsBefore.Count == 0)
        {
		
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeTransitionEndAction"/> class.
        /// </summary>
        /// <param name="transition">The transition.</param>
        /// <param name="newEnd">The new state at which the transition's end will be attached</param>
        /// <param name="destination">If set to <c>true</c>, the transition's end which is moved is the destination end (pointed towards by the arrow). Otherwise, it is the start state of the transition.</param>
        /// <param name="decrementOld">Whether the transitions' detached end sees its required token count decremented. Only effective is <c>destination</c> is set to true.</param>
        /// <param name="incrementNew">Whether the transitions' newly attached end sees its required token count incremented. Only effective is <c>destination</c> is set to true.</param>
        private ChangeTransitionEndAction(Transition transition,
                                          State newEnd,
                                          bool destination,
                                          bool decrementOld,
                                          bool incrementNew)
        {
            _transition = transition;
            _newEnd = newEnd;
            _destination = destination;
            _decrementOld = decrementOld;
            _incrementNew = incrementNew;
            if(destination) {
                _oldEnd = _transition.After;
            }
            else {
                _oldEnd = _transition.Before;
            }
        }

        public override void Apply()
        {
            if(_destination) {
                _transition.After.RemoveTransitionBefore(_transition);
                if(_decrementOld) {
                    --_transition.After.RequiredTokens;
                }
                _transition.After = _newEnd;
                _newEnd.AddTransitionBefore(_transition);
            }
            else {
                _transition.Before.RemoveTransitionAfter(_transition);
                if(_decrementOld) {
                    --_transition.Before.RequiredTokens;
                }
                _transition.Before = _newEnd;
                _newEnd.AddTransitionAfter(_transition);
            }

            if(_destination && _incrementNew) {
                ++_newEnd.RequiredTokens;
            }
        }

        public override GuiAction Reverse()
        {
            return new ChangeTransitionEndAction(_transition,
                                                 _oldEnd,
                                                 _destination,
                                                 _incrementNew,
                                                 _decrementOld); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_transition);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the transition's end");
            }
        }

        Transition _transition;
        State _newEnd, _oldEnd;
        bool _destination, _decrementOld, _incrementNew;
    }

    /// <summary>
    /// Removes a transition from its petri net.
    /// </summary>
    public class RemoveTransitionAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.RemoveTransitionAction"/> class.
        /// </summary>
        /// <param name="transtition">Transtition.</param>
        /// <param name="decrementTokenCount">If set to <c>true</c> then decrement token count of the destination.</param>
        public RemoveTransitionAction(Transition transtition, bool decrementTokenCount)
        {
            _transition = transtition;
            _decrementTokenCount = decrementTokenCount;
        }

        public override void Apply()
        {
            if(_decrementTokenCount) {
                --_transition.After.RequiredTokens;
            }
            _transition.Before.Parent.RemoveTransition(_transition);
        }

        public override GuiAction Reverse()
        {
            return new AddTransitionAction(_transition, _decrementTokenCount); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_transition.Parent);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove the transition");
            }
        }

        Transition _transition;
        bool _decrementTokenCount;
    }

    /// <summary>
    /// Adds a state to its petri net.
    /// </summary>
    public class AddStateAction : GuiAction
    {
        public AddStateAction(State s)
        {
            _state = s;
        }

        public override void Apply()
        {
            _state.Parent.AddState(_state);
        }

        public override GuiAction Reverse()
        {
            return new RemoveStateAction(_state); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_state);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a state");
            }
        }

        State _state;
    }

    /// <summary>
    /// Removes a state from its petri net.
    /// </summary>
    public class RemoveStateAction : GuiAction
    {
        public RemoveStateAction(State s)
        {
            _state = s;
        }

        public override void Apply()
        {
            _state.Parent.RemoveState(_state);
        }

        public override GuiAction Reverse()
        {
            return new AddStateAction(_state); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableEntity(_state.Parent);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove the state");
            }
        }

        State _state;
    }

    /// <summary>
    /// Change the settings of a document.
    /// </summary>
    public class ChangeSettingsAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeSettingsAction"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="newSettings">The new settings.</param>
        public ChangeSettingsAction(Document document, DocumentSettings newSettings)
        {
            _document = document;
            _newSettings = newSettings;
            _oldSettings = _document.Settings;
        }

        public override void Apply()
        {
            _document.Settings = _newSettings;
        }

        public override GuiAction Reverse()
        {
            return new ChangeSettingsAction(_document, _oldSettings); 
        }

        public override IFocusable Focus {
            get {
                return new FocusableSettings(_document);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change document settings");
            }
        }

        Document _document;
        DocumentSettings _newSettings;
        DocumentSettings _oldSettings;
    }

    /// <summary>
    /// Change a preprocessor macro's value.
    /// </summary>
    public class ChangeMacroAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.ChangeMacroAction"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="key">The name of the macro.</param>
        /// <param name="newValue">The new value of the macro.</param>
        public ChangeMacroAction(Document doc, string key, string newValue)
        {
            _document = doc;
            _key = key;
            if(doc.PreprocessorMacros.ContainsKey(key)) {
                _oldValue = doc.PreprocessorMacros[key];
            }
            _newValue = newValue;
        }

        public override void Apply()
        {
            _document.PreprocessorMacros[_key] = _newValue;
        }

        public override GuiAction Reverse()
        {
            if(_oldValue != null) {
                return new ChangeMacroAction(_document, _key, _oldValue); 
            }
            else {
                return new RemoveMacroAction(_document, _key);
            }
        }

        public override IFocusable Focus {
            get {
                return new FocusableMacroEditor(_document);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change preprocessor macro's value");
            }
        }

        string _key;
        string _oldValue;
        string _newValue;
        Document _document;
    }

    /// <summary>
    /// Remove a preprocessor macro.
    /// </summary>
    public class RemoveMacroAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.RemoveMacroAction"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="key">The name of the macro to remove.</param>
        public RemoveMacroAction(Document doc, string key)
        {
            _document = doc;
            _key = key;
            _oldValue = doc.PreprocessorMacros[key];
        }

        public override void Apply()
        {
            _document.PreprocessorMacros.Remove(_key);
        }

        public override GuiAction Reverse()
        {
            return new ChangeMacroAction(_document, _key, _oldValue);
        }

        public override IFocusable Focus {
            get {
                return new FocusableMacroEditor(_document);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove preprocessor macro");
            }
        }

        string _key;
        string _oldValue;
        Document _document;
    }

    /// <summary>
    /// Add a new header to the document.
    /// </summary>
    public class AddHeaderAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.AddHeaderAction"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="path">The path to the new header.</param>
        public AddHeaderAction(Document doc, string path)
        {
            _document = doc;
            _path = path;
        }

        public override void Apply()
        {
            _document.AddHeaderNoUpdate(_path);
        }

        public override GuiAction Reverse()
        {
            return new RemoveHeaderAction(_document, _path);
        }

        public override IFocusable Focus {
            get {
                return new FocusableHeadersEditor(_document);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a header");
            }
        }

        string _path;
        Document _document;
    }

    /// <summary>
    /// Remove a header from the document.
    /// </summary>
    public class RemoveHeaderAction : GuiAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.RemoveHeaderAction"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="path">The path to the document to remove from the document.</param>
        public RemoveHeaderAction(Document doc, string path)
        {
            _document = doc;
            _path = path;
        }

        public override void Apply()
        {
            _document.RemoveHeaderNoUpdate(_path);
            _document.DispatchFunctions();
        }

        public override GuiAction Reverse()
        {
            return new AddHeaderAction(_document, _path);
        }

        public override IFocusable Focus {
            get {
                return new FocusableHeadersEditor(_document);
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove a header");
            }
        }

        string _path;
        Document _document;
    }

    /// <summary>
    /// A dumb GUI action that only remembers an object as the return value of the Focus getter.
    /// </summary>
    public class DoNothingAction : GuiAction
    {
        public DoNothingAction(IFocusable focus)
        {
            _focus = focus;
        }

        public override void Apply()
        {
        }

        public override GuiAction Reverse()
        {
            return new DoNothingAction(_focus); 
        }

        public override IFocusable Focus {
            get {
                return _focus;
            }
        }

        public override string Description {
            get {
                return "";
            }
        }

        IFocusable _focus;
    }
}

