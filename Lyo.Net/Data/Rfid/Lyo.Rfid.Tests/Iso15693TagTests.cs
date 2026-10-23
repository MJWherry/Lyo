namespace Lyo.Rfid.Tests;

public sealed class Iso15693TagTests
{
    private static readonly byte[] Uid = Convert.FromHexString("E004035012345678");

    [Fact]
    public async Task OpenAsync_SystemInformation_SetsUidAndBlockSize()
    {
        await using var tag = await Iso15693Tag.OpenAsync(new ScriptedTransceiver(), TestCt);
        Assert.Equal("E004035012345678", tag.UidHex);
        Assert.Equal(4, tag.BlockSize);
        Assert.Equal(8, tag.BlockCount);
    }

    [Fact]
    public async Task ReadBlocksAsync_UserArea_OneMultiBlockFrame()
    {
        var transceiver = new ScriptedTransceiver();
        var user = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        user.CopyTo(transceiver.UserMemory, 0);
        await using var tag = await Iso15693Tag.OpenAsync(transceiver, TestCt);
        transceiver.Sent.Clear();
        var read = await tag.ReadBlocksAsync(0, 8, TestCt);
        Assert.Equal(user, read);
        Assert.Single(transceiver.Sent, frame => frame[1] == 0x23);
    }

    [Fact]
    public async Task WriteBlockAsync_OneBlock_SendsWriteSingleBlock()
    {
        var transceiver = new ScriptedTransceiver();
        await using var tag = await Iso15693Tag.OpenAsync(transceiver, TestCt);
        transceiver.Sent.Clear();
        var four = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        await tag.WriteBlockAsync(2, four, TestCt);
        var write = Assert.Single(transceiver.Sent, frame => frame[1] == 0x21);
        Assert.Equal(2, write[10]);
        Assert.Equal(four, write.AsSpan(11, 4).ToArray());
    }

    [Fact]
    public async Task ReadBlocksAsync_ErrorFlag_Throws()
    {
        var transceiver = new ScriptedTransceiver { ErrorOnRead = true };
        await using var tag = await Iso15693Tag.OpenAsync(transceiver, TestCt);
        await Assert.ThrowsAsync<RfidException>(() => tag.ReadBlocksAsync(0, 1, TestCt));
    }

    private sealed class ScriptedTransceiver : IIso15693Transceiver
    {
        public List<byte[]> Sent { get; } = [];

        public byte[] UserMemory { get; } = new byte[32];

        public bool ErrorOnRead { get; set; }

        public Task<ReadOnlyMemory<byte>> TransceiveAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
        {
            var bytes = frame.ToArray();
            Sent.Add(bytes);
            var command = bytes[1];
            if (command == 0x01)
                return Task.FromResult(Respond(InventoryPayload()));

            if (command == 0x2B)
                return Task.FromResult(Respond(SystemInfoPayload()));

            if (command == 0x23)
                return Task.FromResult(ErrorOnRead ? Respond([], true) : Respond(UserMemory));

            return Task.FromResult(Respond([]));
        }

        private static byte[] InventoryPayload()
        {
            var payload = new byte[9];
            CopyUidLeastSignificantFirst(payload.AsSpan(1));
            return payload;
        }

        private static byte[] SystemInfoPayload()
        {
            var payload = new byte[11];
            payload[0] = 0x04;
            CopyUidLeastSignificantFirst(payload.AsSpan(1));
            payload[9] = 0x07;
            payload[10] = 0x03;
            return payload;
        }

        private static void CopyUidLeastSignificantFirst(Span<byte> destination)
        {
            for (var i = 0; i < 8; i++)
                destination[i] = Uid[7 - i];
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