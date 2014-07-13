#!/bin/sh
mkdir -p bin
(cd src && mcs *.cs -r:/usr/lib/cli/OpenTK-1.0/OpenTK.dll -r:System.Drawing -out:../bin/lifecube.exe)

