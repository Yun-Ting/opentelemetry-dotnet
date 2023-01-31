// <copyright file="Program.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class Program
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    private const int MaxTimeToAllowForFlush = 10000;

    public static void Main()
    {
        var exportedItemsA = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddProcessInstrumentation()
            .AddInMemoryExporter(exportedItemsA)
            .Build();

        Console.WriteLine(".NET Process metrics are available at http://localhost:9464/metrics, press any key to exit...");
        Console.ReadKey(false);
    }

    private static double GetValue(Metric metric)
    {
        double sum = 0;

        foreach (ref readonly var metricPoint in metric.GetMetricPoints())
        {
            if (metric.MetricType.IsLong())
            {
                sum += metricPoint.GetSumLong();
            }
        }

        return sum;
    }
}
