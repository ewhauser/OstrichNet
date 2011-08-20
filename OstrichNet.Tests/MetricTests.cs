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
﻿using NUnit.Framework;

namespace OstrichNet.Tests
{
    [TestFixture]
    public class MetricTests
    {
        [Test]
        public void MinMeanMax()
        {
            var metric = new Metric();
            metric.Add(10);
            metric.Add(20);
            Assert.That(metric.Count, Is.EqualTo(2));
            Assert.That(metric.Min, Is.EqualTo(10));
            Assert.That(metric.Max, Is.EqualTo(20));
            Assert.That(metric.Mean, Is.EqualTo(15.0d));

            metric.Add(60);

            Assert.That(metric.Count, Is.EqualTo(3));
            Assert.That(metric.Min, Is.EqualTo(10));
            Assert.That(metric.Max, Is.EqualTo(60));
            Assert.That(metric.Mean, Is.EqualTo(30.0d));

            Histogram other = new Histogram();
            other.Add(10);
            other.Add(20);
            other.Add(60);
            CollectionAssert.AreEqual(metric.Histogram.Get(), other.Get());
        }
    }
}
