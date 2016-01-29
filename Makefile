CXXSRC:=$(wildcard Runtime/Cpp/*.cpp) $(wildcard Runtime/C/*.cpp)
CXXOBJ:=$(CXXSRC:%.cpp=build/%.o)
JSONSRC:=$(wildcard Runtime/Cpp/jsoncpp/src/lib_json/*.cpp)
JSONOBJ:=$(JSONSRC:%.cpp=build/json/%.o)

WARN:=-Wall -Wunused-value -Wuninitialized

CXX:=c++
MSBUILD:=xbuild
CXXVERSION:=$(shell $(CXX) --version)

CXXFLAGS:=-std=c++14 -I./Runtime/Cpp/jsoncpp/include
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

.PHONY: builddir editor all clean test

all: lib editor

cleanlib:
	rm -rf build
	rm -f Runtime/$(OUTPUT)

clean: cleanlib
	rm -f Editor/Test/bin/$(OUTPUT)
	rm -f Editor/bin/$(OUTPUT)
	rm -rf Editor/Test/obj
	rm -rf Editor/obj
	rm -rf Editor/Petri.app
	rm -f Editor/Petri.exe
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Projects/Petri.csproj
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Projects/PetriMac.csproj
	$(MSBUILD) /nologo /verbosity:minimal /target:Clean Editor/Test/Test.csproj

editor: builddir mac
	$(MSBUILD) /nologo /verbosity:minimal /property:Configuration=$(CSCONF) Editor/Projects/Petri.csproj
ifeq (, $(shell which mdtool))
mac: ;
else
mac: builddir
	mdtool build -c:$(CSCONF) Editor/Petri.sln
endif

test: all
	mdtool build -c:$(CSCONF) Editor/Petri.sln
	nunit-console Editor/Test/Test.csproj

builddir:
	mkdir -p build/json/Runtime/Cpp/jsoncpp/src/lib_json
	mkdir -p build/Runtime/Cpp
	mkdir -p build/Runtime/C
	mkdir -p Editor/Test/bin
	mkdir -p Editor/bin

lib: builddir buildlib

buildlib: $(CXXOBJ) $(JSONOBJ)
	$(CXX) -o Runtime/$(OUTPUT) $^ $(LDFLAGS)
	ln -sf $(abspath Runtime/$(OUTPUT)) Editor/Test/bin/$(OUTPUT)
	ln -sf $(abspath Runtime/$(OUTPUT)) Editor/bin/$(OUTPUT)

build/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS)

build/json/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS) $(WARN_JSON)
