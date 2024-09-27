// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

namespace OpenTelemetry.Logs;

public class OpenTelemetryEventSourceOptions
{
    public string? EventSourceRegexNames { get; set; }

    public EventLevel? EventLevel { get; set; } = System.Diagnostics.Tracing.EventLevel.Informational;
}
