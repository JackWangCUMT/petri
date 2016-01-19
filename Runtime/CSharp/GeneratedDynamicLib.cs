using System;

namespace Petri.Runtime
{
    /// <summary>
    /// The base class of a generated petri net dynamic library
    /// </summary>
    public abstract class GeneratedDynamicLib
    {
        /// <summary>
        /// Accesses the DynamicLib instance embedded into the library
        /// </summary>
        /// <value>The lib.</value>
        public DynamicLib Lib {
            get;
            protected set;
        }
    }
}

