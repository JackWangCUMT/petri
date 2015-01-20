# Petri/SConstruct

import sys
sys.path.append('../../../..')
from scons_tools import *

env = createEnvironment(['pthreads', 'Outils'])

src_list = Split("""
KillableThread.cpp
ManagedMemoryHeap.cpp
DebugServer.cpp
PetriDebug.cpp
PetriNet.cpp
jsoncpp/src/json_reader.cpp
jsoncpp/src/json_value.cpp
jsoncpp/src/json_writer.cpp
""")

env.StaticLibrary('Petri', src_list)
