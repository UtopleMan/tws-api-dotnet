#!/bin/sh
# Launch the Client Portal Gateway in the foreground as PID 1 so Docker can stop it
# cleanly. run.sh takes the conf path relative to the gateway dir.
set -e
cd /app/gateway
exec sh bin/run.sh root/conf.yaml
