using System;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Runtime.InteropServices;

namespace GitHub.Runner.Worker.Telemetry
{
    public static class OpenTelemetryConfig
    {
        private static TracerProvider? _tracerProvider;
        private static MeterProvider? _meterProvider;

        public static void Initialize(string serviceName, string serviceVersion)
        {
            var resource = ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["os.type"] = RuntimeInformation.OSDescription,
                    ["os.architecture"] = RuntimeInformation.OSArchitecture.ToString(),
                    ["process.runtime.name"] = RuntimeInformation.FrameworkDescription,
                    ["process.runtime.version"] = Environment.Version.ToString(),
                    ["process.runtime.description"] = RuntimeInformation.FrameworkDescription
                });

            var builder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resource)
                .AddSource("GitHub.Runner.Worker");

            // Configure OTLP exporter if endpoint is specified
            var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                builder.AddOtlpExporter(opts => 
                {
                    opts.Endpoint = new Uri(otlpEndpoint);
                });
            }

            _tracerProvider = builder.Build();

            // Configure metrics
            var meterBuilder = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resource)
                .AddMeter("GitHub.Runner.Worker");

            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                meterBuilder.AddOtlpExporter(opts => 
                {
                    opts.Endpoint = new Uri(otlpEndpoint);
                });
            }

            _meterProvider = meterBuilder.Build();
        }

        public static void Shutdown()
        {
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
        }

        public static ActivitySource CreateActivitySource(string name)
        {
            return new ActivitySource(name);
        }

        public static Meter CreateMeter(string name)
        {
            return new Meter(name);
        }
    }
} 