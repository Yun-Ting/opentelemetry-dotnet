// <copyright file="OtlpExporterOptions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Internal;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Exporter
{
    /// <summary>
    /// OpenTelemetry Protocol (OTLP) exporter options.
    /// OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_HEADERS, OTEL_EXPORTER_OTLP_TIMEOUT, OTEL_EXPORTER_OTLP_PROTOCOL
    /// environment variables are parsed during object construction.
    /// </summary>
    /// <remarks>
    /// The constructor throws <see cref="FormatException"/> if it fails to parse
    /// any of the supported environment variables.
    /// </remarks>
    public class OtlpExporterOptions
    {
        internal const string EndpointEnvVarName = "OTEL_EXPORTER_OTLP_ENDPOINT";
        internal const string HeadersEnvVarName = "OTEL_EXPORTER_OTLP_HEADERS";
        internal const string TimeoutEnvVarName = "OTEL_EXPORTER_OTLP_TIMEOUT";
        internal const string ProtocolEnvVarName = "OTEL_EXPORTER_OTLP_PROTOCOL";

        internal readonly Func<HttpClient> DefaultHttpClientFactory;

        private const string DefaultGrpcEndpoint = "http://localhost:4317";
        private const OtlpExportProtocol DefaultOtlpExportProtocol = OtlpExportProtocol.Grpc;

        private Uri endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtlpExporterOptions"/> class.
        /// </summary>
        public OtlpExporterOptions()
            : this(new ConfigurationBuilder().AddEnvironmentVariables().Build())
        {
        }

        internal OtlpExporterOptions(IConfiguration configuration)
        {
            if (configuration.TryGetUriValue(EndpointEnvVarName, out var endpoint))
            {
                this.endpoint = endpoint;
            }

            if (configuration.TryGetStringValue(HeadersEnvVarName, out var headers))
            {
                this.Headers = headers;
            }

            if (configuration.TryGetIntValue(TimeoutEnvVarName, out var timeout))
            {
                this.TimeoutMilliseconds = timeout;
            }

            if (configuration.TryGetValue<OtlpExportProtocol>(
                ProtocolEnvVarName,
                OtlpExportProtocolParser.TryParse,
                out var protocol))
            {
                this.Protocol = protocol;
            }

            this.BatchExportProcessorOptions = new BatchExportActivityProcessorOptions(configuration);
        }

        /// <summary>
        /// Gets or sets the target to which the exporter is going to send telemetry.
        /// Must be a valid Uri with scheme (http or https) and host, and
        /// may contain a port and path. The default value is
        /// * http://localhost:4317 for <see cref="OtlpExportProtocol.Grpc"/>
        /// </summary>
        public Uri Endpoint
        {
            get
            {
                if (this.endpoint == null)
                {
                    this.endpoint = new Uri(DefaultGrpcEndpoint);
                }

                return this.endpoint;
            }

            set
            {
                this.endpoint = value;
                this.ProgrammaticallyModifiedEndpoint = true;
            }
        }

        /// <summary>
        /// Gets or sets optional headers for the connection. Refer to the <a href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md#specifying-headers-via-environment-variables">
        /// specification</a> for information on the expected format for Headers.
        /// </summary>
        public string Headers { get; set; }

        /// <summary>
        /// Gets or sets the max waiting time (in milliseconds) for the backend to process each batch. The default value is 10000.
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
        /// </summary>
        public OtlpExportProtocol Protocol { get; set; } = DefaultOtlpExportProtocol;

        /// <summary>
        /// Gets or sets the export processor type to be used with the OpenTelemetry Protocol Exporter. The default value is <see cref="ExportProcessorType.Batch"/>.
        /// </summary>
        public ExportProcessorType ExportProcessorType { get; set; } = ExportProcessorType.Batch;

        /// <summary>
        /// Gets or sets the BatchExportProcessor options. Ignored unless ExportProcessorType is Batch.
        /// </summary>
        public BatchExportProcessorOptions<Activity> BatchExportProcessorOptions { get; set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Endpoint" /> was modified via its setter.
        /// </summary>
        internal bool ProgrammaticallyModifiedEndpoint { get; private set; }
    }
}
