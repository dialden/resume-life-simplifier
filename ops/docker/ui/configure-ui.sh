#!/bin/sh
set -eu

: "${API_BASE_URL:?Set API_BASE_URL to the browser-accessible HTTP(S) API address.}"
case "$API_BASE_URL" in
    http://?*|https://?*) ;;
    *) printf '%s\n' 'API_BASE_URL must be an absolute HTTP(S) address.' >&2; exit 1 ;;
esac

jq -n --arg url "$API_BASE_URL" '{ApiBaseUrl: $url}' > /usr/share/nginx/html/appsettings.json
