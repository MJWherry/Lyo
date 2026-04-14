﻿using System.Text.Json;
using Lyo.Common.Records;

namespace Lyo.Api.Client;

public interface IApiClient : IDisposable
{
    JsonSerializerOptions GetSerializerOptions();

    HttpClient GetClient();

    Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file as a stream without buffering the entire response in memory.
    /// The caller owns the returned <see cref="HttpResponseMessage"/> and must dispose it.
    /// </summary>
    Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);
}