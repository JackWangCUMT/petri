# Petri

A C# Petri Net editor and compiler with a C++ runtime. The editor/compiler is built against the Mono framework, see below for execution and compilation of the editor.

## Bootstrapping a fresh repository
Simply run the `bootstrap.sh` script at the root of the repository. It currently initializes the Git submodules.

## Running the editor/compiler
### Linux
Install the Mono runtime by following the instructions found here: http://www.mono-project.com/docs/getting-started/install/linux/

### OS X
Install the Mono runtime by following the instructions found here: http://www.mono-project.com/docs/getting-started/install/mac/

## Compilation of the petri net editor/compiler
Although you can get the running executables at https://github.com/rems4e/petri/releases, you may want to compile the source code.

### Tools
#### Linux
The mono distribution that comes with Debian (tested on Debian 8) or Ubuntu (tested on 15.10) is somewhat outdated and may fail to compile the source code.

I recommend that you follow the instructions found at http://www.mono-project.com/docs/getting-started/install/linux/, and then install the `monodevelop` package (at least on Debian based distros, the actual package may be different for others).

#### OS X
First, download the binary release of Xamarin Studio for OS X, found here: http://www.monodevelop.com/download/.

The `mdtool` utility used for compiling a .csproj or .sln file is only bundled within the `Xamarin Studio` application. For some reason, adding the `/Application/Xamarin Studio.app/Contents/MacOS` path, where the tool is located, to the $PATH environment variable did not do the job for me, neither aliasses in my shell. So, the dumb solution I employed was to put the following script somewhere my $PATH points on:
``` bash
#!/bin/bash
/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool "$@"
```


### The compilation process
Some scripts are available in the Editor directory: `build.sh` and `test.sh`. When you run the former, the projects will be compiled and the binaries generated. A script called `clean.sh` will be generated as well, allowing to remove the bianries and intermediate build products.
You can the run the `test.sh` executable to run the unit tests.

### Running the editor/compiler
Once compiled, the editor is available in the Editor/bin directory, for command-line invocation and GUI on Linux.
On OS X, you will find a `Petri.app` application in the Editor directory, which is a lot more practical/friendly.

#### Editor
``` bash
path_to_repo/petri/Editor $ mono bin/Petri.exe
```

This command will spawn the GUI editor.

On Linux, you can simply double click on the `Petri.exe` file, whereas on OS X simply open the `Petri.app` application.

#### Compiler
The compiler is the same executable as before, simply invoked with additional arguments.
``` bash
path_to/_repo/petri/Editor $ mono bin/Petri.exe --help
Usage: mono Petri.exe [--generate|-g] [--compile|-c] [--arch|-a (32|64)] [--verbose|-v] [--] "Path/To/Document.petri"
```

## Compilation of the runtime
The compilation of the C++ runtime requires a C++14-compliant compiler (g++, tested on version 5.2 and 4.9, and clang++ from version 3.4 and over).

Run the following commands:
``` bash
cd Runtime
make
```
They will give you a shared library, libPetriRuntime.so, that contains both the C and C++ runtimes.
