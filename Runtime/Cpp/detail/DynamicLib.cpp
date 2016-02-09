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
//  DynamicLib.cpp
//  IA Pétri
//
//  Created by Rémi on 04/05/2015.
//

#include "../DynamicLib.h"
#include <dlfcn.h>
#include <fcntl.h>
#include <iostream>
#include <stdio.h>
#include <string.h>
#include <unistd.h>

namespace Petri {

    DynamicLib::DynamicLib(bool nodelete, std::string const &path)
            : _nodelete(nodelete)
            , _path(path) {

        // Keeping the working directory to cd into it when load() is invoked.
        // This allows libs specified with a relative path to be loaded when the working directory
        // has been changed.
        _wd = open(".", O_RDONLY);
        if(_wd < 0) {
            std::cerr << "DynamicLib::DynamicLib(): Could not open the current directory ("
                      << strerror(errno) << ")!" << std::endl;
        }
    }

    DynamicLib::~DynamicLib() {
        this->unload();
        if(_wd >= 0) {
            close(_wd);
        }
    }

    void DynamicLib::load() {
        if(this->loaded()) {
            return;
        }

        // Keeping the previous working directory…
        int oldwd = open(".", O_RDONLY);
        try {
            if(oldwd < 0) {
                std::cerr << "DynamicLib::load(): Could not open the current directory ("
                          << strerror(errno) << ")!" << std::endl;
            }

            // … changing to the saved one…
            fchdir(_wd);

            int nodeleteFlag = _nodelete ? RTLD_NODELETE : 0;

            std::string path = this->path();

            _libHandle = dlopen(path.c_str(), RTLD_NOW | RTLD_LOCAL | nodeleteFlag);

            if(_libHandle == nullptr) {
                std::cerr << "Unable to load the dynamic library at path \"" << path << "\"!\n"
                          << "Reason: " << dlerror() << std::endl;

                throw std::runtime_error("Unable to load the dynamic library at path \"" + path + "\"!");
            }

            // … and restoring the previous working directory.
            fchdir(oldwd);
            if(oldwd >= 0) {
                close(oldwd);
            }
        } catch(...) {
            fchdir(oldwd);
            if(oldwd >= 0) {
                close(oldwd);
            }

            throw;
        }
    }

    /**
     * Removes the dynamic library associated to this wrapper from memory.
     */
    void DynamicLib::unload() {
        if(this->loaded()) {
            if(dlclose(_libHandle) != 0) {
                std::cerr << "Unable to unload the dynamic library!\n"
                          << "Reason: " << dlerror() << std::endl;
            }
        }

        _libHandle = nullptr;
    }

    void *DynamicLib::_loadSymbol(const std::string &name) {
        return dlsym(_libHandle, name.c_str());
    }
}
