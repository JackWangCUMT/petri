using System;

namespace Petri.Runtime
{
    public class Entity
    {
        protected Entity()
        {
        }

        public IntPtr Handle {
            get;
            set;
        }
    }
}

