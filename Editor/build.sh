#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo '#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

configuration="Release"
if [[ $# = 1 ]]; then
    configuration="Debug"
fi

mdtool build -t:Clean -c:"$configuration" "$dir"/Petri.sln
' > "$dir"/clean.sh

chmod +x "$dir"/clean.sh

configuration="Release"
if [[ $# = 1 ]]; then
    configuration="Debug"
fi

mdtool build -c:"$configuration" "$dir"/Petri.sln

