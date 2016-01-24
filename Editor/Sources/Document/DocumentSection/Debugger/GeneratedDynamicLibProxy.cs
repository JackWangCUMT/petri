/*
 * Copyright (c) 2016 Rémi Saurel
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

namespace Petri.Editor
{
    /// <summary>
    /// Generated dynamic lib proxy. This class allows for loading and unloading a dynamic lib generated from a C# PetriNet project.
    /// </summary>
    public class GeneratedDynamicLibProxy : MarshalByRefObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.GeneratedDynamicLibProxy"/> class.
        /// </summary>
        /// <param name="libPath">The directory containing the assembly to load.</param>
        /// <param name="libName">The name of the assambly to load. <example>MyLib.dll</example></param>
        /// <param name="className">The name of the class inheriting <see cref="Petri.Runtime.GeneratedDynamicLib"/> contained in the assembly.</param>
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

        /// <summary>
        /// Loads the assembly and extracts the <see cref="Petri.Runtime.GeneratedDynamicLib"/> instance it encloses.
        /// </summary>
        public Runtime.GeneratedDynamicLib Load()
        {
            var filePath = System.IO.Path.Combine(LibPath, LibName);

            if(_domain == null) {
                _domain = AppDomain.CreateDomain("PetriDynamicLib" + filePath);
            }

            Petri.Runtime.GeneratedDynamicLib dylib = null;
            try {
                dylib = (Petri.Runtime.GeneratedDynamicLib)_domain.CreateInstanceFromAndUnwrap(filePath,
                                                                                               "Petri.Generated." + ClassName);
            }
            catch(Exception ex) {
                Console.Error.WriteLine("Exception when loading assembly " + filePath + ": " + ex);
            }                        
            return dylib;
        }

        /// <summary>
        /// Unloads the assembly.
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

