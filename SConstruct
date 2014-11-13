# Petri/SConstruct

import sys
sys.path.append('../../../..')
from scons_tools import *

env = createEnvironment(['pthreads', 'Outils'])

src_list = Split("""
KillableThread.cpp
ManagedMemoryHeap.cpp
""")

env.StaticLibrary('Petri', src_list)
