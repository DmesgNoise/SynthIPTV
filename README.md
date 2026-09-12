# SynthIPTV

Reliable live TV normalization for Emby and Jellyfin.

SynthIPTV combines supported live TV sources behind a single tuner interface and provides stable M3U and XMLTV output for your media server. It handles provider-specific ingest, persistent channel numbering, shared stream workers, clear and AES-128 sources, and supported protected sources.

SynthIPTV runs in Docker and provides a web interface for setup and channel management. Emby and Jellyfin connect to the playlist and guide URLs provided by SynthIPTV.

## Requirements

- AMD64 computer or server
- Docker
- Docker Compose v2
- Internet access

Supported protected sources also require the Synth Protected Runtime and a supported Chrome/Widevine installation on the host.

## Installation

The recommended installation method is Docker Compose.

The SynthIPTV v1.0 Docker image is:

```text
ghcr.io/dmesgnoise/synthiptv:1.0
```

### Docker Compose

Clone the SynthIPTV repository:

```bash
git clone https://github.com/DmesgNoise/SynthIPTV.git
```

Enter the SynthIPTV directory:

```bash
cd SynthIPTV
```

Start SynthIPTV:

```bash
docker compose up -d
```

Docker automatically downloads the SynthIPTV image from GitHub Container Registry and starts the container.

The default web interface port is:

```text
8892
```

The default configuration directory is:

```text
./config
```

When installation is complete, open:

```text
http://SERVER-IP:8892
```

Replace `SERVER-IP` with the IP address of the computer running SynthIPTV.

To change the port or configuration directory, copy the included example environment file:

```bash
cp .env.example .env
```

Edit `.env` as needed.

Example:

```text
SYNTHIPTV_PORT=8892
SYNTHIPTV_CONFIG=./config
SYNTHIPTV_IMAGE=ghcr.io/dmesgnoise/synthiptv:1.0
```

Then start or recreate the container:

```bash
docker compose up -d
```

Keep the config directory when updating SynthIPTV.

### Portainer

SynthIPTV can be deployed as a Portainer stack.

Create a new stack named:

```text
synthiptv
```

Use this Compose configuration:

```yaml
services:
  synthiptv:
    image: ghcr.io/dmesgnoise/synthiptv:1.0
    container_name: synthiptv
    restart: unless-stopped
    ports:
      - "8892:8892"
    volumes:
      - /opt/synthiptv/config:/opt/synthiptv/config
      - /run/synth-protected:/run/synth-protected
```

Change `/opt/synthiptv/config` to the permanent host directory where you want SynthIPTV configuration stored.

The `/run/synth-protected` mount is used only when the optional protected runtime is installed.

Deploy the stack.

When deployment is complete, open:

```text
http://SERVER-IP:8892
```

Keep the host config directory when updating or replacing the SynthIPTV container.

### Manual Docker

SynthIPTV can also be run directly with Docker.

Create a persistent configuration directory:

```bash
mkdir -p ~/.config/synthiptv
```

Pull the SynthIPTV image:

```bash
docker pull ghcr.io/dmesgnoise/synthiptv:1.0
```

Start the container:

```bash
docker run -d \
  --name synthiptv \
  --restart unless-stopped \
  -p 8892:8892 \
  -v "$HOME/.config/synthiptv:/opt/synthiptv/config" \
  -v /run/synth-protected:/run/synth-protected \
  ghcr.io/dmesgnoise/synthiptv:1.0
```

Open:

```text
http://SERVER-IP:8892
```

Keep `~/.config/synthiptv` when replacing or updating the container.

The `/run/synth-protected` mount is harmless when the optional protected runtime is not installed.

## Setup

Open the SynthIPTV web interface.

Use the guided setup to choose your media server and configure the available providers and channels.

SynthIPTV displays the M3U playlist URL and XMLTV guide URL in the web interface. Use those URLs in your media server without modifying them.

## Emby

SynthIPTV was developed primarily against Emby and works as a normal M3U tuner plus XMLTV guide source.

In Emby:

1. Open Live TV setup.
2. Add the M3U tuner URL shown by SynthIPTV.
3. Add the XMLTV guide URL shown by SynthIPTV.
4. Refresh guide data.

No special FFmpeg configuration is required for the normal Emby path.

## Jellyfin

Jellyfin uses different Live TV FFmpeg input and timestamp behavior than Emby. SynthIPTV includes a Jellyfin compatibility wrapper for this reason.

During guided setup, select Jellyfin and provide:

- The Jellyfin server URL
- A Jellyfin API key

SynthIPTV will show the Jellyfin-specific M3U URL and the required wrapper setup instructions.

The included wrapper is:

```text
jellyfin/synth-jellyfin-ffmpeg
```

Follow the instructions shown in the SynthIPTV web interface for your Jellyfin installation type.

Do not point Jellyfin at the normal Emby playlist when Jellyfin support has been configured. Use the Jellyfin M3U URL shown by SynthIPTV.

## Protected Sources

Clear and AES-128 sources do not require the protected runtime.

Supported protected sources use the separate Synth Protected Runtime. The runtime is not included in the public repository and does not redistribute Chrome or Widevine.

If you have access to the protected runtime package, set its release URL in `.env`:

```text
SYNTH_PROTECTED_RUNTIME_URL=
```

Then run:

```bash
./install-protected-runtime.sh
```

The installer configures the host runtime and its local socket. The SynthIPTV container communicates with that runtime through:

```text
/run/synth-protected
```

After installation, restart SynthIPTV if needed:

```bash
docker compose up -d
```

## Configuration

Channel selections, numbering, provider settings, and media-server settings are stored outside the SynthIPTV container in the host directory mounted to:

```text
/opt/synthiptv/config
```

Replacing the SynthIPTV container does not remove the configuration as long as the host config directory is preserved.

## Updating

Always keep your existing config directory when updating SynthIPTV.

### Docker Compose

Enter the SynthIPTV directory and run:

```bash
docker compose pull
docker compose up -d
```

### Portainer

Pull or redeploy the current image and recreate the stack while keeping the same host config directory.

### Manual Docker

Pull the current image:

```bash
docker pull ghcr.io/dmesgnoise/synthiptv:1.0
```

Remove the old container:

```bash
docker rm -f synthiptv
```

Then run the same `docker run` command used for installation.

Your configuration remains in the host config directory.

## Checking SynthIPTV

Check that the SynthIPTV container is running:

```bash
docker ps --filter name=synthiptv
```

A running container named `synthiptv` should be listed.

View the SynthIPTV log:

```bash
docker logs -f synthiptv
```

Press `Ctrl+C` to stop viewing the log. SynthIPTV continues running.

The web interface should be available at:

```text
http://SERVER-IP:8892
```

## FFmpeg

SynthIPTV v1.0 contains a custom-built FFmpeg 8.1.2 used as part of its media pipeline.

Build configuration:

```text
--disable-doc --disable-debug --enable-gpl --enable-version3 --enable-gnutls
```

The complete corresponding FFmpeg source for the distributed build is included under:

```text
compliance/ffmpeg/
```

See `FFMPEG-NOTICE.md` for build and licensing information.

## License

SynthIPTV itself is proprietary software. See `LICENSE`.

Third-party software included with or used by SynthIPTV remains subject to its own license terms.
