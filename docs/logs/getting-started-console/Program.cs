// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

var foodName = "tomato";
var foodPrice = 2.99;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry(logging =>
    {
        logging.AddConsoleExporter();
    });
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation($"Food {foodName} price changed to {foodPrice}.");
