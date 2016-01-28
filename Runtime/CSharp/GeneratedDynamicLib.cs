using System;

namespace Petri.Runtime
{
    public interface GeneratedDynamicLib
    {
        DynamicLib Lib {
            get;
        }
    }

    /// <summary>
    /// The base class of a generated petri net dynamic library
    /// </summary>
    public abstract class CSharpGeneratedDynamicLib : MarshalByRefObject, GeneratedDynamicLib
    {
        /// <summary>
        /// Accesses the DynamicLib instance embedded into the library
        /// </summary>
        /// <value>The lib.</value>
        public DynamicLib Lib {
            get {
                return _lib;
            }
        }

        protected DynamicLib _lib;
    }

    public class CGeneratedDynamicLib : GeneratedDynamicLib
    {
        public CGeneratedDynamicLib(IntPtr handle) {
            _lib = new DynamicLib(handle);
        }

        public DynamicLib Lib {
            get {
                return _lib;
            }
        }

        protected DynamicLib _lib;
    }
}

