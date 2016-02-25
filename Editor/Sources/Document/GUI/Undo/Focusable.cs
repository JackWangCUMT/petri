using System;
using System.Collections.Generic;

namespace Petri.Editor
{
    /// <summary>
    /// An object that can gain the users focus.
    /// </summary>
    public interface IFocusable
    {
        /// <summary>
        /// Gets the user's focus.
        /// </summary>
        void Focus();
    }

    /// <summary>
    /// A list of IFocusable. If all of the IFocusable instances are FocusableEntity, then the corresponding Entity instances are set as the document's selection.
    /// Else, they are all focused, one after the other.
    /// </summary>
    public class FocusableList : IFocusable
    {
        public FocusableList(List<IFocusable> focus)
        {
            _focus = new HashSet<IFocusable>(focus);
        }

        public void Focus()
        {
            bool isEntitiesList = true;
            foreach(IFocusable f in _focus) {
                if(!(f is FocusableEntity)) {
                    isEntitiesList = false;
                    break;
                }
            }
            if(isEntitiesList && _focus.Count > 1) {
                Document doc = null;
                foreach(var f in _focus) {
                    doc = (Document)((FocusableEntity)f).Entity.Document;
                    break;
                }
                doc.Window.EditorGui.View.SelectedEntities.Clear();

                foreach(FocusableEntity e in _focus) {
                    doc.Window.EditorGui.View.SelectedEntities.Add(e.Entity);
                }
                doc.EditorController.UpdateSelection();
            }
            else {
                foreach(IFocusable f in _focus) {
                    f.Focus();
                }
            }
        }

        HashSet<IFocusable> _focus;
    }

    /// <summary>
    /// Focus on an Entity instance of a petri net
    /// </summary>
    public class FocusableEntity : IFocusable
    {
        public FocusableEntity(Entity focus)
        {
            Entity = focus;
        }

        public void Focus()
        {
            ((Document)Entity.Document).Window.EditorGui.View.SelectedEntity = Entity;
        }

        public Entity Entity {
            get;
            private set;
        }

        public override bool Equals(object o) {
            return o is FocusableEntity && ((FocusableEntity)o).Entity == Entity;
        }

        public override int GetHashCode()
        {
            return Entity.GetHashCode();
        }
    }

    /// <summary>
    /// Focus on the document's settings
    /// </summary>
    public class FocusableSettings : IFocusable
    {
        public FocusableSettings(Document doc)
        {
            _document = doc;
        }

        public void Focus()
        {
            _document.EditSettings();
        }

        Document _document;
    }

    /// <summary>
    /// Focus on the document's macros editor
    /// </summary>
    public class FocusableMacroEditor : IFocusable
    {
        public FocusableMacroEditor(Document doc)
        {
            _document = doc;
        }

        public void Focus()
        {
            _document.ManageMacros();
        }

        Document _document;
    }

    /// <summary>
    /// Focus on the document's headers editor
    /// </summary>
    public class FocusableHeadersEditor : IFocusable
    {
        public FocusableHeadersEditor(Document doc)
        {
            _document = doc;
        }

        public void Focus()
        {
            _document.ManageHeaders();
        }

        Document _document;
    }
}

