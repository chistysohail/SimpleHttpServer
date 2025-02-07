using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

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

        // Simulate HTTP Requests
        while (true)
        {
            SimulateHttpRequest();
            System.Threading.Thread.Sleep(5000);
        }
    }

    static void SimulateHttpRequest()
    {
        using (var activity = ActivitySource.StartActivity("HttpRequest"))
        {
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.url", "http://localhost:8080");

            Console.WriteLine("HTTP request simulated and traced.");
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
