#!/bin/sh
set -eu

# launchd supplies the repository working directory and explicit developer PATH.
docker desktop start --timeout 180
exec node tools/dev/start-local.mjs
