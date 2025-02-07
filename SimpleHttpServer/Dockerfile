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
