#!/bin/bash

dir="$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

mkdir -p "$dir/Interop"

for h in "$dir"/../C/*.h; do
    filename=$(basename "$h")
    filename="${filename%.*}"
    echo "// This source file has been generated automatically from ../../C/$filename.h by C2CS.sh. Do not edit by hand.

using System;
using System.Runtime.InteropServices;

namespace Petri.Runtime.Interop {

    public class $filename {
$(sed -n 's/[ 	]*\(.*(.*)\)[ 	]*;/\1;/p' <"$h" \
    | grep -v "\<typedef\>" \
    | sed 's/volatile int64_t \*/IntPtr /g' \
    | sed 's/uint\([0-9]\{1,\}\)_t/UInt\1/g' \
    | sed 's/int\([0-9]\{1,\}\)_t/Int\1/g' \
    | sed 's/callable_t/ActionCallableDel/g' \
    | sed 's/parametrizedCallable_t/ParametrizedActionCallableDel/g' \
    | sed 's/transitionCallable_t/TransitionCallableDel/g' \
    | sed 's/parametrizedTransitionCallable_t/ParametrizedTransitionCallableDel/g' \
    | sed 's/Petri_actionResult_t/Int32/g' \
    | sed 's/char const \*(\*\([^)]*\))()/StringCallableDel \1/g' \
    | sed 's/void \*(\*\([^)]*\))()/PtrCallableDel \1/g' \
    | sed 's/UInt16 (\*\([^)]*\))()/UInt16CallableDel \1/g' \
    | sed 's/char const \*/[MarshalAs(UnmanagedType.LPTStr)] string /g' \
    | sed 's/struct[ 	]\{1,\}[^ 	]\{1,\}[ 	]*\*/IntPtr /g' \
    \
    | sed 's/\(.*\)/        [DllImport("PetriRuntime")]\
        public static extern \1\
/' \
    | sed 's/\(.*\)public static extern \[MarshalAs(UnmanagedType\.LPTStr)\] string/\1public static extern IntPtr/'

)
    }
}
" > "$dir/Interop/$filename"Interop.cs

done

echo "Code generation done."
