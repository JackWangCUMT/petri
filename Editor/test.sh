#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

configuration="Release"
if [[ $# = 1 ]]; then
    configuration="Debug"
fi

nunit-console --config="$configuration" "$dir"/Test/Test.csproj

