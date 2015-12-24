#!/bin/bash

dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

nunit-console "$dir"/Test/Test.csproj

