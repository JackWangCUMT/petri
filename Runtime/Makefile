CXXSRC=$(wildcard Cpp/*.cpp) $(wildcard C/*.cpp) $(wildcard Cpp/jsoncpp/src/lib_json/*.cpp)
CXXOBJ=$(CXXSRC:%.cpp=build/%.o)

WARN=-Wall -Wunused-value -Wuninitialized -Werror=return-stack-address

CXX=c++
CXXFLAGS=$(WARN) -std=c++14 -arch i386 -arch x86_64 -I./Cpp/jsoncpp/include

LDFLAGS=-undefined dynamic_lookup -flat_namespace -arch i386 -arch x86_64

OUTPUT=libPetriRuntime.so

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
