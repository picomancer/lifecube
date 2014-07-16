#!/bin/sh
mkdir -p bin
(cd src && mcs *.cs -r:../dep/OpenTK.dll -r:System.Drawing -out:../bin/lifecube.exe)
[ -e bin/OpenTK.dll ] || ln -s ../dep/OpenTK.dll bin/OpenTK.dll
