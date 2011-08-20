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

namespace OstrichNet.Tests
{
    [TestFixture]
    public class StatsCollectionTests
    {
        private StatsCollection stats;

        [SetUp]
        public void Setup()
        {
            stats = new StatsCollection();
        }

        [Test]
        public void BasicCounters()
        {
            stats.Increment("widgets", 1);
            stats.Increment("wodgets", 12);
            stats.Increment("wodgets");

            CollectionAssert.AreEqual(stats.Counters.ToArray(), new List<KeyValuePair<string, AtomicLong>>
                                                 {
                                                    new KeyValuePair<string, AtomicLong>("widgets" , new AtomicLong(1)),
                                                    new KeyValuePair<string, AtomicLong>("wodgets" , new AtomicLong(13))
                                                 });
        }

        [Test]
        public void NegativeCounters()
        {
            stats.Increment("widgets", 3);
            stats.Increment("widgets", -1);

            CollectionAssert.AreEqual(stats.Counters.ToArray(), new List<KeyValuePair<string, AtomicLong>>
                                                 {
                                                    new KeyValuePair<string, AtomicLong>("widgets" , new AtomicLong(2)),
                                                 });
        }

        [Test]
        public void EmptyMetrics()
        {
            stats.RecordMetric("test", 0);
            var metric = stats.GetMetric("test");
            Assert.AreEqual(metric.Min, 0);
            Assert.AreEqual(metric.Max, 0);
            Assert.AreEqual(metric.Mean, 0);
            Assert.AreEqual(metric.Count, 1);
        }

        [Test]
        public void MetricBasicMeanMinMax()
        {
            stats.RecordMetric("test", 1);
            stats.RecordMetric("test", 2);
            stats.RecordMetric("test", 3);

            var metric = stats.GetMetric("test");
            Assert.AreEqual(metric.Min, 1);
            Assert.AreEqual(metric.Max, 3);
            Assert.AreEqual(metric.Mean, 2);
            Assert.AreEqual(metric.Count, 3);
        }

        [Test]
        public void BasicGauge()
        {
            stats.AddGauge("pi", new Gauge(() => Math.PI));
            Assert.That(stats.Gauges.Values.First().Value, Is.EqualTo(Math.PI));
        }

        [Test]
        public void ClearGauge()
        {
            stats.AddGauge("pi", new Gauge(() => Math.PI));
            stats.DeleteGauge("pi");
            Assert.That(stats.Gauges.Count, Is.EqualTo(0));
        }

        [Test]
        public void GaugesUpdate()
        {
            float seed = 0;
            stats.AddGauge("autoIncrement", new Gauge(() => seed++));
            Assert.That(stats.Gauges.First().Value.Value, Is.EqualTo(0));
            Assert.That(stats.Gauges.First().Value.Value, Is.EqualTo(1));
            Assert.That(stats.Gauges.First().Value.Value, Is.EqualTo(2));
            Assert.That(stats.Gauges.First().Value.Value, Is.EqualTo(3));
        }

        [TearDown]
        public void Teardown()
        {
            stats.ClearAll();
        }
    }
}
