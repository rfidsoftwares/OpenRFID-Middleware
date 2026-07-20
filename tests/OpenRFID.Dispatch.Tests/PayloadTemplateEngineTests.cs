using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Dispatch.Templating;
using Xunit;

namespace OpenRFID.Dispatch.Tests;

public class PayloadTemplateEngineTests
{
    [Fact]
    public void PayloadTemplateEngine_Render_DefaultJsonArrayTemplate_RendersValidJson()
    {
        var engine = new PayloadTemplateEngine();
        var config = new ReaderConfig { ReaderId = "gate-1", ProviderId = "identium", BrandName = "Identium 4-Port" };

        var tags = new[]
        {
            new TagReadEvent { EPC = "E2801111", TID = "90001", RSSI = -55.5f, AntennaPort = 1, ReaderId = "gate-1" },
            new TagReadEvent { EPC = "E2802222", TID = "90002", RSSI = -60.0f, AntennaPort = 2, ReaderId = "gate-1" }
        };

        string output = engine.Render(PayloadTemplateEngine.DefaultJsonArrayTemplate, tags, config, "tx-12345");

        Assert.Contains("\"deviceId\": \"gate-1\"", output);
        Assert.Contains("\"transactionId\": \"tx-12345\"", output);
        Assert.Contains("\"epc\": \"E2801111\"", output);
        Assert.Contains("\"epc\": \"E2802222\"", output);
        Assert.Contains("\"tagCount\": 2", output);
    }

    [Fact]
    public void PayloadTemplateEngine_Render_FormUrlEncodedTemplate_RendersExpectedQuery()
    {
        var engine = new PayloadTemplateEngine();
        var config = new ReaderConfig { ReaderId = "gate-2", ProviderId = "tcp-socket" };
        var tags = new[] { new TagReadEvent { EPC = "E2809999", RSSI = -42.0f, ReaderId = "gate-2" } };

        string output = engine.Render(PayloadTemplateEngine.FormUrlEncodedTemplate, tags, config, "tx-999");

        Assert.Equal("deviceId=gate-2&transactionId=tx-999&epc=E2809999&rssi=-42", output);
    }
}
