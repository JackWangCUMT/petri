CXXSRC:=$(wildcard Cpp/*.cpp) $(wildcard C/*.cpp) $(wildcard Cpp/jsoncpp/src/lib_json/*.cpp)
CXXOBJ:=$(CXXSRC:%.cpp=build/%.o)

WARN:=-Wall -Wunused-value -Wuninitialized

CXX:=c++
CXXVERSION:=$(shell $(CXX) --version)

CXXFLAGS:=-std=c++14 -I./Cpp/jsoncpp/include
LDFLAGS:=-shared

ifneq (,$(findstring clang,$(CXXVERSION)))
# if the compiler is clang++

WARN:=$(WARN) -Werror=return-stack-address
CXXFLAGS:=$(CXXFLAGS) -arch i386 -arch x86_64
LDFLAGS:=$(LDFLAGS) -undefined dynamic_lookup -flat_namespace -arch i386 -arch x86_64

else
# else, we assume g++

WARN:=$(WARN) -Werror=return-local-addr
LDFLAGS:=$(LDFLAGS) -fPIC

endif

CXXFLAGS:=$(WARN) $(CXXFLAGS)

OUTPUT:=libPetriRuntime.so


.PHONY: builddir

all: builddir lib

clean:
	rm -rf build
	rm -f $(OUTPUT)

builddir:
	mkdir -p build/Cpp/jsoncpp/src/lib_json
	mkdir -p build/C

lib: $(CXXOBJ)
	$(CXX) -o $(OUTPUT) $^ $(LDFLAGS)

build/%.o: %.cpp
	$(CXX) -o $@ -c $< $(CXXFLAGS)
