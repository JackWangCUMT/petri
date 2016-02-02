CXXSRC:=$(wildcard Runtime/Cpp/detail/*.cpp) $(wildcard Runtime/C/detail/*.cpp)
CXXOBJ:=$(CXXSRC:%.cpp=build/%.o)
JSONSRC:=$(wildcard Runtime/Cpp/detail/jsoncpp/src/lib_json/*.cpp)
JSONOBJ:=$(JSONSRC:%.cpp=build/json/%.o)

WARN:=-Wall -Wunused-value -Wuninitialized

CXX:=c++
MSBUILD:=xbuild
CXXVERSION:=$(shell $(CXX) --version)

CXXFLAGS:=-std=c++14 -I./Runtime/Cpp/detail/jsoncpp/include
WARN_JSON:=
LDFLAGS:=-shared

CSCONF:=Release

ifneq (,$(findstring clang,$(CXXVERSION)))
# if the compiler is clang++

WARN:=$(WARN) -Werror=return-stack-address -Woverloaded-virtual -Wdocumentation -Wunused-parameter -Wmissing-prototypes
CXXFLAGS:=$(CXXFLAGS) -arch i386 -arch x86_64
LDFLAGS:=$(LDFLAGS) -undefined dynamic_lookup -flat_namespace -arch i386 -arch x86_64
WARN_JSON:=$(WARN_JSON) -Wno-documentation -Wno-missing-prototypes

else
# else, we assume g++

WARN:=$(WARN) -Werror=return-local-addr
CXXFLAGS:=$(CXXFLAGS) -fPIC
LDFLAGS:=$(LDFLAGS) -fPIC

endif

CXXFLAGS:=$(WARN) $(CXXFLAGS)

OUTPUT:=libPetriRuntime.so

.PHONY: builddir editor all clean test examples

all: lib editor

cleanlib:
	@rm -rf build
	@rm -f Runtime/$(OUTPUT)
	@rm -f Editor/bin/$(OUTPUT)

clean: cleanlib
	@rm -rf Editor/Test/bin/
	@rm -f Editor/bin/*.dll
	@rm -f Editor/bin/*.mdb
	@rm -f Editor/bin/*.exe
	@rm -rf Editor/Test/obj
	@rm -rf Editor/obj
	@rm -rf Editor/Projects/obj
	@rm -rf Editor/Petri.app
	@rm -f Editor/Petri.exe
	@rm -f Examples/CSRuntime.dll
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Projects/Petri.csproj
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Projects/PetriMac.csproj
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Projects/CSRuntime.csproj
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Test/Test.csproj

editor: builddir mac
	$(MSBUILD) /nologo /verbosity:minimal /property:Configuration=$(CSCONF) Editor/Projects/Petri.csproj

ifeq ($(shell uname -s),Darwin)

mac:
	$(MSBUILD) /nologo /verbosity:minimal /property:Configuration=$(CSCONF) Editor/Projects/PetriMac.csproj

else

mac:

endif


test: all
	@ln -sf "$(abspath Editor/bin/CSRuntime.dll)" "$(abspath Examples/)" || true
	$(MSBUILD) /nologo /verbosity:minimal /property:Configuration=$(CSCONF) Editor/Test/Test.csproj
	nunit-console Editor/Test/Test.csproj

builddir:
	@mkdir -p build/json/Runtime/Cpp/detail/jsoncpp/src/lib_json
	@mkdir -p build/Runtime/Cpp/detail
	@mkdir -p build/Runtime/C/detail
	@mkdir -p Editor/Test/bin
	@mkdir -p Editor/bin

lib: builddir buildlib
	$(MSBUILD) /nologo /verbosity:minimal /property:Configuration=$(CSCONF) Editor/Projects/CSRuntime.csproj

buildlib: $(CXXOBJ) $(JSONOBJ)
	$(CXX) -o Runtime/$(OUTPUT) $^ $(LDFLAGS)
	@ln -sf "$(abspath Runtime/$(OUTPUT))" "$(abspath Editor/Test/bin/$(OUTPUT))" || true
	@ln -sf "$(abspath Runtime/$(OUTPUT))" "$(abspath Editor/bin/$(OUTPUT))" || true
	@ln -sf "$(abspath Runtime/$(OUTPUT))" "$(abspath Editor/Petri.app/Contents/MonoBundle/$(OUTPUT))" 2>/dev/null || true

build/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS)

build/json/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS) $(WARN_JSON)

examples: editor
	@find Examples -name "*.petri" -exec mono Editor/bin/Petri.exe -gcv {} \;

examplesclean: editor
	@find Examples -name "*.petri" -exec mono Editor/bin/Petri.exe -kv {} \;

