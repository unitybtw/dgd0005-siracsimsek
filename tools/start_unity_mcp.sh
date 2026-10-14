#!/bin/sh
set -eu
export DISABLE_TELEMETRY=1
exec uvx --from 'mcpforunityserver==10.0.0' mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8080
