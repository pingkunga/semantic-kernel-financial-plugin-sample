using OpenTelemetry.Exporter;

namespace ds.opentelemetry;

public class ObservabilityOptions
{
    public string ServiceName { get; set; } = default!;
    public string CollectorUrl { get; set; } = @"http://localhost:4317";
    public string CollectorProtocol { get; set; } = "grpc";

    public bool IsDevelopment { get; set; } = true;

    public string SamplingStrategy { get; set; } = "always";
    public double SamplingRate { get; set; } = 1.0;

    public Uri CollectorUri => new(this.CollectorUrl);
    public OtlpExportProtocol CollectorProtocolEnum
    {
        get
        {
            return this.CollectorProtocol.ToLowerInvariant() switch
            {
                "grpc" => OtlpExportProtocol.Grpc,
                "http" => OtlpExportProtocol.HttpProtobuf,
                _
                    => throw new ArgumentException(
                        $"Unsupported collector protocol: {CollectorProtocol}"
                    )
            };
        }
    }
}
