// <copyright file="BatchTestMoveNextBenchmarks.cs" company="OpenTelemetry Authors">
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

using BenchmarkDotNet.Attributes;
using OpenTelemetry.Internal;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Tests.Trace
{
    public class BatchTestMoveNextBenchmarks
    {
        [MemoryDiagnoser]
        public class MoveNextTests
        {
            [Benchmark]
            public void SingleElement()
            {
                var value = "a";
                var batch = new Batch<string>(value);
                foreach (var item in batch)
                {
                }
            }

            [Benchmark]
            public void MoveNextCircularBuffer()
            {
                var circularBuffer = new CircularBuffer<string>(5);
                var value1 = "a";
                var value2 = "b";
                var value3 = "c";
                var value4 = "d";
                var value5 = "e";
                circularBuffer.Add(value1);
                circularBuffer.Add(value2);
                circularBuffer.Add(value3);
                circularBuffer.Add(value4);
                circularBuffer.Add(value5);
                var batch = new Batch<string>(circularBuffer, 5);
                foreach (var item in batch)
                {
                }
            }

            [Benchmark]
            public void MoveNextMetrix()
            {
                var metrics = new Metric[5];
                var batch3 = new Batch<Metric>(metrics, 5);
                foreach (var item in batch3)
                {
                }
            }
        }
    }
}
