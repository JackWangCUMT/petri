﻿/*
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

namespace Petri
{
    public class UndoManager
    {
        public UndoManager()
        {
            _undoStack = new Stack<ActionDescription>();
            _redoStack = new Stack<ActionDescription>();
        }

        public void PostAction(GuiAction a)
        {
            _redoStack.Clear();
            _undoStack.Push(new ActionDescription(a, a.Description));
            a.Apply();
        }

        public void Undo()
        {
            this.SwapAndApply(_undoStack, _redoStack);
        }

        public void Redo()
        {
            this.SwapAndApply(_redoStack, _undoStack);
        }

        public GuiAction NextUndo {
            get {
                if(_undoStack.Count > 0)
                    return _undoStack.Peek()._action;
                else
                    return null;
            }
        }

        public string NextUndoDescription {
            get {
                return _undoStack.Peek()._description;
            }
        }

        public GuiAction NextRedo {
            get {
                if(_redoStack.Count > 0)
                    return _redoStack.Peek()._action;
                else
                    return null;
            }
        }

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

        private void SwapAndApply(Stack<ActionDescription> toPop, Stack<ActionDescription> toPush)
        {
            if(toPop.Count > 0) {
                var actionDescription = toPop.Pop();
                actionDescription._action = actionDescription._action.Reverse();
                toPush.Push(new ActionDescription(actionDescription._action, actionDescription._description));
                actionDescription._action.Apply();
            }
        }

        Stack<ActionDescription> _undoStack;
        Stack<ActionDescription> _redoStack;
    }

    public abstract class GuiAction
    {
        public abstract void Apply();

        public abstract GuiAction Reverse();

        public abstract object Focus {
            get;
        }

        public abstract string Description {
            get;
        }
    }

    public class GuiActionWrapper : GuiAction
    {
        public GuiActionWrapper(GuiAction a, string description)
        {
            _action = a;
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

        public override object Focus {
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

    public class GuiActionList : GuiAction
    {
        public GuiActionList(IEnumerable<GuiAction> a, string description)
        {
            _actions = new List<GuiAction>(a);
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

        public override GuiAction Reverse()
        {
            var l = new List<GuiAction>();
            foreach(var a in _actions) {
                l.Insert(0, a.Reverse());
            }

            return new GuiActionList(l, _description);
        }

        public override object Focus {
            get {
                var l = new List<Entity>();
                foreach(var a in _actions) {
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
                return _description;
            }
        }

        List<GuiAction> _actions;
        string _description;
    }

    public class ChangeParentAction : GuiAction
    {
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

        public override object Focus {
            get {
                return _entity;
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

    public class ChangeNameAction : GuiAction
    {
        public ChangeNameAction(Entity e, string newName)
        {
            _entity = e;
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

        public override object Focus {
            get {
                return _entity;
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

    public class ChangeRequiredTokensAction : GuiAction
    {
        public ChangeRequiredTokensAction(State s, int newCount)
        {
            _state = s;
            _newCount = newCount;
            _oldCount = s.RequiredTokens;
        }

        public override void Apply()
        {
            _state.RequiredTokens = _newCount;
        }

        public override GuiAction Reverse()
        {
            return new ChangeRequiredTokensAction(_state, _oldCount); 
        }

        public override object Focus {
            get {
                return _state;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change requested tokens count");
            }
        }

        State _state;
        int _newCount;
        int _oldCount;
    }

    public class MoveAction : GuiAction
    {
        public MoveAction(Entity e, Cairo.PointD delta, bool grid) : this(e, delta, grid, false)
        {
			
        }

        private MoveAction(Entity e, Cairo.PointD delta, bool grid, bool oldGrid)
        {
            _oldGrid = oldGrid;
            _grid = grid;
            _entity = e;
            _delta = new Cairo.PointD(delta.X, delta.Y);

            if(_grid) {
                var pos = new Cairo.PointD(_entity.Position.X + _delta.X, _entity.Position.Y + _delta.Y);
                pos.X = Math.Round(pos.X / Entity.GridSize) * Entity.GridSize;
                pos.Y = Math.Round(pos.Y / Entity.GridSize) * Entity.GridSize;
                _delta.X = pos.X - _entity.Position.X;
                _delta.Y = pos.Y - _entity.Position.Y;
            }
        }

        public override void Apply()
        {
            _entity.Position = new Cairo.PointD(_entity.Position.X + _delta.X, _entity.Position.Y + _delta.Y);
        }

        public override GuiAction Reverse()
        {
            return new MoveAction(_entity, new Cairo.PointD(-_delta.X, -_delta.Y), _oldGrid, _grid); 
        }

        public override object Focus {
            get {
                return _entity;
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

        public override object Focus {
            get {
                return _entity;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Change the state");
            }
        }

        State _entity;
    }

    public class ConditionChangeAction : GuiAction
    {
        public ConditionChangeAction(Transition t, Cpp.Expression newCondition)
        {
            Transition = t;
            _oldCondition = t.Condition;
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

        public override object Focus {
            get {
                return Transition;
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

        Cpp.Expression _newCondition, _oldCondition;
    }

    public class InvocationChangeAction : GuiAction
    {
        public InvocationChangeAction(Action a, Cpp.FunctionInvocation i)
        {
            Action = a;
            _oldInvocation = a.Function;
            _newInvocation = i;
        }

        public override void Apply()
        {
            Action.Function = _newInvocation;
        }

        public override GuiAction Reverse()
        {
            return new InvocationChangeAction(Action, _oldInvocation); 
        }

        public override object Focus {
            get {
                return Action;
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

        Cpp.FunctionInvocation _newInvocation, _oldInvocation;
    }

    public class AddCommentAction : GuiAction
    {
        public AddCommentAction(Comment c)
        {
            _comment = c;
        }

        public override void Apply()
        {
            _comment.Parent.AddComment(_comment);
        }

        public override GuiAction Reverse()
        {
            return new RemoveCommentAction(_comment); 
        }

        public override object Focus {
            get {
                return _comment;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a comment");
            }
        }

        Comment _comment;
    }

    public class RemoveCommentAction : GuiAction
    {
        public RemoveCommentAction(Comment c)
        {
            _comment = c;
        }

        public override void Apply()
        {
            _comment.Parent.RemoveComment(_comment);
        }

        public override GuiAction Reverse()
        {
            return new AddCommentAction(_comment); 
        }

        public override object Focus {
            get {
                return _comment.Parent;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove the comment");
            }
        }

        Comment _comment;
    }

    public class ChangeCommentColorAction : GuiAction
    {
        public ChangeCommentColorAction(Comment c, Cairo.Color color)
        {
            _comment = c;
            _oldColor = new Cairo.Color(c.Color.R, c.Color.G, c.Color.B, c.Color.A);
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

        public override object Focus {
            get {
                return _comment;
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

    public class ResizeCommentAction : GuiAction
    {
        public ResizeCommentAction(Comment c, Cairo.PointD size)
        {
            _comment = c;
            _oldSize = new Cairo.PointD(c.Size.X, c.Size.Y);
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

        public override object Focus {
            get {
                return _comment;
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

        public override object Focus {
            get {
                return _transition;
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

    public class ChangeTransitionEndAction : GuiAction
    {
        public ChangeTransitionEndAction(Transition t, State newEnd, bool destination) : this(t, newEnd, destination, destination && t.After.RequiredTokens == t.After.TransitionsBefore.Count, destination && newEnd.TransitionsBefore.Count == 0)
        {
		
        }

        private ChangeTransitionEndAction(Transition t, State newEnd, bool destination, bool decrementOld, bool incrementNew)
        {
            _transition = t;
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

            if(_incrementNew) {
                ++_newEnd.RequiredTokens;
            }
        }

        public override GuiAction Reverse()
        {
            return new ChangeTransitionEndAction(_transition, _oldEnd, _destination, _incrementNew, _decrementOld); 
        }

        public override object Focus {
            get {
                return _transition;
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

    public class RemoveTransitionAction : GuiAction
    {
        public RemoveTransitionAction(Transition t, bool decrementTokenCount)
        {
            _transition = t;
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

        public override object Focus {
            get {
                return _transition.Parent;
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

        public override object Focus {
            get {
                return _state;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Add a state");
            }
        }

        State _state;
    }

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

        public override object Focus {
            get {
                return _state.Parent;
            }
        }

        public override string Description {
            get {
                return Configuration.GetLocalized("Remove the state");
            }
        }

        State _state;
    }

    public class DoNothingAction : GuiAction
    {
        public DoNothingAction(Entity e)
        {
            _entity = e;
        }

        public override void Apply()
        {
        }

        public override GuiAction Reverse()
        {
            return new DoNothingAction(_entity); 
        }

        public override object Focus {
            get {
                return _entity;
            }
        }

        public override string Description {
            get {
                return "";
            }
        }

        Entity _entity;
    }
}
