// <copyright file="PrometheusExporter.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Exporter
{
    /// <summary>
    /// Exporter of OpenTelemetry metrics to Prometheus.
    /// </summary>
    [ExportModes(ExportModes.Pull)]
    public class PrometheusExporter : BaseExporter<Metric>, IPullMetricExporter
    {
        internal const string HttpListenerStartFailureExceptionMessage = "PrometheusExporter http listener could not be started.";
        internal readonly PrometheusExporterOptions Options;
        private Func<int, bool> funcCollect;
        private Func<Batch<Metric>, ExportResult> funcExport;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrometheusExporter"/> class.
        /// </summary>
        /// <param name="options">Options for the exporter.</param>
        internal PrometheusExporter(PrometheusExporterOptions options)
        {
            this.Options = options;
            this.CollectionManager = new PrometheusCollectionManager(this);
        }

        public Func<int, bool> Collect
        {
            get => this.funcCollect;
            set => this.funcCollect = value;
        }

        internal Func<Batch<Metric>, ExportResult> OnExport
        {
            get => this.funcExport;
            set => this.funcExport = value;
        }

        internal PrometheusCollectionManager CollectionManager { get; }

        public override ExportResult Export(in Batch<Metric> metrics)
        {
            return this.OnExport(metrics);
        }
    }
}
