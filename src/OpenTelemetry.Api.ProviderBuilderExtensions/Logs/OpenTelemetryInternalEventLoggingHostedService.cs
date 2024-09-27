// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenTelemetry.Logs;

internal class OpenTelemetryInternalEventLoggingHostedService : IHostedService, IDisposable
{
    public ILogger OpenTelemetryEventLogger;

    public OpenTelemetryInternalEventLoggingHostedService(
        ILoggerFactory loggerFactory)
    {
#if NET9_0
        ArgumentNullException.ThrowIfNull(loggerFactory);
#endif

        this.OpenTelemetryEventLogger = loggerFactory.CreateLogger("OpenTelemetry.Internal");
    }

    public ILogger GetOpenTelemetryEventLogger() => this.OpenTelemetryEventLogger;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
