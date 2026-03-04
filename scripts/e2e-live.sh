#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${CHUMMER_API_BASE_URL:-${CHUMMER_WEB_BASE_URL:-http://127.0.0.1:${CHUMMER_API_PORT:-${CHUMMER_WEB_PORT:-8088}}}}"
XML_FILE="${1:-Chummer.Tests/TestFiles/BLUE.chum5}"
API_KEY="${CHUMMER_API_KEY:-}"

curl_json() {
  local method="$1"
  local url="$2"
  shift 2
  if [[ -n "$API_KEY" ]]; then
    curl -fsS -X "$method" "$url" -H "X-Api-Key: $API_KEY" "$@"
  else
    curl -fsS -X "$method" "$url" "$@"
  fi
}

if [[ ! -f "$XML_FILE" ]]; then
  echo "E2E XML file not found: $XML_FILE" >&2
  exit 1
fi

xml_escaped=$(perl -0777 -pe 's/^\xEF\xBB\xBF//; s/\\/\\\\/g; s/"/\\"/g; s/\r//g; s/\n/\\n/g' "$XML_FILE")
xml_payload="{\"xml\":\"$xml_escaped\"}"
payload_file=$(mktemp)
trap 'rm -f "$payload_file"' EXIT
printf '%s' "$xml_payload" > "$payload_file"

check_post() {
  local path="$1"
  local out status
  local response_file
  response_file=$(mktemp)
  echo "checking: $path"
  if [[ -n "$API_KEY" ]]; then
    status=$(curl -sS -o "$response_file" -w "%{http_code}" -X POST "$BASE_URL$path" -H "Content-Type: application/json" -H "X-Api-Key: $API_KEY" --data-binary "@$payload_file")
  else
    status=$(curl -sS -o "$response_file" -w "%{http_code}" -X POST "$BASE_URL$path" -H "Content-Type: application/json" --data-binary "@$payload_file")
  fi
  out=$(cat "$response_file")
  rm -f "$response_file"
  if [[ "$status" -lt 200 || "$status" -ge 300 ]]; then
    echo "status: $status" >&2
    [[ -n "$out" ]] && echo "$out" >&2
    echo "failed: $path" >&2
    return 1
  fi
  if [[ -z "$out" ]]; then
    echo "Empty response from $path" >&2
    return 1
  fi
  echo "ok: $path"
}

curl_json GET "$BASE_URL/api/info" >/dev/null
curl_json GET "$BASE_URL/api/health" >/dev/null

echo "service healthy at $BASE_URL"

check_post "/api/characters/summary"
check_post "/api/characters/validate"
check_post "/api/characters/sections/profile"
check_post "/api/characters/sections/progress"
check_post "/api/characters/sections/rules"
check_post "/api/characters/sections/build"
check_post "/api/characters/sections/movement"
check_post "/api/characters/sections/awakening"
check_post "/api/characters/sections/attributes"
check_post "/api/characters/sections/attributedetails"
check_post "/api/characters/sections/inventory"
check_post "/api/characters/sections/gear"
check_post "/api/characters/sections/weapons"
check_post "/api/characters/sections/weaponaccessories"
check_post "/api/characters/sections/armors"
check_post "/api/characters/sections/armormods"
check_post "/api/characters/sections/cyberwares"
check_post "/api/characters/sections/vehicles"
check_post "/api/characters/sections/vehiclemods"
check_post "/api/characters/sections/skills"
check_post "/api/characters/sections/qualities"
check_post "/api/characters/sections/contacts"
check_post "/api/characters/sections/spells"
check_post "/api/characters/sections/powers"
check_post "/api/characters/sections/complexforms"
check_post "/api/characters/sections/spirits"
check_post "/api/characters/sections/foci"
check_post "/api/characters/sections/aiprograms"
check_post "/api/characters/sections/martialarts"
check_post "/api/characters/sections/limitmodifiers"
check_post "/api/characters/sections/lifestyles"
check_post "/api/characters/sections/metamagics"
check_post "/api/characters/sections/arts"
check_post "/api/characters/sections/initiationgrades"
check_post "/api/characters/sections/critterpowers"
check_post "/api/characters/sections/mentorspirits"
check_post "/api/characters/sections/expenses"
check_post "/api/characters/sections/sources"
check_post "/api/characters/sections/gearlocations"
check_post "/api/characters/sections/armorlocations"
check_post "/api/characters/sections/weaponlocations"
check_post "/api/characters/sections/vehiclelocations"
check_post "/api/characters/sections/calendar"
check_post "/api/characters/sections/improvements"
check_post "/api/characters/sections/customdatadirectorynames"
check_post "/api/characters/sections/drugs"

curl_json GET "$BASE_URL/api/lifemodules/stages" >/dev/null
curl_json GET "$BASE_URL/api/lifemodules/modules" >/dev/null

echo "live E2E completed"
