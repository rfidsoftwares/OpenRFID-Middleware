using OpenRFID.Core.Abstractions;
using OpenRFID.Plugins.LLRP;
using Xunit;

namespace OpenRFID.Plugins.Tests;

public class LlrpDriverTests
{
    [Fact]
    public void LlrpMessageDecoder_DecodeRoAccessReport_ValidHeader_ParsesTag()
    {
        // Construct minimal synthetic LLRP RO_ACCESS_REPORT message
        // Header: Type=61, Length=27, MessageID=1
        // TagReportData parameter: Type=240, Length=17
        // EPC-96 parameter: Type=140, 12 bytes EPC "112233445566778899AABBCC"
        byte[] epcBytes = Convert.FromHexString("112233445566778899AABBCC");
        byte[] packet = new byte[27];

        // Header: type=61 (0x003D)
        packet[0] = 0x00;
        packet[1] = 0x3D;
        // Length = 27 (0x0000001B)
        packet[2] = 0x00;
        packet[3] = 0x00;
        packet[4] = 0x00;
        packet[5] = 0x1B;
        // Message ID = 1
        packet[6] = 0x00;
        packet[7] = 0x00;
        packet[8] = 0x00;
        packet[9] = 0x01;

        // Parameter TagReportData: type=240 (0x00F0), length=17 (0x0011)
        packet[10] = 0x00;
        packet[11] = 0xF0;
        packet[12] = 0x00;
        packet[13] = 0x11;

        // Parameter EPC-96 (TV Param): type=140 (0x8C)
        packet[14] = 0x8C;
        Array.Copy(epcBytes, 0, packet, 15, 12);

        var decoded = LlrpMessageDecoder.DecodeRoAccessReport(packet);

        Assert.Single(decoded);
        Assert.Equal("112233445566778899AABBCC", decoded[0].EPC);
    }
}
