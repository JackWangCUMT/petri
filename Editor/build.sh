#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ ! -f "$dir"/clean.sh ]; then
    echo '#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

mdtool build -t:Clean "$dir"/Petri.sln
' > "$dir"/clean.sh

    chmod +x "$dir"/clean.sh
fi

mdtool build "$dir"/Petri.sln

