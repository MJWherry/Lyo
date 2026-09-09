using Lyo.Encryption.Models;

namespace Lyo.Encryption.Tests;

/// <summary>Protects the headerless <c>[nonce][tag][ciphertext]</c> layout used by <see cref="IRawAead" />.</summary>
public sealed class RawAeadEnvelopeTests
{
    [Fact]
    public void Allocate_Layout_NonceThenTagThenPayload()
    {
        var envelope = RawAeadEnvelope.Allocate(12, 16, 8, randomizeNonce: false);
        Assert.Equal(12 + 16 + 8, envelope.Buffer.Length);
        Assert.Equal(0, envelope.NonceOffset);
        Assert.Equal(12, envelope.NonceLength);
        Assert.Equal(16, envelope.AuthenticatorLength);
        Assert.Equal(8, envelope.PayloadLength);
        envelope.Nonce.Fill(0x11);
        envelope.Authenticator.Fill(0x22);
        envelope.Payload.Fill(0x33);
        Assert.Equal(new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11 }, envelope.Buffer.AsSpan(0, 12).ToArray());
        Assert.All(envelope.Buffer.AsSpan(12, 16).ToArray(), b => Assert.Equal((byte)0x22, b));
        Assert.All(envelope.Buffer.AsSpan(28, 8).ToArray(), b => Assert.Equal((byte)0x33, b));
        Assert.True(envelope.Body.Length == envelope.Buffer.Length);
    }

    [Fact]
    public void Allocate_AesSiv_BodyIsSivThenCiphertext()
    {
        var envelope = RawAeadEnvelope.Allocate(16, 0, 4, randomizeNonce: false);
        envelope.Nonce.Fill(0xAB);
        envelope.Payload.Fill(0xCD);
        Assert.Equal(20, envelope.Buffer.Length);
        Assert.Empty(envelope.Authenticator.ToArray());
        Assert.Equal(new byte[] { 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xCD, 0xCD, 0xCD, 0xCD }, envelope.Buffer);
    }

    [Fact]
    public void Read_SlicesNonceTagAndPayload()
    {
        var envelope = RawAeadEnvelope.Allocate(12, 16, 3, randomizeNonce: false);
        envelope.Nonce.Fill(1);
        envelope.Authenticator.Fill(2);
        envelope.Payload.Fill(3);
        var header = RawAeadEnvelope.Read(envelope.Buffer, 12, 16);
        Assert.Equal(12, header.NonceLength);
        Assert.Equal(16, header.AuthenticatorLength);
        Assert.Equal(28, header.PayloadOffset);
        Assert.Equal(envelope.Nonce.ToArray(), header.Nonce(envelope.Buffer).ToArray());
        Assert.Equal(envelope.Authenticator.ToArray(), header.Authenticator(envelope.Buffer).ToArray());
        Assert.Equal(envelope.Payload.ToArray(), header.Payload(envelope.Buffer).ToArray());
    }

    [Fact]
    public void Read_Truncated_Throws()
        => Assert.Throws<InvalidDataException>(() => RawAeadEnvelope.Read(new byte[EncryptionAlgorithmInfo.AesGcm.AuthenticatorOverheadBytes - 1], 12, 16));
}
