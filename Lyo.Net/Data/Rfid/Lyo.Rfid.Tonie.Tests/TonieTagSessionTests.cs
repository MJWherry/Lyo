namespace Lyo.Rfid.Tonie.Tests;

public sealed class TonieTagSessionTests
{
    [Fact]
    public async Task PrepareCustomTag_OnMemoryTransceiver_EnablesPrivacyAndRoundTripsBlocks()
    {
        var uid = Convert.FromHexString("E004035012345678");
        var user = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var transceiver = new MemorySlixTransceiver();
        await TonieTagSession.PrepareCustomTagAsync(transceiver, uid, user, null, TestCt);
        var xor = SlixPrivacy.XorPassword(ToniePrivacy.NxpDefault.Span, [0x12, 0x34]);
        var privacy = SlixFrames.EnablePrivacy(uid, xor);
        Assert.Contains(transceiver.Sent, frame => frame.AsSpan().SequenceEqual(privacy));
        var read = await TonieTagSession.ReadUserMemoryAsync(transceiver, uid, null, TestCt);
        Assert.Equal(user, read);
    }

    [Fact]
    public async Task ReadUserMemoryAsync_UserArea_OneMultiBlockRead()
    {
        var uid = Convert.FromHexString("E004035012345678");
        var transceiver = new MemorySlixTransceiver();
        await TonieTagSession.PrepareCustomTagAsync(transceiver, uid, null, null, TestCt);
        transceiver.Sent.Clear();
        var read = await TonieTagSession.ReadUserMemoryAsync(transceiver, uid, null, TestCt);
        Assert.Equal(32, read.Length);
        Assert.Single(transceiver.Sent, frame => frame[1] == 0x23);
        Assert.DoesNotContain(transceiver.Sent, frame => frame[1] == 0x20);
    }

    private sealed class MemorySlixTransceiver : IIso15693Transceiver
    {
        private readonly byte[] _memory = new byte[32];
        private bool _authed = true;
        private bool _privacy;

        public List<byte[]> Sent { get; } = [];

        public Task<ReadOnlyMemory<byte>> TransceiveAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
        {
            var bytes = frame.ToArray();
            Sent.Add(bytes);
            var command = bytes[1];
            if (command == 0xB2)
                return Task.FromResult(Respond([0x12, 0x34]));

            if (command == 0xB3) {
                _authed = true;
                _privacy = false;
                return Task.FromResult(Respond([]));
            }

            if (command == 0xBA) {
                _privacy = true;
                _authed = false;
                return Task.FromResult(Respond([]));
            }

            if (_privacy && !_authed)
                return Task.FromResult(Respond([0x0F], true));

            if (command == 0x21) {
                var block = bytes[10];
                bytes.AsSpan(11, 4).CopyTo(_memory.AsSpan(block * 4, 4));
                return Task.FromResult(Respond([]));
            }

            if (command == 0x20) {
                var block = bytes[10];
                var data = _memory.AsSpan(block * 4, 4).ToArray();
                return Task.FromResult(Respond(data));
            }

            if (command == 0x23) {
                var first = bytes[10];
                var count = bytes[11] + 1;
                var data = _memory.AsSpan(first * 4, count * 4).ToArray();
                return Task.FromResult(Respond(data));
            }

            return Task.FromResult(Respond([]));
        }

        private static ReadOnlyMemory<byte> Respond(byte[] payload, bool error = false)
        {
            var body = new byte[1 + payload.Length];
            body[0] = error ? (byte)0x01 : (byte)0x00;
            payload.CopyTo(body, 1);
            return Iso15693Crc.Append(body);
        }
    }
}