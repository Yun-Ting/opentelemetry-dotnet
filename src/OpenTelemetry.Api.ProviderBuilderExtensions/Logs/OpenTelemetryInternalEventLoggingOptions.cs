// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Logs;

public class OpenTelemetryInternalEventLoggingOptions
{
    internal static IReadOnlyCollection<OpenTelemetryEventSourceOptions> DefaultEventSources { get; } =
    [
        new OpenTelemetryEventSourceOptions
        {
            EventSourceRegexNames = "OpenTelemetry-*$",
        },
    ];

}


