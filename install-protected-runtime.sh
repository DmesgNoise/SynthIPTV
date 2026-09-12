#!/usr/bin/env bash

main() {
  local url="${SYNTH_PROTECTED_RUNTIME_URL:-}"
  local port="${SYNTHIPTV_PORT:-8892}"
  local work=""

  if [[ -z "$url" ]]; then
    echo "[SYNTH] SYNTH_PROTECTED_RUNTIME_URL is not configured."
    echo "[SYNTH] Set it to the official Synth Protected Runtime 1.0 archive URL."
    return 1
  fi

  if ! command -v curl >/dev/null 2>&1; then
    echo "[SYNTH] curl is required."
    return 1
  fi

  work="$(mktemp -d)" || return 1

  echo "[SYNTH] downloading protected runtime"
  curl -fL --progress-bar "$url" -o "$work/runtime.tar.gz" || return 1

  echo "[SYNTH] extracting protected runtime"
  tar -xzf "$work/runtime.tar.gz" -C "$work" || return 1

  if [[ ! -x "$work/synth-protected-runtime/install.sh" ]]; then
    echo "[SYNTH] protected runtime archive is invalid."
    return 1
  fi

  echo "[SYNTH] installing protected runtime"
  SYNTHIPTV_PORT="$port" "$work/synth-protected-runtime/install.sh" || return 1

  rm -rf "$work"
  echo "[SYNTH] protected runtime installation complete"
}

main "$@"
