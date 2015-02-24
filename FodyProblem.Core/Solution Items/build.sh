#!/bin/bash

mono --runtime=v4.0 .nuget/NuGet.exe install FAKE -Version 3.17.6 -o "packages"
mono --runtime=v4.0 packages/FAKE.3.17.6/tools/FAKE.exe build.fsx $@