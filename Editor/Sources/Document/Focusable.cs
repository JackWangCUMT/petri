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
            _focus = focus.AsReadOnly();
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
            if(isEntitiesList) {
                var doc = (Document)((FocusableEntity)_focus[0]).Entity.Document;
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

        IReadOnlyList<IFocusable> _focus;
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
}

