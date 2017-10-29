using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Prtg.RabbitMq.Sensor
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PrtgUnit
    {
        BytesBandwidth,
        BytesMemory,
        BytesDisk,
        Temperature,
        Percent,
        TimeResponse,
        TimeSeconds,
        Custom,
        Count,
        CPU,
        BytesFile,
        SpeedDisk,
        SpeedNet,
        TimeHours
    }

    public class PrtgResponse
    {
        public List<PrtgResult> result { get; set; } = new List<PrtgResult>();
    }

    public class PrtgResult
    {
        public string channel { get; set; }
        public string value { get; set; }
        public PrtgUnit unit { get; set; }
        public int? ShowChart { get; set; }
        public int? Float { get; set; }
    }
}