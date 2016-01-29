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

//
//  DynamicLib.h
//  Pétri
//
//  Created by Rémi on 25/01/2015.
//

#ifndef Petri_DynamicLib_h
#define Petri_DynamicLib_h

#include <stdexcept>
#include <string>

namespace Petri {

    class DynamicLib {
    public:
        /**
         * Creates the dynamic library wrapper. It still needs to be loaded to access the dylib
         * symbols.
         */
        DynamicLib(bool nodelete, std::string const &path = "")
                : _nodelete(nodelete), _path(path) {}
        DynamicLib(DynamicLib const &pn) = delete;
        DynamicLib &operator=(DynamicLib const &pn) = delete;

        DynamicLib(DynamicLib &&pn) = default;
        DynamicLib &operator=(DynamicLib &&pn) = default;
        virtual ~DynamicLib() {
            this->unload();
        }

        /**
         * Returns whether the dylib code resides in memory or not
         * @return The loaded state of the dynamic library
         */
        virtual bool loaded() const {
            return _libHandle != nullptr;
        }

        /**
         * Gives access to the path of the dynamic library archive, relative to the executable path.
         * @return The relative path of the dylib
         */
        virtual std::string path() const {
            return _path;
        }

        /**
         * Loads the dynamic library associated to this wrapper.
         * @throws std::runtime_error when an error occurred (see subclasses doc for the possible
         * errors).
         */
        virtual void load();

        /**
         * Removes the dynamic library associated to this wrapper from memory.
         */
        virtual void unload();

        /**
         * Unloads the code of the dynamic library previously loaded, and loads the code contained
         * in a possibly updated dylib.
         */
        void reload() {
            this->unload();
            this->load();
        }

        /**
         * Loads the specified function symbol and returns it as a function pointer.
         * @param name The name of the symbol to load.
         * @throws std::runtime_error When the dynamic library is not load()ed.
         * @throws std:runtime_error When the symbol could not be found in the library.
         */
        template <typename FuncType>
        FuncType *loadSymbol(std::string const &name) {
            if(!this->loaded()) {
                throw std::runtime_error("Dynamic library not loaded!");
            }

            void *sym = _loadSymbol(name);

            if(sym == nullptr) {
                throw std::runtime_error("Could not find symbol " + name + " in the library!");
            }

            return reinterpret_cast<FuncType *>(sym);
        }

    protected:
        bool _nodelete;

        void *_loadSymbol(std::string const &name);
        void *_libHandle = nullptr;
        std::string const _path;
    };
}

#endif
