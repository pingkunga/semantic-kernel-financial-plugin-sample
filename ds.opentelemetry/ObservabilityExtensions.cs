using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace ds.opentelemetry;

public static class ObservabilityExtensions
{
    public static void AddObservability(this WebApplicationBuilder builder, string serviceName = "")
    {
        // Get observability options from configuration
        ObservabilityOptions observabilityOptions = builder.GetObservabilityOptions(serviceName);
        builder.Services.AddSingleton(observabilityOptions);

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing => ConfigureTracing(tracing, observabilityOptions))
            .WithMetrics(metrics => ConfigureMetrics(metrics, observabilityOptions));

        builder.AddSerilog(observabilityOptions);
    }

    private static ObservabilityOptions GetObservabilityOptions(
        this WebApplicationBuilder builder,
        string serviceName
    )
    {
        // Get the configuration from the builder
        var configuration = builder.Configuration;

        ObservabilityOptions observabilityOptions = new();

        configuration.GetRequiredSection(nameof(ObservabilityOptions)).Bind(observabilityOptions);

        // Set from ENV
        //OTEL_EXPORTER_OTLP_ENDPOINT
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            observabilityOptions.CollectorUrl = otlpEndpoint.ToString();
        }

        //OTEL_EXPORTER_OTLP_PROTOCOL
        var otlpProtocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        if (!string.IsNullOrEmpty(otlpProtocol))
        {
            observabilityOptions.CollectorProtocol = otlpProtocol.ToString();
        }

        if (string.IsNullOrEmpty(observabilityOptions.ServiceName))
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                observabilityOptions.ServiceName = builder.Environment.ApplicationName;
            }
            else
            {
                observabilityOptions.ServiceName = serviceName;
            }
        }

        // Set from ENV
        // export ASPNETCORE_ENVIRONMENT=Development
        observabilityOptions.IsDevelopment = builder.Environment.IsDevelopment();

        return observabilityOptions;
    }

    private static IHostApplicationBuilder AddSerilog(
        this IHostApplicationBuilder builder,
        ObservabilityOptions config
    )
    {
        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom
                    .Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                    );

                // Check if OTLP endpoint is configured (Aspire will set this automatically)
                if (!string.IsNullOrWhiteSpace(config.CollectorUrl))
                {
                    loggerConfiguration.WriteTo.OpenTelemetry(options =>
                    {
                        options.Endpoint = config.CollectorUrl;
                        options.Protocol = GetOtlpProtocol(config);
                        options.IncludedData =
                            IncludedData.TraceIdField
                            | IncludedData.SpanIdField
                            | IncludedData.MessageTemplateTextAttribute
                            | IncludedData.SpecRequiredResourceAttributes;
                        options.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = config.ServiceName,
                            ["service.version"] =
                                typeof(Extensions).Assembly.GetName().Version?.ToString()
                                ?? "1.0.0",
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        };
                    });
                }

                if (builder.Environment.IsDevelopment())
                {
                    loggerConfiguration.MinimumLevel.Debug();
                }
                else
                {
                    loggerConfiguration.MinimumLevel.Information();
                }

                loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                loggerConfiguration.MinimumLevel.Override(
                    "Microsoft.Hosting.Lifetime",
                    LogEventLevel.Information
                );
                loggerConfiguration.MinimumLevel.Override(
                    "Microsoft.EntityFrameworkCore",
                    LogEventLevel.Warning
                );
            }
        );

        builder.Services.AddSingleton<ILogger>(Log.Logger);

        builder.Services.Configure<OpenTelemetryLoggerOptions>(
            logging =>
                logging.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = config.CollectorUri;
                    otlp.Protocol = config.CollectorProtocolEnum;
                })
        );

   

        return builder;
    }

    private static OtlpProtocol GetOtlpProtocol(ObservabilityOptions config)
    {
        return config.CollectorProtocol switch
        {
            "protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => OtlpProtocol.Grpc
        };
    }

    public static void ConfigureTracing(TracerProviderBuilder builder, ObservabilityOptions config)
    {
        builder
            .SetResourceBuilder(CreateResourceBuilder(config))
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation(options => options.RecordException = true)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = config.CollectorUri;
                otlp.Protocol = config.CollectorProtocolEnum;
            });

        if (config.IsDevelopment)
        {
            builder.AddConsoleExporter();
        }

        // Configure sampling
        var samplingStrategy = config.SamplingStrategy ?? "default";
        switch (samplingStrategy.ToLower())
        {
            case "always":
                builder.SetSampler(new AlwaysOnSampler());
                break;
            case "never":
                builder.SetSampler(new AlwaysOffSampler());
                break;
            case "custom":
                //builder.SetSampler(new CustomSampler());
                break;
            case "ratio":
                var ratio =
                    config.SamplingRate > 0 && config.SamplingRate <= 1 ? config.SamplingRate : 1.0; // Default to 100% if invalid rate
                builder.SetSampler(new TraceIdRatioBasedSampler(ratio));
                break;
            default:
                // Default to 10% sampling in production, 100% in development
                var defaultRatio = config.IsDevelopment ? 1.0 : 0.1;
                builder.SetSampler(new TraceIdRatioBasedSampler(defaultRatio));
                break;
        }
    }

    public static void ConfigureMetrics(MeterProviderBuilder builder, ObservabilityOptions config)
    {
        builder
            .SetResourceBuilder(CreateResourceBuilder(config))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http")
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = config.CollectorUri;
                otlp.Protocol = config.CollectorProtocolEnum;
            });

        if (config.IsDevelopment)
        {
            builder.AddConsoleExporter();
        }
    }

    public static ResourceBuilder CreateResourceBuilder(ObservabilityOptions config)
    {
        return ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: config.ServiceName,
                serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName
            );
    }
}
