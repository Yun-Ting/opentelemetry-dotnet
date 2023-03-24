// <copyright file="AggregatorTest.cs" company="OpenTelemetry Authors">
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
using System.Threading;
using Xunit;

namespace OpenTelemetry.Metrics.Tests
{
    public class AggregatorTest
    {
        private readonly AggregatorStore aggregatorStore = new("test", AggregationType.Histogram, AggregationTemporality.Cumulative, 1024, Metric.DefaultHistogramBounds);

        [Fact]
        public void HistogramDistributeToAllBucketsDefault()
        {
            var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.Histogram, null, null, Metric.DefaultHistogramBounds);
            histogramPoint.Update(-1);
            histogramPoint.Update(0);
            histogramPoint.Update(2);
            histogramPoint.Update(5);
            histogramPoint.Update(8);
            histogramPoint.Update(10);
            histogramPoint.Update(11);
            histogramPoint.Update(25);
            histogramPoint.Update(40);
            histogramPoint.Update(50);
            histogramPoint.Update(70);
            histogramPoint.Update(75);
            histogramPoint.Update(99);
            histogramPoint.Update(100);
            histogramPoint.Update(246);
            histogramPoint.Update(250);
            histogramPoint.Update(499);
            histogramPoint.Update(500);
            histogramPoint.Update(999);
            histogramPoint.Update(1000);
            histogramPoint.Update(1001);
            histogramPoint.Update(10000000);
            histogramPoint.TakeSnapshot(true);

            var count = histogramPoint.GetHistogramCount();

            Assert.Equal(22, count);

            int actualCount = 0;
            foreach (var histogramMeasurement in histogramPoint.GetHistogramBuckets())
            {
                Assert.Equal(2, histogramMeasurement.BucketCount);
                actualCount++;
            }
        }

        [Fact]
        public void HistogramDistributeToAllBucketsCustom()
        {
            var boundaries = new double[] { 10, 20 };
            var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.Histogram, null, null, boundaries);

            // 5 recordings <=10
            histogramPoint.Update(-10);
            histogramPoint.Update(0);
            histogramPoint.Update(1);
            histogramPoint.Update(9);
            histogramPoint.Update(10);

            // 2 recordings >10, <=20
            histogramPoint.Update(11);
            histogramPoint.Update(19);

            histogramPoint.TakeSnapshot(true);

            var count = histogramPoint.GetHistogramCount();
            var sum = histogramPoint.GetHistogramSum();

            // Sum of all recordings
            Assert.Equal(40, sum);

            // Count  = # of recordings
            Assert.Equal(7, count);

            int index = 0;
            int actualCount = 0;
            var expectedBucketCounts = new long[] { 5, 2, 0 };
            foreach (var histogramMeasurement in histogramPoint.GetHistogramBuckets())
            {
                Assert.Equal(expectedBucketCounts[index], histogramMeasurement.BucketCount);
                index++;
                actualCount++;
            }

            Assert.Equal(boundaries.Length + 1, actualCount);
        }

        [Fact]
        public void HistogramWithOnlySumCount()
        {
            var boundaries = Array.Empty<double>();
            var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.HistogramSumCount, null, null, boundaries);

            histogramPoint.Update(-10);
            histogramPoint.Update(0);
            histogramPoint.Update(1);
            histogramPoint.Update(9);
            histogramPoint.Update(10);
            histogramPoint.Update(11);
            histogramPoint.Update(19);

            histogramPoint.TakeSnapshot(true);

            var count = histogramPoint.GetHistogramCount();
            var sum = histogramPoint.GetHistogramSum();

            // Sum of all recordings
            Assert.Equal(40, sum);

            // Count  = # of recordings
            Assert.Equal(7, count);

            // There should be no enumeration of BucketCounts and ExplicitBounds for HistogramSumCount
            var enumerator = histogramPoint.GetHistogramBuckets().GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void MultiThreadedHistogramUpdateAndSnapShotTest()
        {
            var boundaries = Array.Empty<double>();

            var argsToThread = new UpdateThreadArguments
            {
                MreToBlockUpdateThread = new ManualResetEvent(false),
                MreToEnsureAllThreadsStart = new ManualResetEvent(false),
                HistogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.Histogram, null, boundaries, Metric.DefaultExponentialHistogramMaxBuckets),
            };

            var numberOfThreads = 10;
            Thread[] t = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                t[i] = new Thread(HistogramUpdateThread);
                t[i].Start(argsToThread);
            }

            argsToThread.MreToEnsureAllThreadsStart.WaitOne();
            argsToThread.MreToBlockUpdateThread.Set();

            for (int i = 0; i < numberOfThreads; ++i)
            {
                t[i].Join();
            }

            var histogramPoint = argsToThread.HistogramPoint;
            histogramPoint.TakeSnapshot(true);

            var sum = histogramPoint.GetHistogramSum();
            Assert.Equal(400, sum);

            var count = histogramPoint.GetHistogramCount();
            Assert.Equal(70, count);

            // Reset UpdateThreadArguments arguments
            argsToThread.MreToEnsureAllThreadsStart.Reset();
            argsToThread.MreToBlockUpdateThread.Reset();
            argsToThread.ThreadStartedCount = 0;

            Thread[] t2 = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                t2[i] = new Thread(HistogramUpdateThread);
                t2[i].Start(argsToThread);
            }

            argsToThread.MreToEnsureAllThreadsStart.WaitOne();
            argsToThread.MreToBlockUpdateThread.Set();

            for (int i = 0; i < numberOfThreads; ++i)
            {
                t2[i].Join();
            }

            var histogramPoint2 = argsToThread.HistogramPoint;
            histogramPoint2.TakeSnapshot(true);

            var sum2 = histogramPoint2.GetHistogramSum();
            Assert.Equal(400, sum2);

            var count2 = histogramPoint2.GetHistogramCount();
            Assert.Equal(70, count2 - count);
        }

        private static void HistogramUpdateThread(object obj)
        {
            if (obj is not UpdateThreadArguments args)
            {
                throw new Exception("invalid args");
            }

            var mreToEnsureAllThreadsStart = args.MreToEnsureAllThreadsStart;

            if (Interlocked.Increment(ref args.ThreadStartedCount) == 10)
            {
                mreToEnsureAllThreadsStart.Set();
            }

            args.MreToBlockUpdateThread.WaitOne();
            args.HistogramPoint.Update(-10);
            args.HistogramPoint.Update(0);
            args.HistogramPoint.Update(1);
            args.HistogramPoint.Update(9);
            args.HistogramPoint.Update(10);
            args.HistogramPoint.Update(11);
            args.HistogramPoint.Update(19);
        }

        private class UpdateThreadArguments
        {
            public ManualResetEvent MreToBlockUpdateThread;
            public ManualResetEvent MreToEnsureAllThreadsStart;
            public int ThreadStartedCount;
            public MetricPoint HistogramPoint;
            public int NumberOfThreads = 10;
        }
    }
}
