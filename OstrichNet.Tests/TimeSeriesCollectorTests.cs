/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */
﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OstrichNet.Service;
using OstrichNet.Util;

namespace OstrichNet.Tests
{
    [TestFixture]
    public class TimeSeriesCollectorTests
    {
        private StatsCollection stats;
        private TimeSeriesCollector collector;
        private IDisposable timeFreeze;

        [SetUp]
        public void Setup()
        {
            stats = Stats.GetDefault();
            stats.ClearAll();
            collector = new TimeSeriesCollector(false);
            timeFreeze = SystemClock.Freeze();
        }

        [Test]
        public void StatsIncr()
        {
            Stats.Incr("cats");
            Stats.Incr("dogs", 3);

            collector.Periodic(null);

            Stats.Incr("dogs", 60000);
            SystemClock.Advance(TimeSpan.FromMinutes(1));

            collector.Periodic(null);

            var series = collector.Get("counter:dogs", null);
            Assert.NotNull(series);
            Assert.That(series.Count(), Is.EqualTo(60));
            var seconds = SystemClock.Minus(TimeSpan.FromMinutes(2d)).MillisFromEpoch() / 1000;
            AssertClose(new List<long>(new[] { seconds, 0 }), series.ElementAt(57));
            seconds = SystemClock.Minus(TimeSpan.FromMinutes(1d)).MillisFromEpoch() / 1000;
            AssertClose(new List<long>(new[] { seconds, 3 }), series.ElementAt(58));
            AssertClose(new List<long>(new[] { SystemClock.Seconds(), 60000 }), series.ElementAt(59));
        }

        [Test]
        public void StatsWithCounterUpdate()
        {
            Stats.Incr("tps", 10);

            collector.Periodic(null);
            SystemClock.Advance(TimeSpan.FromMinutes(1));
            Stats.Incr("tps", 5);
            collector.Periodic(null);

            var series = collector.Get("counter:tps", null);
            Assert.NotNull(series);
            Assert.That(series.Count(), Is.EqualTo(60));
            var seconds = SystemClock.Minus(TimeSpan.FromMinutes(2d)).MillisFromEpoch() / 1000;
            AssertClose(new List<long>(new[] { seconds, 0 }), series.ElementAt(57));
            seconds = SystemClock.Minus(TimeSpan.FromMinutes(1d)).MillisFromEpoch() / 1000;
            AssertClose(new List<long>(new[] { seconds, 10 }), series.ElementAt(58));
            AssertClose(new List<long>(new[] { SystemClock.Seconds(), 5 }), series.ElementAt(59));
        }

        [Test]
        public void SpecificTimingProfiles()
        {
            Stats.AddMetric("run", 5);
            Stats.AddMetric("run", 10);
            Stats.AddMetric("run", 15);
            Stats.AddMetric("run", 20);

            collector.Periodic(null);

            var series = collector.Get("timing:run", null);
            Assert.NotNull(series);
            Assert.That(series.Count(), Is.EqualTo(60));
            AssertClose(new List<long>(new[] { SystemClock.Seconds(), 6, 10, 17, 23, 23, 23, 23, 23 }), series.ElementAt(59).ToArray());

            series = collector.Get("timing:run", new[] { 0, 2 });
            Assert.NotNull(series);
            Assert.That(series.Count(), Is.EqualTo(60));
            AssertClose(new List<long>(new[] { SystemClock.Seconds(), 6, 17 }), series.ElementAt(59).ToArray());

            series = collector.Get("timing:run", new[] { 1, 7 });
            Assert.NotNull(series);
            Assert.That(series.Count(), Is.EqualTo(60));
            AssertClose(new List<long>(new[] { SystemClock.Seconds(), 10, 23 }), series.ElementAt(59).ToArray());
        }

        //punt timestamp division rounding temporarily
        public void AssertClose(IEnumerable<long> a, IEnumerable<long> b)
        {
            Assert.That(a.Count(), Is.EqualTo(b.Count()));
            for (int i = 0; i < a.Count(); i++)
            {
                var itemA = a.ElementAt(i);
                var itemB = b.ElementAt(i);

                if (itemA + 1 == itemB)
                    itemA = itemA + 1;
                else if (itemB + 1 == itemA)
                    itemB = itemB + 1;

                Assert.AreEqual(itemA, itemB, "Elements at index " + i + " did not match.");
            }
        }

        [TearDown]
        public void Teardown()
        {
            timeFreeze.Dispose();
            collector.Dispose();
        }
    }
}
