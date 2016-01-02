#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo '#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

configuration="Release"
if [[ $# = 1 ]]; then
    configuration="Debug"
fi

oldpwd=$(pwd)
cd "$dir"/../Runtime
make clean
cd "$oldpwd"

mdtool build -t:Clean -c:"$configuration" "$dir"/Petri.sln
' > "$dir"/clean.sh

chmod +x "$dir"/clean.sh

configuration="Release"
if [[ $# = 1 ]]; then
    configuration="Debug"
fi

oldpwd=$(pwd)
cd "$dir"/../Runtime
make
cd "$oldpwd"

mdtool build -c:"$configuration" "$dir"/Petri.sln

