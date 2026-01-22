using System.Text.Json.Serialization;

namespace Stackdose.UI.Core.Models
{
    ///// <summary>
    ///// 閃噴參數
    ///// </summary>
    //public class SpitParams
    //{
    //    /// <summary>頻率 (kHz)</summary>
    //    public double Frequency { get; set; } = 0.1;

    //    /// <summary>工作時間 (秒)</summary>
    //    public double WorkDuration { get; set; } = 1.0;

    //    /// <summary>閒置時間 (秒)</summary>
    //    public double IdleDuration { get; set; } = 1.0;

    //    /// <summary>液滴數</summary>
    //    public byte Drops { get; set; } = 1;
    //}

    /// <summary>
    /// PrintHead 配置模型（對應 JSON 配置檔）
    /// </summary>
    public class PrintHeadConfig
    {
        [JsonPropertyName("DriverType")]
        public string DriverType { get; set; } = "Feiyang";

        [JsonPropertyName("Model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("MachineType")]
        public string MachineType { get; set; } = string.Empty;

        [JsonPropertyName("HeadIndex")]
        public int HeadIndex { get; set; }

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

        [JsonPropertyName("Waveform")]
        public string Waveform { get; set; } = string.Empty;

        [JsonPropertyName("Firmware")]
        public FirmwareConfig Firmware { get; set; } = new();

        [JsonPropertyName("PrintMode")]
        public PrintModeConfig PrintMode { get; set; } = new();
    }

    /// <summary>
    /// 韌體配置
    /// </summary>
    public class FirmwareConfig
    {
        [JsonPropertyName("MachineType")]
        public string MachineType { get; set; } = string.Empty;

        [JsonPropertyName("JetColors")]
        public int[] JetColors { get; set; } = Array.Empty<int>();

        [JsonPropertyName("BaseVoltages")]
        public double[] BaseVoltages { get; set; } = Array.Empty<double>();

        [JsonPropertyName("OffsetVoltages")]
        public double[] OffsetVoltages { get; set; } = Array.Empty<double>();

        [JsonPropertyName("HeatTemperature")]
        public double HeatTemperature { get; set; }

        [JsonPropertyName("DisableColumnMask")]
        public int DisableColumnMask { get; set; }

        [JsonPropertyName("PrintheadColorCount")]
        public int PrintheadColorCount { get; set; }

        [JsonPropertyName("InstallDirectionPositive")]
        public bool InstallDirectionPositive { get; set; }

        [JsonPropertyName("EncoderFunction")]
        public int EncoderFunction { get; set; }
    }

    /// <summary>
    /// 列印模式配置
    /// </summary>
    public class PrintModeConfig
    {
        [JsonPropertyName("PrintDirection")]
        public string PrintDirection { get; set; } = "LeftToRight";

        [JsonPropertyName("GratingDpi")]
        public int GratingDpi { get; set; }

        [JsonPropertyName("ImageDpi")]
        public int ImageDpi { get; set; }

        [JsonPropertyName("GrayScale")]
        public int GrayScale { get; set; }

        [JsonPropertyName("GrayScaleDrop")]
        public int GrayScaleDrop { get; set; }

        [JsonPropertyName("ResetEncoder")]
        public int ResetEncoder { get; set; }

        [JsonPropertyName("LColumnCali")]
        public double[] LColumnCali { get; set; } = Array.Empty<double>();

        [JsonPropertyName("RColumnCali")]
        public double[] RColumnCali { get; set; } = Array.Empty<double>();

        [JsonPropertyName("CaliPixelMM")]
        public double CaliPixelMM { get; set; }
    }
}
