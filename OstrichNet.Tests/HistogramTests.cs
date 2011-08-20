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
using NUnit.Framework;

namespace OstrichNet.Tests
{
    [TestFixture]
    public class HistogramTests
    {
        Histogram histogram = new Histogram();
        Histogram histogram2 = new Histogram();

        [SetUp]
        public void Setup()
        {
            histogram.Clear();
            histogram2.Clear();
        }

        [Test]
        public void CanFindTheRightBucketForTimings()
        {
            histogram.Add(0);
            histogram.Get(true)[0].Equal(1);
            histogram.Add(9999999);
            var h = histogram.Get(true);
            h[h.Length - 1].Equal(1);
            histogram.Add(1);
            histogram.Get(true)[1].Equal(1);
            histogram.Add(2);
            histogram.Get(true)[2].Equal(1);
            histogram.Add(11);
            histogram.Add(12);
            histogram.Add(13);
            histogram.Get(true)[8].Equal(3);
        }

        [Test]
        public void FindHistogramCutoffsForVariousPercentages()
        {
            for (int i = 0; i < 1000; i++) histogram.Add(i);
                
            Histogram.BinarySearch(histogram.GetPercentile(0.0d)).Equal(Histogram.BinarySearch(0));
            Histogram.BinarySearch(histogram.GetPercentile(0.5d)).Equal(Histogram.BinarySearch(500));
            Histogram.BinarySearch(histogram.GetPercentile(0.9d)).Equal(Histogram.BinarySearch(900));
            Histogram.BinarySearch(histogram.GetPercentile(0.99d)).Equal(Histogram.BinarySearch(999));
            Histogram.BinarySearch(histogram.GetPercentile(1.0d)).Equal(Histogram.BinarySearch(1000));
        }

        [Test]
        public void Merge()
        {
            for (int i = 0; i < 50; i++)
            {
                histogram.Add(i * 10);
                histogram2.Add(i * 10);
            }

            var origTotal = histogram.Total;
            histogram.Merge(histogram2);
            histogram.Total.Equal(origTotal + histogram2.Total);
            var stats = histogram.Get(true);
            var stats2 = histogram2.Get(true);
            for (int i =0 ; i < 50; i++)
            {
                var bucket = Histogram.BinarySearch(i*10);
                stats[bucket].Equal(2*stats2[bucket]);
            }
        }

        [Test]
        public void HandleAVeryLargeTiming()
        {
            histogram.Add(100000000);
            histogram.GetPercentile(1.0d).Equal(Int32.MaxValue);
        }
    }

    //Easy than moving the whole library
    public static class ShouldExtensions
    {
        public static void Equal(this object a, object b)
        {
            Assert.That(a, Is.EqualTo(b));
        }
    }
}
