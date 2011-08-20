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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using OstrichNet.Util;

namespace OstrichNet
{
    public class StatsListener
    {
        private static readonly Lazy<StatsListener> defaultListener = new Lazy<StatsListener>(() => new StatsListener(Stats.GetDefault()), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly StatsCollection collection;
        private readonly ConcurrentDictionary<string, Metric> metrics = new ConcurrentDictionary<string, Metric>();
        private readonly ConcurrentDictionary<string, AtomicLong> lastCounters = new ConcurrentDictionary<string, AtomicLong>();

        public static StatsListener Default
        {
            get { return defaultListener.Value; }
        }

        public StatsListener(StatsCollection collection)
        {
            Guard.NotNull(collection);
            this.collection = collection;
            collection.AddListener(this);
            collection.Counters.Each(kv => lastCounters[kv.Key] = kv.Value);
        }

        public Metric GetMetric(string tag)
        {
            return metrics.GetOrAdd(tag, k => new Metric());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IDictionary<string, AtomicLong> GetCounters()
        {
            var deltas = new Dictionary<string, AtomicLong>();
            foreach (var kv in collection.Counters)
            {  
                deltas[kv.Key] = Stats.Delta(lastCounters.GetOrAdd(kv.Key, new AtomicLong(0)), kv.Value);
                lastCounters[kv.Key] = kv.Value;
            }
            return deltas;
        }

        public IDictionary<string, Gauge> GetGauges()
        {
            return collection.Gauges;
        }

        public IDictionary<string, Metric> GetMetrics()
        {
            var timingStatistics = new Dictionary<string, Metric>();
            metrics.Each(kv =>
            {
                var metric = kv.Value;
                timingStatistics[kv.Key] = metric.Snapshot();
                metric.Clear();
            });
            return timingStatistics;
        }

        public StatsSummary GetSummary()
        {
            return new StatsSummary(GetCounters(), GetMetrics(), collection.Gauges);
        }
    }
}
