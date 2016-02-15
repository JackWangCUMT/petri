# Petri

A C# Petri Net editor and compiler with a C, C++ and C# runtime. The editor/compiler is built against the Mono framework, see below for compiling and running of the editor.

## Bootstrapping a fresh repository
Simply run the `bootstrap.sh` script at the root of the repository. It currently initializes the Git submodules.

## Compilation of the petri net editor/compiler
Although you can get the running executables at https://github.com/rems4e/petri/releases, you may want to compile the source code.

### Tools
#### Linux
The mono distribution that comes with Debian (tested on Debian 8) or Ubuntu (tested on 15.10) is somewhat outdated and may fail to compile the source code.

I recommend that you follow the instructions found at http://www.mono-project.com/docs/getting-started/install/linux/, and then install the `mono-devel` and `gtk-sharp2` packages (at least on Debian based distros, the actual package may be different for others).

Alternatively, you can install the `monodevelop` package. This will give you a complete IDE.

#### OS X
There are two simple methods to install mono on OS X:
* By following the instructions at http://www.mono-project.com/docs/getting-started/install/mac/.
* By first installing Homebrew, the awesome package manager. For that, just follow the instructions at http://brew.sh, and then run the command `brew install mono`.

Alternatively, you can install Xamarin Studio for OS X from here: http://www.monodevelop.com/download/. This will give you a complete IDE.

### The compilation process
``` bash
make editor
```
This command will compile the editor.

``` bash
make test
```
This command will run the unit tests. It requires the `nunit-console` package.

## Running the editor/compiler
Once compiled, the editor is available in the Editor/bin directory, for command-line invocation and GUI on Linux.
On OS X, you will find a `Petri.app` application in the Editor directory, which is a lot more practical/friendly.

### Editor
``` bash
path_to_repo/petri/Editor $ mono bin/Petri.exe
```

This command will spawn the GUI editor.

On Linux, you can simply double click on the `Petri.exe` file, whereas on OS X simply open the `Petri.app` application.

### Compiler
The compiler is the same executable as before, simply invoked with additional arguments.
``` bash
path_to/_repo/petri/Editor $ mono bin/Petri.exe --help
Usage: mono Petri.exe [--generate|-g] [--compile|-c] [--run|-r] [--clean|-k] [--arch|-a (32|64)] [--verbose|-v] [--open|-o] [--] "Path/To/Document.petri"
```

## Compilation of the runtime
The compilation of the C++ runtime requires a C++14-compliant compiler (g++, tested on version 5.2 and 4.9, and clang++ from version 3.4 and over).

Run the following commands:
``` bash
make lib
```
They will give you a shared library, libPetriRuntime.so, that contains both the C and C++ runtimes, and a C# dll for the C# runtime.
