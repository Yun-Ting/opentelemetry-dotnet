// <copyright file="OtlpLogExporterTests.cs" company="OpenTelemetry Authors">
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

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.Exporter.OpenTelemetryProtocol.Tests
{
    public class OtlpLogExporterTests : Http2UnencryptedSupportTests
    {
        private readonly ILogger logger;
        private readonly List<LogRecord> exportedItems = new List<LogRecord>();
        private readonly ILoggerFactory loggerFactory;
        private readonly BaseExporter<LogRecord> exporter;
        private OpenTelemetryLoggerOptions options;

        public OtlpLogExporterTests()
        {
            this.exporter = new InMemoryExporter<LogRecord>(this.exportedItems);
            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    this.options = options;
                    this.options.AddInMemoryExporter(this.exportedItems);
                });
            });

            this.logger = this.loggerFactory.CreateLogger<OtlpLogExporterTests>();
        }

        // TODO:
        // Validate attributes, severity, traceid,spanid etc.

        [Fact]
        public void ToOtlpLogRecordTest()
        {
            this.options.IncludeFormattedMessage = true;
            this.options.ParseStateValues = true;

            this.logger.LogInformation("Hello from {name} {price}.", "tomato", 2.99);
            Assert.Single(this.exportedItems);
            var logRecord = this.exportedItems[0];
            var otlpLogRecord = logRecord.ToOtlpLog();
            Assert.NotNull(otlpLogRecord);
            Assert.Equal("Hello from tomato 2.99.", otlpLogRecord.Body.StringValue);
            this.exportedItems.Clear();

            this.logger.LogInformation(new EventId(10, null), "Hello from {name} {price}.", "tomato", 2.99);
            Assert.Single(this.exportedItems);
            logRecord = this.exportedItems[0];
            otlpLogRecord = logRecord.ToOtlpLog();
            Assert.NotNull(otlpLogRecord);
            Assert.Equal("Hello from tomato 2.99.", otlpLogRecord.Body.StringValue);
            this.exportedItems.Clear();

            this.logger.LogInformation(new EventId(10, "MyEvent10"), "Hello from {name} {price}.", "tomato", 2.99);
            Assert.Single(this.exportedItems);
            logRecord = this.exportedItems[0];
            otlpLogRecord = logRecord.ToOtlpLog();
            Assert.NotNull(otlpLogRecord);
            Assert.Equal("Hello from tomato 2.99.", otlpLogRecord.Body.StringValue);
        }
    }
}
