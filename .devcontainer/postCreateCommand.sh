#!/usr/bin/env bash
# Reports the installed tooling and restores the standalone Release solution.

set -euo pipefail

dotnet --version
pre-commit --version
dotnet restore ./CasCap.Api.GooglePhotos.Release.slnx
