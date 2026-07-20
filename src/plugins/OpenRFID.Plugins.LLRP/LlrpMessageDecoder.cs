using System.Buffers.Binary;
using System.Text;

namespace OpenRFID.Plugins.LLRP;

/// <summary>
/// Parser for LLRP (Low Level Reader Protocol v1.0.1 / v1.1) binary message frames.
/// </summary>
public static class LlrpMessageDecoder
{
    public const ushort RO_ACCESS_REPORT_MSG_TYPE = 61;
    public const ushort PARAM_TAG_REPORT_DATA = 240;
    public const ushort PARAM_EPC_96 = 140;
    public const ushort PARAM_EPC_DATA = 241;
    public const ushort PARAM_PEAK_RSSI = 218;
    public const ushort PARAM_ANTENNA_ID = 221;

    public static List<(string EPC, float RSSI, int Antenna)> DecodeRoAccessReport(byte[] buffer)
    {
        var tags = new List<(string EPC, float RSSI, int Antenna)>();
        if (buffer == null || buffer.Length < 10) return tags;

        // LLRP Header: [Type (10 bits), Reserved (6 bits)], [Length (32 bits)], [MessageID (32 bits)]
        ushort msgTypeHeader = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(0, 2));
        ushort msgType = (ushort)(msgTypeHeader & 0x03FF);
        uint length = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(2, 4));

        if (msgType != RO_ACCESS_REPORT_MSG_TYPE || buffer.Length < length)
        {
            return tags;
        }

        int index = 10;
        int maxIndex = (int)length;

        while (index + 4 <= maxIndex)
        {
            ushort paramHeader = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index, 2));
            ushort paramType = (ushort)(paramHeader & 0x03FF);
            ushort paramLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index + 2, 2));

            if (paramLength == 0 || index + paramLength > maxIndex) break;

            if (paramType == PARAM_TAG_REPORT_DATA)
            {
                var (epc, rssi, antenna) = ParseTagReportData(buffer, index + 4, index + paramLength);
                if (!string.IsNullOrEmpty(epc))
                {
                    tags.Add((epc, rssi, antenna));
                }
            }

            index += paramLength;
        }

        return tags;
    }

    private static (string EPC, float RSSI, int Antenna) ParseTagReportData(byte[] buffer, int start, int end)
    {
        string epc = "";
        float rssi = -50f;
        int antenna = 1;

        int index = start;
        while (index < end)
        {
            byte firstByte = buffer[index];
            bool isTvParam = (firstByte & 0x80) != 0;

            if (isTvParam)
            {
                byte tvType = (byte)(firstByte & 0x7F);
                if (firstByte == 0x8C || tvType == 12 || tvType == 13 || tvType == 140) // EPC-96 (TV Param 140 = 0x8C)
                {
                    if (index + 13 <= end)
                    {
                        byte[] epcBytes = new byte[12];
                        Array.Copy(buffer, index + 1, epcBytes, 0, 12);
                        epc = Convert.ToHexString(epcBytes);
                        index += 13;
                        continue;
                    }
                }
                else if (tvType == (PARAM_PEAK_RSSI & 0x7F))
                {
                    if (index + 2 <= end)
                    {
                        sbyte peakRssi = (sbyte)buffer[index + 1];
                        rssi = peakRssi;
                        index += 2;
                        continue;
                    }
                }
                else if (tvType == (PARAM_ANTENNA_ID & 0x7F))
                {
                    if (index + 3 <= end)
                    {
                        antenna = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index + 1, 2));
                        index += 3;
                        continue;
                    }
                }
                index += 1;
            }
            else
            {
                // TLV Parameter
                if (index + 4 > end) break;
                ushort paramHeader = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index, 2));
                ushort paramTypeTlv = (ushort)(paramHeader & 0x03FF);
                ushort paramLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index + 2, 2));
                if (paramLength == 0 || index + paramLength > end) break;

                if (paramTypeTlv == PARAM_EPC_DATA && index + 4 < end)
                {
                    ushort epcBitLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index + 4, 2));
                    int epcByteLen = (epcBitLen + 7) / 8;
                    if (index + 6 + epcByteLen <= end)
                    {
                        byte[] epcBytes = new byte[epcByteLen];
                        Array.Copy(buffer, index + 6, epcBytes, 0, epcByteLen);
                        epc = Convert.ToHexString(epcBytes);
                    }
                }

                index += paramLength;
            }
        }

        return (epc, rssi, antenna);
    }
}
