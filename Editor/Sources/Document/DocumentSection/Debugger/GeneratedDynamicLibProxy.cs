using System;

namespace Petri.Editor
{
    public class GeneratedDynamicLibProxy : MarshalByRefObject
    {
        public GeneratedDynamicLibProxy(string libPath, string libName, string className)
        {
            LibPath = libPath;
            LibName = libName;
            ClassName = className;
        }

        string LibPath {
            get;
            set;
        }

        string LibName {
            get;
            set;
        }

        string ClassName {
            get;
            set;
        }

        public Petri.Runtime.GeneratedDynamicLib Load()
        {
            var filePath = System.IO.Path.Combine(LibPath, LibName);

            if(_domain == null) {
                _domain = AppDomain.CreateDomain("PetriDynamicLib" + filePath);
            }

            Petri.Runtime.GeneratedDynamicLib dylib = null;
            try {
                dylib = (Petri.Runtime.GeneratedDynamicLib)_domain.CreateInstanceFromAndUnwrap(filePath, "Petri.Generated." + ClassName);
            }
            catch(Exception ex) {
                Console.WriteLine("Exxxxxx: " + ex.Message + "\n" + ex.StackTrace + "\n\n" + ex);
            }                        
            return dylib;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unload()
        {
            if(_domain != null) {
                AppDomain.Unload(_domain);
                _domain = null;
            }            
        }

        AppDomain _domain;
    }
}

