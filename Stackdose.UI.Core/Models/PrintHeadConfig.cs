using System.Text.Json.Serialization;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// PrintHead 配置模型（完全對齊 Feiyang 工業標準 JSON）
    /// </summary>
    public class PrintHeadConfig
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("BoardIP")]
        public string BoardIP { get; set; } = string.Empty;

        [JsonPropertyName("BoardPort")]
        public int BoardPort { get; set; }

        [JsonPropertyName("PcIP")]
        public string PcIP { get; set; } = string.Empty;

        [JsonPropertyName("PcPort")]
        public int PcPort { get; set; }

        [JsonPropertyName("DriverType")]
        public string DriverType { get; set; } = "Feiyang";

        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("Firmware")]
        public FirmwareConfig Firmware { get; set; } = new();

        [JsonPropertyName("PrintMode")]
        public PrintModeConfig PrintMode { get; set; } = new();
    }

    public class FirmwareConfig
    {
        [JsonPropertyName("WaveformPath")]
        public string WaveformPath { get; set; } = string.Empty;

        [JsonPropertyName("EnableTkling")]
        public bool EnableTkling { get; set; }

        [JsonPropertyName("PrintheadColorCount")]
        public int PrintheadColorCount { get; set; }

        [JsonPropertyName("InstallDirectionPositive")]
        public bool InstallDirectionPositive { get; set; }

        [JsonPropertyName("EncoderFunction")]
        public int EncoderFunction { get; set; }

        [JsonPropertyName("HeatTempreture")] // 配合硬體端拼法
        public double HeatTempreture { get; set; }

        [JsonPropertyName("BaseVoltage")]
        public double[] BaseVoltage { get; set; } = Array.Empty<double>();

        [JsonPropertyName("OffsetVoltage")]
        public double[] OffsetVoltage { get; set; } = Array.Empty<double>();

        [JsonPropertyName("JetColor")]
        public int[] JetColor { get; set; } = Array.Empty<int>();

        [JsonPropertyName("DisableColumnMask")]
        public int DisableColumnMask { get; set; }
    }

    public class PrintModeConfig
    {
        [JsonPropertyName("Repeat")]
        public int Repeat { get; set; } = 1;

        [JsonPropertyName("Direction")]
        public string Direction { get; set; } = "Bidirection";

        [JsonPropertyName("GratingDPI")]
        public int GratingDPI { get; set; }

        [JsonPropertyName("GrayScale")]
        public int GrayScale { get; set; }

        [JsonPropertyName("GrayScaleDrop")]
        public int GrayScaleDrop { get; set; }

        [JsonPropertyName("ResetEncoder")]
        public int ResetEncoder { get; set; }

        [JsonPropertyName("PrintBegin")]
        public int PrintBegin { get; set; }

        [JsonPropertyName("XSpace")]
        public int XSpace { get; set; }

        [JsonPropertyName("LColumnCali")]
        public double[] LColumnCali { get; set; } = Array.Empty<double>();

        [JsonPropertyName("RColumnCali")]
        public double[] RColumnCali { get; set; } = Array.Empty<double>();
    }
}
