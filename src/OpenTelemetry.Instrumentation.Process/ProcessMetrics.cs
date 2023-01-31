// <copyright file="ProcessMetrics.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics.Metrics;
using System.Reflection;
using Diagnostics = System.Diagnostics;

namespace OpenTelemetry.Instrumentation.Process;

internal sealed class ProcessMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(ProcessMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version.ToString());

    // vars for calculating CPU utilization
    private static DateTime lastCollectionTimeUtc;
    private static double lastCollectedUserProcessorTime;
    private static double lastCollectedPrivilegedProcessorTime;

    static ProcessMetrics()
    {
        lastCollectionTimeUtc = DateTime.UtcNow;
        lastCollectedUserProcessorTime = Diagnostics.Process.GetCurrentProcess().UserProcessorTime.TotalSeconds;
        lastCollectedPrivilegedProcessorTime = Diagnostics.Process.GetCurrentProcess().PrivilegedProcessorTime.TotalSeconds;

        MeterInstance.CreateObservableUpDownCounter(
            "process.memory.usage",
            () =>
            {
                return Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            },
            unit: "By",
            description: "The amount of physical memory allocated for this process.");

        MeterInstance.CreateObservableUpDownCounter(
            "process.memory.virtual",
            () =>
            {
                return Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            },
            unit: "By",
            description: "The amount of committed virtual memory for this process.");

        MeterInstance.CreateObservableCounter(
            "process.cpu.time",
            () =>
            {
                var process = Diagnostics.Process.GetCurrentProcess();
                return new[]
                {
                    new Measurement<double>(process.UserProcessorTime.TotalSeconds, new KeyValuePair<string, object?>("state", "user")),
                    new Measurement<double>(process.PrivilegedProcessorTime.TotalSeconds, new KeyValuePair<string, object?>("state", "system")),
                };
            },
            unit: "s",
            description: "Total CPU seconds broken down by different states.");

        MeterInstance.CreateObservableGauge(
            "process.cpu.utilization",
            () =>
            {
                return GetCpuUtilization();
            },
            unit: "1",
            description: "Difference in process.cpu.time since the last measurement, divided by the elapsed time and number of CPUs available to the process.");

        MeterInstance.CreateObservableUpDownCounter(
            "process.threads",
            () =>
            {
                return Diagnostics.Process.GetCurrentProcess().Threads.Count;
            },
            unit: "{threads}",
            description: "Process threads count.");
    }

    public ProcessMetrics(ProcessInstrumentationOptions options)
    {
    }

    public void Dispose()
    {
        MeterInstance.Dispose();
    }

    private static IEnumerable<Measurement<double>> GetCpuUtilization()
    {
        var process = Diagnostics.Process.GetCurrentProcess();
        var elapsedTimeForAllCpus = (DateTime.UtcNow - lastCollectionTimeUtc).TotalSeconds * Environment.ProcessorCount;
        var userProcessorUtilization = (process.UserProcessorTime.TotalSeconds - lastCollectedUserProcessorTime) / elapsedTimeForAllCpus;
        var privilegedProcessorUtilization = (process.PrivilegedProcessorTime.TotalSeconds - lastCollectedPrivilegedProcessorTime) / elapsedTimeForAllCpus;

        lastCollectionTimeUtc = DateTime.UtcNow;
        lastCollectedUserProcessorTime = process.UserProcessorTime.TotalSeconds;
        lastCollectedPrivilegedProcessorTime = process.PrivilegedProcessorTime.TotalSeconds;

        return new[]
        {
            new Measurement<double>(Math.Min(userProcessorUtilization, 1D), new KeyValuePair<string, object?>("state", "user")),
            new Measurement<double>(Math.Min(privilegedProcessorUtilization, 1D), new KeyValuePair<string, object?>("state", "system")),
        };
    }
}
