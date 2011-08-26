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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using OstrichNet.Util;

namespace OstrichNet.Service
{
    public class TimeSeriesCollector : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly List<double> percentiles = new List<double>(new[] { 0.25d, 0.5d, 0.75d, 0.9d, 0.95d, .99d, 0.999d, 0.9999d });
        private static readonly List<long> emptyTimings = new List<long>(new[] { 0L, 0L, 0L, 0L, 0L, 0L, 0L });
        private static readonly object locker = new object();

        private readonly ConcurrentDictionary<string, TimeSeries<double>> hourly = new ConcurrentDictionary<string, TimeSeries<double>>();
        private readonly ConcurrentDictionary<string, TimeSeries<List<long>>> hourlyTimings = new ConcurrentDictionary<string, TimeSeries<List<long>>>();
        private readonly Timer collectionTimer;
        private readonly StatsListener listener;

        private long last = DateTimeUtils.Epoch.MillisFromEpoch();

        public TimeSeriesCollector() : this(true)
        {
        }

        public TimeSeriesCollector(StatsListener listener, bool collect = true)
        {
            this.listener = listener;
            if (!collect) return;
            collectionTimer = new Timer(Periodic, null, Timeout.Infinite, 60000);
            collectionTimer.Change(10000, 60000);
        }

        public TimeSeriesCollector(bool collect) : this (StatsListener.Default, collect)
        {
        }

        public IEnumerable<string> GetKeys()
        {
            return hourly.Keys.Concat(hourlyTimings.Keys);
        }

        public void Periodic(object state)
        {
            try
            {
                lock (locker)
                {
                    foreach (var kv in listener.GetCounters())
                    {
                        var name = kv.Key.ToLower();
                        var counter = kv.Value;
                        hourly.AddOrUpdate("counter:" + name, k => TimeSeries<double>.Hourly(() => 0d, counter.Value), (k, ts) => ts.Add(Convert.ToDouble(counter.Value)));
                    }

                    foreach (var kv in listener.GetGauges())
                    {
                        var name = kv.Key.ToLower();
                        var gauge = kv.Value;
                        hourly.AddOrUpdate("gauge:" + name, k => TimeSeries<double>.Hourly(() => 0d, gauge.Value), (k, ts) => ts.Add(Convert.ToDouble(gauge.Value)));
                    }

                    foreach (var kv in listener.GetMetrics())
                    {
                        var data = percentiles.Select(p => Convert.ToInt64(kv.Value.Histogram.GetPercentile(p))).ToList();
                        hourlyTimings.AddOrUpdate("timing:" + kv.Key.ToLower(), k => TimeSeries<List<long>>.Hourly(() => new List<long>(emptyTimings), data), (k, ts) => ts.Add(data));
                    }

                    last = SystemClock.UtcNow().MillisFromEpoch();
                }
            } 
            catch (Exception e)
            {
                logger.Error("This is just a warning - Exception occured while rolling stats", e);
            }
        }

        public IEnumerable<IEnumerable<long>> Get(string key, IEnumerable<int> selection)
        {
            var times = Enumerable.Range(0, 60).Select(i => (TimeSpan.FromMilliseconds(last).TotalSeconds + TimeSpan.FromMinutes(i - 59).TotalSeconds)).ToArray();
            
            if (hourly.ContainsKey(key))
            {
                var data = times.Zip(hourly[key], (a, b) => new List<long>(new[] { Convert.ToInt64(a), Convert.ToInt64(b) }));
                return data;
            }
            
            TimeSeries<List<long>> timings;
            if (hourlyTimings.TryGetValue(key, out timings))
            {
                var data = times.Zip(timings, (a, b) =>
                {    
                    var rv = new List<long>(new[] { Convert.ToInt64(a) });
                    var filtered = b.Where((row, i) => selection == null || selection.Contains(i));
                    return rv.Concat(filtered);
                });
                return data.ToArray();
            }
            return null;
        }

        private class TimeSeries<T> : IEnumerable<T>
        {
            private readonly int size;
            private T[] data;
            private int index;

            private TimeSeries(int size, Func<T> empty)
            {
                this.size = size;
                data = new T[size];
                data.EachWithIndex((n, i) => data[i] = empty());
            }

            public TimeSeries<T> Add(T n)
            {
                data[index] = n;
                index = (index + 1) % size;
                return this;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = index; i < size; i++)
                    yield return data[i];
                for (var i = 0; i < index; i++)
                    yield return data[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static TimeSeries<T> Hourly(Func<T> empty, T initialValue)
            {
                var ts = new TimeSeries<T>(60, empty);
                ts.Add(initialValue);
                return ts;
            }
        }

        public void Dispose()
        {
            if (collectionTimer != null)
            {
                collectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                collectionTimer.Dispose();
            }
        }
    }
}
