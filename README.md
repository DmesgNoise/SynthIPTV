# SynthIPTV

SynthIPTV normalizes supported live TV sources into a single tuner interface for Emby and Jellyfin.

It provides:

- Persistent channel numbering
- M3U tuner output
- XMLTV guide output
- Shared live-stream workers
- Clear, AES-128, and supported protected-source handling
- Emby integration
- Jellyfin integration with a Live TV FFmpeg compatibility wrapper

SynthIPTV was developed primarily against Emby. Jellyfin handles Live TV timestamps differently, so Jellyfin installations require the included compatibility wrapper.

## Requirements

- Linux host
- Docker
- Docker Compose v2

Protected-source support additionally requires:

- Official Google Chrome installed on the host
- Node.js and npm
- xvfb-run
- Synth Protected Runtime

SynthIPTV does not distribute Google Chrome or Widevine CDM.

## Basic configuration

Copy `.env.example` to `.env`.

The default configuration is:

SYNTHIPTV_PORT=8892
SYNTHIPTV_CONFIG=./config
SYNTHIPTV_IMAGE=ghcr.io/dmesgnoise/synthiptv:1.0

Then start SynthIPTV with Docker Compose.

After startup, open:

http://SERVER-IP:8892

The web interface guides media-server and provider setup.

## Emby

Use the M3U and XMLTV URLs shown by the SynthIPTV web interface.

## Jellyfin

Jellyfin requires the included `jellyfin/synth-jellyfin-ffmpeg` compatibility wrapper for SynthIPTV Live TV streams.

Follow the Jellyfin setup instructions in the SynthIPTV web interface for your installation type.

## Protected sources

Protected-source support uses a separate closed-source Synth Protected Runtime installed on the Linux host.

The protected runtime does not contain or redistribute Google Chrome or Widevine CDM. Official Google Chrome must be installed separately.

## FFmpeg

SynthIPTV 1.0 contains a custom-built FFmpeg 8.1.2 binary built with:

--disable-doc --disable-debug --enable-gpl --enable-version3 --enable-gnutls

The corresponding source used for the distributed binary is provided under `compliance/ffmpeg/`.

See `FFMPEG-NOTICE.md` for details.

## License

SynthIPTV itself is proprietary software.

Third-party components remain subject to their respective licenses.
