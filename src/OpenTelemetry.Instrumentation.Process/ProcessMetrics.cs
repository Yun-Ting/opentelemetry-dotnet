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

using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using Diagnostics = System.Diagnostics;

namespace OpenTelemetry.Instrumentation.Process;

internal class ProcessMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(ProcessMetrics).Assembly.GetName();
    internal readonly Meter MeterInstance = new(AssemblyName.Name, AssemblyName.Version.ToString());

    private readonly InstrumentsValues instrumentsValues;

    public ProcessMetrics(ProcessInstrumentationOptions options)
    {
        this.instrumentsValues = new InstrumentsValues();

        // TODO: change to ObservableUpDownCounter
        this.MeterInstance.CreateObservableGauge(
            "process.memory.usage",
            () => this.instrumentsValues.GetMemoryUsage(),
            unit: "By",
            description: "The amount of physical memory in use.");
    }

    private class InstrumentsValues
    {
        private readonly Diagnostics.Process currentProcess = Diagnostics.Process.GetCurrentProcess();
        private double? memoryUsage;

        internal double GetMemoryUsage()
        {
            if (!this.memoryUsage.HasValue)
            {
                Console.WriteLine($"threadId: {Environment.CurrentManagedThreadId.ToString()} invoke GetMemoryUsage() without the latest snapshot.");
                this.Snapshot();
            }

            Console.WriteLine($"threadId: {Environment.CurrentManagedThreadId.ToString()} retrieve memory usage from snapshot.");
            var value = this.memoryUsage.Value;
            this.memoryUsage = null;
            return value;
        }

        private void Snapshot()
        {
            Console.WriteLine($"threadId: {Environment.CurrentManagedThreadId.ToString()} Refresh()");
            this.currentProcess.Refresh();
            this.memoryUsage = this.currentProcess.WorkingSet64;
        }
    }
}
