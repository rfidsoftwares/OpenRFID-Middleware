using OpenRFID.Core.Abstractions;
using OpenRFID.Plugins.Identium;
using OpenRFID.Plugins.Impinj;
using OpenRFID.Plugins.MQTT;
using OpenRFID.Plugins.Zebra;
using Xunit;

namespace OpenRFID.Plugins.Tests;

public class VendorDriverTests
{
    [Fact]
    public async Task VendorProviders_InstantiateConnectionsSuccessfully()
    {
        var config = new ReaderConfig { ReaderId = "vendor-r1", ProviderId = "identium" };

        var identiumProvider = new IdentiumReaderProvider();
        var impinjProvider = new ImpinjReaderProvider();
        var zebraProvider = new ZebraReaderProvider();
        var mqttProvider = new MqttReaderProvider();

        var conn1 = await identiumProvider.CreateConnectionAsync(config);
        var conn2 = await impinjProvider.CreateConnectionAsync(config);
        var conn3 = await zebraProvider.CreateConnectionAsync(config);
        var conn4 = await mqttProvider.CreateConnectionAsync(config);

        Assert.NotNull(conn1);
        Assert.NotNull(conn2);
        Assert.NotNull(conn3);
        Assert.NotNull(conn4);

        Assert.Equal("identium", identiumProvider.ProviderId);
        Assert.Equal("impinj", impinjProvider.ProviderId);
        Assert.Equal("zebra", zebraProvider.ProviderId);
        Assert.Equal("mqtt-broker", mqttProvider.ProviderId);
    }

    [Fact]
    public void MqttConnection_ProcessMqttMessage_EmitsTagReadEventWithTopicMetadata()
    {
        var config = new ReaderConfig { ReaderId = "mqtt-reader-1", ProviderId = "mqtt-broker" };
        var connection = new MqttReaderConnection(config);
        TagReadEvent? emittedTag = null;

        connection.TagRead += (s, e) => emittedTag = e.Tag;

        string json = "{\"epc\":\"E20000000000000000099999\",\"rssi\":-55.0,\"antenna\":3}";
        connection.ProcessMqttMessage("openrfid/tags/gate1", json);

        Assert.NotNull(emittedTag);
        Assert.Equal("E20000000000000000099999", emittedTag.EPC);
        Assert.Equal(-55.0f, emittedTag.RSSI);
        Assert.Equal(3, emittedTag.AntennaPort);
        Assert.True(emittedTag.ExtraMetadata.ContainsKey("MqttTopic"));
        Assert.Equal("openrfid/tags/gate1", emittedTag.ExtraMetadata["MqttTopic"]);
    }
}
