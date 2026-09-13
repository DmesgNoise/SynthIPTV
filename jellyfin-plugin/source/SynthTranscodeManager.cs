using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace SynthIPTV.Jellyfin.Compatibility;

/// <summary>
/// Thin decorator around Jellyfin's stock ITranscodeManager.
///
/// It changes exactly one thing: immediately before Jellyfin launches FFmpeg,
/// a positively identified SynthIPTV Live TV command receives the same six
/// argument corrections already proven by SynthIPTV's external compatibility
/// wrapper. Every non-Synth invocation is passed to Jellyfin byte-for-byte
/// unchanged.
/// </summary>
public sealed class SynthTranscodeManager : ITranscodeManager, IDisposable
{
    private static readonly Regex ReToken = new(
        @"(?<!\S)-re(?=\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReadrateCatchupToken = new(
        "(?<!\\S)-readrate_catchup\\s+(?:\\\"[^\\\"]*\\\"|'[^']*'|\\S+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CopyTsToken = new(
        @"(?<!\S)-copyts(?=\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StartAtZeroToken = new(
        @"(?<!\S)-start_at_zero(?=\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AvoidNegativeTsToken = new(
        "(?<!\\S)-avoid_negative_ts\\s+(?:\\\"[^\\\"]*\\\"|'[^']*'|\\S+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FFlagsToken = new(
        "(?<!\\S)-fflags\\s+(?<flags>\\\"[^\\\"]*\\\"|'[^']*'|\\S+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITranscodeManager _inner;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<SynthTranscodeManager> _logger;
    private bool _disposed;

    public SynthTranscodeManager(
        ITranscodeManager inner,
        IMediaSourceManager mediaSourceManager,
        IConfigurationManager configurationManager,
        ILogger<SynthTranscodeManager> logger)
    {
        _inner = inner;
        _mediaSourceManager = mediaSourceManager;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public TranscodingJob? GetTranscodingJob(string playSessionId)
        => _inner.GetTranscodingJob(playSessionId);

    public TranscodingJob? GetTranscodingJob(string path, TranscodingJobType type)
        => _inner.GetTranscodingJob(path, type);

    public void PingTranscodingJob(string playSessionId, bool? isUserPaused)
        => _inner.PingTranscodingJob(playSessionId, isUserPaused);

    public Task KillTranscodingJobs(
        string deviceId,
        string? playSessionId,
        Func<string, bool> deleteFiles)
        => _inner.KillTranscodingJobs(deviceId, playSessionId, deleteFiles);

    public void ReportTranscodingProgress(
        TranscodingJob job,
        StreamState state,
        TimeSpan? transcodingPosition,
        float? framerate,
        double? percentComplete,
        long? bytesTranscoded,
        int? bitRate)
        => _inner.ReportTranscodingProgress(
            job,
            state,
            transcodingPosition,
            framerate,
            percentComplete,
            bytesTranscoded,
            bitRate);

    public Task<TranscodingJob> StartFfMpeg(
        StreamState state,
        string outputPath,
        string commandLineArguments,
        Guid userId,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource,
        string? workingDirectory = null)
    {
        if (IsSynthLiveTv(state))
        {
            commandLineArguments = RewriteSynthArguments(commandLineArguments);
            _logger.LogInformation(
                "[SYNTH-JF] Live TV timestamp compatibility active");
        }

        return _inner.StartFfMpeg(
            state,
            outputPath,
            commandLineArguments,
            userId,
            transcodingJobType,
            cancellationTokenSource,
            workingDirectory);
    }

    public TranscodingJob? OnTranscodeBeginRequest(
        string path,
        TranscodingJobType type)
        => _inner.OnTranscodeBeginRequest(path, type);

    public void OnTranscodeEndRequest(TranscodingJob job)
        => _inner.OnTranscodeEndRequest(job);

    public ValueTask<IDisposable> LockAsync(
        string outputPath,
        CancellationToken cancellationToken)
        => _inner.LockAsync(outputPath, cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Positive identification happens inside Jellyfin, not by inspecting an
    /// arbitrary FFmpeg command. The active LiveStreamId resolves to Jellyfin's
    /// ILiveStream, which carries its TunerHostId. We then inspect the actual
    /// configured tuner and require its URL path to be SynthIPTV's dedicated
    /// /jellyfin.m3u endpoint.
    ///
    /// This exactly matches the authoritative SynthIPTV release: /jellyfin.m3u
    /// emits /stream/&lt;cid&gt;?client=jellyfin, while the generic playlist does not.
    /// SynthIPTV's existing API-key integration remains responsible for mapping
    /// that Jellyfin session and closing the LiveStreamId after the last client
    /// disconnects; the plugin does not duplicate or replace that lifecycle.
    /// </summary>
    private bool IsSynthLiveTv(StreamState state)
    {
        var liveStreamId = state.Request?.LiveStreamId;

        if (string.IsNullOrWhiteSpace(liveStreamId))
        {
            liveStreamId = state.MediaSource?.LiveStreamId;
        }

        if (string.IsNullOrWhiteSpace(liveStreamId))
        {
            return false;
        }

        try
        {
            var liveStream = _mediaSourceManager.GetLiveStreamInfo(liveStreamId);

            if (string.IsNullOrWhiteSpace(liveStream.TunerHostId))
            {
                return false;
            }

            var liveTvOptions =
                _configurationManager.GetConfiguration<LiveTvOptions>("livetv");

            var tuner = liveTvOptions.TunerHosts.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    liveStream.TunerHostId,
                    StringComparison.OrdinalIgnoreCase));

            return tuner is not null && IsSynthJellyfinPlaylist(tuner.Url);
        }
        catch (Exception exception)
        {
            // Fail closed. If positive Synth identification fails for any reason,
            // do not touch Jellyfin's command line.
            _logger.LogDebug(
                exception,
                "[SYNTH-JF] Source identification failed; FFmpeg arguments left unchanged");

            return false;
        }
    }

    private static bool IsSynthJellyfinPlaylist(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath
                .TrimEnd('/')
                .EndsWith("/jellyfin.m3u", StringComparison.OrdinalIgnoreCase);
        }

        var path = url.Split('?', '#')[0].TrimEnd('/');
        return path.EndsWith(
            "/jellyfin.m3u",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mirrors the authoritative release wrapper exactly:
    /// - remove -re
    /// - remove -readrate_catchup and its value
    /// - remove -copyts
    /// - remove -start_at_zero
    /// - remove igndts from -fflags and preserve/add genpts
    /// - force -avoid_negative_ts make_zero
    /// </summary>
    internal static string RewriteSynthArguments(string arguments)
    {
        var rewritten = ReToken.Replace(arguments, string.Empty);
        rewritten = ReadrateCatchupToken.Replace(rewritten, string.Empty);
        rewritten = CopyTsToken.Replace(rewritten, string.Empty);
        rewritten = StartAtZeroToken.Replace(rewritten, string.Empty);
        rewritten = AvoidNegativeTsToken.Replace(
            rewritten,
            "-avoid_negative_ts make_zero");

        rewritten = FFlagsToken.Replace(rewritten, match =>
        {
            var rawFlags = match.Groups["flags"].Value;
            var quote = rawFlags.Length >= 2
                && ((rawFlags[0] == '"' && rawFlags[^1] == '"')
                    || (rawFlags[0] == '\'' && rawFlags[^1] == '\''))
                    ? rawFlags[0].ToString()
                    : string.Empty;

            var flags = quote.Length == 0
                ? rawFlags
                : rawFlags[1..^1];

            flags = flags.Replace(
                "+igndts",
                string.Empty,
                StringComparison.Ordinal);
            flags = flags.Replace(
                "igndts+",
                string.Empty,
                StringComparison.Ordinal);
            flags = flags.Replace(
                "igndts",
                string.Empty,
                StringComparison.Ordinal);

            if (!flags.Contains("genpts", StringComparison.Ordinal))
            {
                flags += "+genpts";
            }

            return quote.Length == 0
                ? "-fflags " + flags
                : "-fflags " + quote + flags + quote;
        });

        return Regex.Replace(rewritten, @"\s{2,}", " ").Trim();
    }
}
