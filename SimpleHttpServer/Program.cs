using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using System.Diagnostics.Metrics;

class Program
{
    private static readonly ActivitySource ActivitySource = new("SimpleHttpServer");

    static void Main(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var openTelemetrySettings = configuration.GetSection("OpenTelemetry");

        // Extract configurations
        var resourceAttributes = openTelemetrySettings["ResourceAttributes"];
        var otlpExporterSettings = openTelemetrySettings.GetSection("OtlpExporter");
        var otlpEndpoint = otlpExporterSettings["Endpoint"];
        var otlpHeaders = otlpExporterSettings["Headers"];
        var otlpProtocol = otlpExporterSettings["Protocol"];
        var metricsExporter = openTelemetrySettings["MetricsExporter"];
        var logsExporter = openTelemetrySettings["LogsExporter"];

        Console.WriteLine("Configuring OpenTelemetry for Elastic APM...");

        // Fix: Convert Protocol String to Enum
        OtlpExportProtocol exportProtocol = otlpProtocol switch
        {
            "grpc" => OtlpExportProtocol.Grpc,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new ArgumentException($"Invalid OTLP protocol: {otlpProtocol}")
        };

        // Configure OpenTelemetry for Tracing
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddAttributes(ParseResourceAttributes(resourceAttributes)))
            .AddSource("SimpleHttpServer")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                if (!string.IsNullOrEmpty(otlpHeaders))
                {
                    options.Headers = otlpHeaders;
                }
                options.Protocol = exportProtocol;
            })
            .Build();

        Console.WriteLine("Tracing configured...");

        // ✅ Configure Metrics (only if "MetricsExporter" is set to "otlp")
        if (metricsExporter == "otlp")
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddAttributes(ParseResourceAttributes(resourceAttributes)))
                .AddMeter("SimpleHttpServer")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    if (!string.IsNullOrEmpty(otlpHeaders))
                    {
                        options.Headers = otlpHeaders;
                    }
                    options.Protocol = exportProtocol;
                })
                .Build();
        }

        // ✅ Configure Logs (only if "LogsExporter" is set to "otlp")
        if (logsExporter == "otlp")
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddAttributes(ParseResourceAttributes(resourceAttributes)));

                    options.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otlpEndpoint);
                        if (!string.IsNullOrEmpty(otlpHeaders))
                        {
                            otlpOptions.Headers = otlpHeaders;
                        }
                        otlpOptions.Protocol = exportProtocol;
                    });
                });
            });

            var logger = loggerFactory.CreateLogger<Program>();

            Console.WriteLine("Logging configured.");
            SimulateHttpRequest(logger);
        }
        else
        {
            Console.WriteLine("Logging is disabled in appsettings.json.");
        }

        // Simulate HTTP Requests
        while (true)
        {
            SimulateHttpRequest(null); // Pass logger only if logs are enabled
            System.Threading.Thread.Sleep(5000);
        }
    }

    static void SimulateHttpRequest(ILogger? logger)
    {
        using (var activity = ActivitySource.StartActivity("HttpRequest"))
        {
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.url", "http://localhost:8080");

            Console.WriteLine("HTTP request simulated and traced.");

            // Only log if logger is enabled
            logger?.LogInformation("Handled HTTP GET request to /");

            // Simulating a metric
            var meter = new Meter("SimpleHttpServer");
            var counter = meter.CreateCounter<long>("http_requests_total");

            // Fix: Pass KeyValuePairs as an array
            counter.Add(1, new KeyValuePair<string, object?>[] { new("http.method", "GET") });
        }
    }

    static Dictionary<string, object> ParseResourceAttributes(string resourceAttributes)
    {
        var attributes = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(resourceAttributes))
        {
            var pairs = resourceAttributes.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    attributes[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
        }
        return attributes;
    }
}
