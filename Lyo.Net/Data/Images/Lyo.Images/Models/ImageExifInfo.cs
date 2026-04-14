namespace Lyo.Images.Models;

/// <summary>Structured EXIF metadata extracted from images (camera info, location, date taken, etc.).</summary>
/// <param name="CameraMake">Camera or device manufacturer (e.g., "Apple", "Canon").</param>
/// <param name="CameraModel">Camera or device model (e.g., "iPhone 15 Pro", "EOS R5").</param>
/// <param name="Software">Software used to create or edit the image.</param>
/// <param name="DateTimeTaken">When the photo was taken (original capture time).</param>
/// <param name="DateTimeDigitized">When the image was digitized.</param>
/// <param name="Latitude">GPS latitude in decimal degrees (-90 to 90). Null if not present.</param>
/// <param name="Longitude">GPS longitude in decimal degrees (-180 to 180). Null if not present.</param>
/// <param name="AltitudeMeters">GPS altitude in meters (positive = above sea level). Null if not present.</param>
/// <param name="ExposureTime">Exposure time in seconds (e.g., 1/125). Null if not present.</param>
/// <param name="FNumber">F-number / aperture (e.g., 2.8). Null if not present.</param>
/// <param name="IsoSpeed">ISO speed rating. Null if not present.</param>
/// <param name="Orientation">Image orientation (1=Normal, 3=180°, 6=90° CW, 8=90° CCW). Null if not present.</param>
public sealed record ImageExifInfo(
    string? CameraMake = null,
    string? CameraModel = null,
    string? Software = null,
    DateTime? DateTimeTaken = null,
    DateTime? DateTimeDigitized = null,
    double? Latitude = null,
    double? Longitude = null,
    double? AltitudeMeters = null,
    double? ExposureTime = null,
    double? FNumber = null,
    ushort? IsoSpeed = null,
    ushort? Orientation = null);