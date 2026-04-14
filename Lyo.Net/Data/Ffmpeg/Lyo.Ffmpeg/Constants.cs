namespace Lyo.Ffmpeg;

/// <summary>Constants for the Ffmpeg library.</summary>
public static class Constants
{
    /// <summary>Constants for FFmpeg service metric names.</summary>
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
    }
}