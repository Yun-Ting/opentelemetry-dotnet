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

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Metrics;

public class Program
{
    public static void Main()
    {

        var exportedItemsA = new List<Metric>();
        var exportedItemsB = new List<Metric>();

        Meter m1 = new("myMeter");
        Meter m2 = new("myMeter");

        m1.CreateObservableCounter(
            "myCounterName",
            () => { return 1D; },
            unit: "1",
            description: "test");

        m2.CreateObservableCounter(
            "myCounterName",
            () => { return 2D; },
            unit: "1",
            description: "test");

        using var meterProviderA = Sdk.CreateMeterProviderBuilder()
            .AddMeter("myMeter")
            .AddInMemoryExporter(exportedItemsA)
            .Build();

        using var meterProviderB = Sdk.CreateMeterProviderBuilder()
            .AddMeter("myMeter")
            .AddInMemoryExporter(exportedItemsB)
            .Build();

        meterProviderA.ForceFlush();
        meterProviderB.ForceFlush();

        var metricA = exportedItemsA.FirstOrDefault(i => i.Name == "myCounterName");
        var metricB = exportedItemsB.FirstOrDefault(i => i.Name == "myCounterName");

        Console.WriteLine("press any key to exit...");
        Console.ReadKey(false);
    }
}
