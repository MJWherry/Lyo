using Lyo.Exceptions;
using Lyo.Rfid;

namespace Lyo.Rfid.Tonie;

/// <summary>
/// Talks to one ICODE SLIX-L tag through <see cref="IIso15693Transceiver"/>.
/// <see cref="PrepareCustomTagAsync"/> writes 32 bytes of user memory and enables privacy so a Toniebox will accept the tag.
/// Place audio on the box SD card under <see cref="TonieContentPath"/> for that UID.
/// </summary>
public static class TonieTagSession
{
    /// <summary>Inventory response UID, in display order (<c>E0</c> first).</summary>
    public static async Task<byte[]> InventoryAsync(IIso15693Transceiver transceiver, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(transceiver);
        var payload = await ExchangeAsync(transceiver, SlixFrames.Inventory(), cancellationToken).ConfigureAwait(false);
        if (payload.Length != 9)
            throw new RfidException("ISO 15693 inventory response did not include a UID.");

        // Payload is DSFID plus the UID, least-significant byte first. Display order starts with E0.
        var display = new byte[8];
        for (var i = 0; i < 8; i++)
            display[i] = payload[8 - i];

        return display;
    }

    /// <summary>
    /// Unlocks privacy with <paramref name="password"/> (default <see cref="ToniePrivacy.NxpDefault"/> when null) and returns the 32-byte user memory.
    /// </summary>
    public static async Task<byte[]> ReadUserMemoryAsync(IIso15693Transceiver transceiver, ReadOnlyMemory<byte> uid, ReadOnlyMemory<byte>? password, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(transceiver);
        var key = PasswordOrDefault(password);
        await UnlockAsync(transceiver, uid, key, cancellationToken).ConfigureAwait(false);
        var payload = await ExchangeAsync(transceiver, SlixFrames.ReadMultipleBlocks(uid.Span, firstBlock: 0, blockCount: 8), cancellationToken).ConfigureAwait(false);
        if (payload.Length < 32)
            throw new RfidException("SLIX-L user memory response was short.");

        return payload.AsSpan(0, 32).ToArray();
    }

    /// <summary>
    /// Writes <paramref name="userMemory"/> (32 bytes, or zeros when null), then enables privacy.
    /// <paramref name="password"/> defaults to <see cref="ToniePrivacy.NxpDefault"/>.
    /// </summary>
    public static async Task PrepareCustomTagAsync(
        IIso15693Transceiver transceiver,
        ReadOnlyMemory<byte> uid,
        ReadOnlyMemory<byte>? userMemory,
        ReadOnlyMemory<byte>? password,
        CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(transceiver);
        _ = TonieContentPath.FromUid(uid.Span);
        var blocks = userMemory ?? new byte[32];
        ArgumentHelpers.ThrowIfNotEqual(blocks.Length, 32);
        var key = PasswordOrDefault(password);
        await UnlockAsync(transceiver, uid, key, cancellationToken).ConfigureAwait(false);
        for (byte block = 0; block < 8; block++) {
            var page = Block(blocks, block);
            await ExchangeAsync(transceiver, SlixFrames.WriteSingleBlock(uid.Span, block, page), cancellationToken).ConfigureAwait(false);
        }

        var random = await GetRandomAsync(transceiver, uid, cancellationToken).ConfigureAwait(false);
        var xor = SlixPrivacy.XorPassword(key.Span, random);
        await ExchangeAsync(transceiver, SlixFrames.EnablePrivacy(uid.Span, xor), cancellationToken).ConfigureAwait(false);
    }

    private static async Task UnlockAsync(IIso15693Transceiver transceiver, ReadOnlyMemory<byte> uid, ReadOnlyMemory<byte> password, CancellationToken cancellationToken)
    {
        var random = await GetRandomAsync(transceiver, uid, cancellationToken).ConfigureAwait(false);
        var xor = SlixPrivacy.XorPassword(password.Span, random);
        await ExchangeAsync(transceiver, SlixFrames.SetPassword(uid.Span, xor), cancellationToken).ConfigureAwait(false);
    }

    private static byte[] Block(ReadOnlyMemory<byte> memory, byte block)
    {
        var page = new byte[4];
        memory.Span.Slice(block * 4, 4).CopyTo(page);
        return page;
    }

    private static async Task<byte[]> GetRandomAsync(IIso15693Transceiver transceiver, ReadOnlyMemory<byte> uid, CancellationToken cancellationToken)
    {
        var payload = await ExchangeAsync(transceiver, SlixFrames.GetRandom(uid.Span), cancellationToken).ConfigureAwait(false);
        if (payload.Length < 2)
            throw new RfidException("GET RANDOM NUMBER did not return two bytes.");

        return payload.AsSpan(0, 2).ToArray();
    }

    private static async Task<byte[]> ExchangeAsync(IIso15693Transceiver transceiver, byte[] request, CancellationToken cancellationToken)
    {
        var response = await transceiver.TransceiveAsync(request, cancellationToken).ConfigureAwait(false);
        return Iso15693Response.Payload(response.Span);
    }

    private static ReadOnlyMemory<byte> PasswordOrDefault(ReadOnlyMemory<byte>? password)
    {
        var key = password ?? ToniePrivacy.NxpDefault;
        ArgumentHelpers.ThrowIfNotEqual(key.Length, 4);
        return key;
    }
}
