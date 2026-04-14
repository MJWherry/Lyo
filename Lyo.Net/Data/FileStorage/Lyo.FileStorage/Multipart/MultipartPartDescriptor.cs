namespace Lyo.FileStorage.Multipart;

public sealed record MultipartPartDescriptor(int PartNumber, string? PresignedPutUrl, string HttpMethod = "PUT");