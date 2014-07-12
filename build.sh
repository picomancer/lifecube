#!/bin/sh
mkdir -p bin
(cd src && mcs *.cs -out:../bin/lifecube.exe)

