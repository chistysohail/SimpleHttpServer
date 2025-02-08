# Use official .NET 6 runtime as the base image
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

# Use official .NET 6 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY SimpleHttpServer.csproj ./
RUN dotnet restore

# Copy the rest of the source files and build the application
COPY . ./
RUN dotnet publish -c Release -o /publish

# Create final runtime container
FROM base AS final
WORKDIR /app

# Copy built files from the previous build stage
COPY --from=build /publish .

# Set environment variables for OpenTelemetry
ENV OTEL_RESOURCE_ATTRIBUTES="service.name=SimpleHttpServer,deployment.environment=production,realm=apm-test-7"
ENV OTEL_EXPORTER_OTLP_ENDPOINT="http://elastic-apm-server:8200"
ENV OTEL_EXPORTER_OTLP_HEADERS="Authorization=Bearer YOUR_ELASTIC_APM_API_KEY"
ENV OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf"
ENV OTEL_METRICS_EXPORTER="otlp"
ENV OTEL_LOGS_EXPORTER="otlp"

# Start the application
CMD ["dotnet", "SimpleHttpServer.dll"]





/////////////////////////////////sample-trace.json to pb//////////////////////
////otel-collector --config=config.yaml --exporter=otlp-file --file=sample-trace.pb < sample-trace.json
////protoc --encode=opentelemetry.proto.trace.v1.ResourceSpans sample-trace.json > sample-trace.pb

////Test APM 
// curl -X POST "http://elasticsearch.company.com:8200/v1/traces" \
// -H "Content-Type: application/x-protobuf" \
// -H "Authorization: Bearer your_apm_api_key" \
// --data-binary @sample-trace.pb

////Test APM 
// curl -X POST "http://elasticsearch.company.com:8200/v1/traces" \
// -H "Content-Type: application/json" \
// -H "Authorization: Bearer your_apm_api_key" \
// --data @sample-trace.json


////test OpenTelemetry
// curl -X POST "http://elasticsearch.company.com:8200/v1/traces" \
// -H "Content-Type: application/x-protobuf" \
// -H "Authorization: Bearer your_apm_api_key" \
// --data-binary @sample-trace.pb

/////////////////////////////////sample-trace.json 
// {
//   "resourceSpans": [
//     {
//       "resource": {
//         "attributes": [
//           {
//             "key": "service.name",
//             "value": {
//               "stringValue": "Dotnet6App"
//             }
//           }
//         ]
//       },
//       "instrumentationLibrarySpans": [
//         {
//           "spans": [
//             {
//               "traceId": "abc1234567890abcdef1234567890abc",
//               "spanId": "1234567890abcdef",
//               "name": "Test Span",
//               "kind": 1,
//               "startTimeUnixNano": "1690000000000000000",
//               "endTimeUnixNano": "1690000001000000000",
//               "status": {
//                 "code": 0
//               }
//             }
//           ]
//         }
//       ]
//     }
//   ]
// }
