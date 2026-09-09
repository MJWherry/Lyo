namespace Lyo.FFmpeg;

/// <summary>Shared names used across the FFmpeg library.</summary>
public static class Constants
{
    /// <summary>Stable <see cref="Lyo.Result.Error.Code" /> values returned on failure.</summary>
    public static class Errors
    {
        public const string FFmpegError = "FFmpegError";
        public const string FFprobeError = "FFprobeError";
        public const string FfplayError = "FfplayError";
        public const string ProbeParseError = "ProbeParseError";
    }

    /// <summary>Metric instrument names for FFmpeg convert, probe, play, and extract.</summary>
    public static class Metrics
    {
        public const string ConvertDuration = "ffmpeg.convert.duration";
        public const string ConvertSuccess = "ffmpeg.convert.success";
        public const string ConvertFailure = "ffmpeg.convert.failure";
        public const string ProbeDuration = "ffmpeg.probe.duration";
        public const string ProbeSuccess = "ffmpeg.probe.success";
        public const string ProbeFailure = "ffmpeg.probe.failure";
        public const string PlayDuration = "ffmpeg.play.duration";
        public const string PlaySuccess = "ffmpeg.play.success";
        public const string PlayFailure = "ffmpeg.play.failure";
        public const string ExtractDuration = "ffmpeg.extract.duration";
        public const string ExtractSuccess = "ffmpeg.extract.success";
        public const string ExtractFailure = "ffmpeg.extract.failure";
    }
}
