#!/usr/bin/env bash
ROOT="$(cd "$(dirname "$0")" && pwd)"

main() {
  cd "$ROOT" || return 1

  if ! command -v docker >/dev/null 2>&1; then
    echo "[SYNTH] Docker is required."
    return 1
  fi

  if ! docker compose version >/dev/null 2>&1; then
    echo "[SYNTH] Docker Compose v2 is required."
    return 1
  fi

  if [[ ! -f .env ]]; then
    cp .env.example .env || return 1
  fi

  mkdir -p config || return 1

  local port
  port="$(sed -n 's/^[[:space:]]*SYNTHIPTV_PORT[[:space:]]*=[[:space:]]*//p' .env | tail -n 1 | tr -d '\r' | sed 's/^"//;s/"$//')"
  if [[ -z "$port" ]]; then
    port="8892"
  fi

  local runtime_url
  runtime_url="$(sed -n 's/^[[:space:]]*SYNTH_PROTECTED_RUNTIME_URL[[:space:]]*=[[:space:]]*//p' .env | tail -n 1 | tr -d '\r' | sed 's/^"//;s/"$//')"

  if [[ -n "$runtime_url" ]]; then
    echo "[SYNTH] installing protected-source support"
    SYNTHIPTV_PORT="$port"     SYNTH_PROTECTED_RUNTIME_URL="$runtime_url"       "$ROOT/install-protected-runtime.sh" || return 1
  else
    echo "[SYNTH] protected runtime URL not configured"
    echo "[SYNTH] clear and AES sources remain available"
  fi

  echo "[SYNTH] starting SynthIPTV"
  docker compose up -d || return 1

  echo "[SYNTH] installation complete"
  echo "[SYNTH] open: http://SERVER-IP:$port"
}

main "$@"
