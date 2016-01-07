CXXSRC:=$(wildcard Runtime/Cpp/*.cpp) $(wildcard Runtime/C/*.cpp) $(wildcard Runtime/Cpp/jsoncpp/src/lib_json/*.cpp)
CXXOBJ:=$(CXXSRC:%.cpp=build/%.o)

WARN:=-Wall -Wunused-value -Wuninitialized

CXX:=c++
CXXVERSION:=$(shell $(CXX) --version)

CXXFLAGS:=-std=c++14 -I./Runtime/Cpp/jsoncpp/include
LDFLAGS:=-shared

ifneq (,$(findstring clang,$(CXXVERSION)))
# if the compiler is clang++

WARN:=$(WARN) -Werror=return-stack-address
CXXFLAGS:=$(CXXFLAGS) -arch i386 -arch x86_64
LDFLAGS:=$(LDFLAGS) -undefined dynamic_lookup -flat_namespace -arch i386 -arch x86_64

else
# else, we assume g++

WARN:=$(WARN) -Werror=return-local-addr
CXXFLAGS:=$(CXXFLAGS) -fPIC
LDFLAGS:=$(LDFLAGS) -fPIC

endif

CXXFLAGS:=$(WARN) $(CXXFLAGS)

OUTPUT:=libPetriRuntime.so


.PHONY: builddir

all: builddir lib

clean:
	rm -rf build
	rm -f Runtime/$(OUTPUT)

builddir:
	mkdir -p build/Runtime/Cpp/jsoncpp/src/lib_json
	mkdir -p build/Runtime/C
	mkdir -p Editor/Test/bin
	mkdir -p Editor/bin

lib: $(CXXOBJ)
	$(CXX) -o Runtime/$(OUTPUT) $^ $(LDFLAGS)
	ln -sf $(abspath Runtime/$(OUTPUT)) Editor/Test/bin/$(OUTPUT)
	ln -sf $(abspath Runtime/$(OUTPUT)) Editor/bin/$(OUTPUT)

build/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS)
