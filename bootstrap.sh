#!/bin/bash

dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

oldpwd=$(pwd)

cd "$dir"

git submodule update --init

cd "$oldpwd"


