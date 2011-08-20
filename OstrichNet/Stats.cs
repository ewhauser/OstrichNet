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
using System.Diagnostics;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using OstrichNet.Util;

namespace OstrichNet
{
    public class Stats : StatsCollection
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly ConcurrentDictionary<string, StatsCollection> namedCollections = new ConcurrentDictionary<string, StatsCollection>();

        static Stats()
        {
            var stats = new Stats();
            namedCollections[string.Empty] = stats;

            try
            {
                var heapBytes = new PerformanceCounter(".NET CLR Memory", "# Bytes in all Heaps", "_Global_");
                MachineGauge("clr.heap_mb_bytes", () => heapBytes.NextValue() / 1000);

                var counter = new PerformanceCounter(".NET CLR Memory", "% Time in GC", "_Global_");
                MachineGauge("clr.time_in_gc", () => counter.NextValue());

                var exceptions = new PerformanceCounter(".NET CLR Exceptions", "# of Exceps thrown / sec", "_Global_");
                MachineGauge("clr.exceptions_sec", () => exceptions.NextValue());

                var cpu = new PerformanceCounter("Processor", "% User Time", "_Total");
                MachineGauge("cpu.user_time", () => cpu.NextValue());

                var cacheTotalMemory = new PerformanceCounter("ASP.NET Applications", "Cache % Machine Memory Limit Used", "__Total__");
                MachineGauge("aspnet.cache.machine_memory_limit_pct", () => cacheTotalMemory.NextValue());

                var cacheProcessMemory = new PerformanceCounter("ASP.NET Applications", "Cache % Process Memory Limit Used", "__Total__");
                MachineGauge("aspnet.cache.process_memory_limit_pct", () => cacheProcessMemory.NextValue());

                var cacheRatio = new PerformanceCounter("ASP.NET Applications", "Cache Total Hit Ratio", "__Total__");
                MachineGauge("aspnet.cache.hit_ratio", () => cacheRatio.NextValue());

                var cacheTrims = new PerformanceCounter("ASP.NET Applications", "Cache Total Trims", "__Total__");
                MachineGauge("aspnet.cache.trims", () => cacheTrims.NextValue());
            } 
            catch (Exception e)
            {
                logger.Warn("Could not read performance counters so the performance counter gauges will not be available. " + 
                            " The current process probably does not have the right permissions.", e);
            }
        }

        public static void MachineGauge(string name, Func<double> gauge)
        {
            GetDefault().AddGauge(Environment.MachineName + "." + name, new Gauge(gauge));
        }

        public static void Gauge(string name, Func<double> gauge)
        {
            GetDefault().AddGauge(name, new Gauge(gauge));
        }

        public static void RemoveGauge(string name)
        {
            GetDefault().DeleteGauge(name);
        }

        public static void AddMetric(string name, int n)
        {
            GetDefault().RecordMetric(name, n);
        }

        public static void Time(string name, int millis)
        {
            GetDefault().RecordMetric(name, millis);
        }

        public static void Time(string name, Stopwatch timer)
        {
            GetDefault().RecordMetric(name, timer);
        }

        public static void Time(string tag, Action action)
        {
            GetDefault().RecordMetric(tag, action);
        }

        public static T Time<T>(string tag, Func<T> func)
        {
            return GetDefault().RecordMetric(tag, func);
        }

        public static Metric RemoveTime(string tag)
        {
            return GetDefault().DeleteTime(tag);
        }

        public static long Incr(string name, int count)
        {
            return GetDefault().Increment(name, count);
        }

        public static long Incr(string name)
        {
            return GetDefault().Increment(name);
        }

        public static AtomicLong RemoveCounter(string name)
        {
            return GetDefault().DeleteCounter(name);
        }

        public static StatsCollection GetDefault()
        {
            return namedCollections[string.Empty];
        }

        public static AtomicLong Delta(AtomicLong oldValue, AtomicLong newValue)
        {
            if (oldValue.Value <= newValue.Value)
                return new AtomicLong(newValue.Value - oldValue.Value);

            var count = (AtomicLong.MaxValue.Value - oldValue.Value) + (newValue.Value - AtomicLong.MinValue.Value);
            return new AtomicLong(count + 1);
        }

        public static void WriteJson(JsonWriter jsonWriter)
        {
            var stats = GetDefault();

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("metrics");
            jsonWriter.WriteStartObject();
            foreach (var metric in stats.Metrics)
            {
                jsonWriter.WritePropertyName(metric.Key.ToLower());
                metric.Value.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndObject();
            jsonWriter.WritePropertyName("counters");
            jsonWriter.WriteStartObject();
            foreach (var counter in stats.Counters)
            {
                jsonWriter.WritePropertyName(counter.Key.ToLower());
                jsonWriter.WriteValue(counter.Value.Value);
            }
            jsonWriter.WriteEndObject();
            jsonWriter.WritePropertyName("gauges");
            jsonWriter.WriteStartObject();
            foreach (var gauge in stats.Gauges)
            {
                jsonWriter.WritePropertyName(gauge.Key.ToLower());
                jsonWriter.WriteValue(gauge.Value.Value);
            }
            jsonWriter.WriteEndObject();
        }

        public static IDictionary<string, object> ToDictionary()
        {
            var stats = GetDefault();
            return new SortedDictionary<string, object>
            {
              { "counters", BuildDict(stats.Counters, c => c.Value) },
              { "metrics", BuildDict(stats.Metrics, m => m.ToDictionary()) },
              { "gauges", BuildDict(stats.Gauges, g => g.Value) }
            };
        }

        private static SortedDictionary<string, object> BuildDict<TRender, TValue>(IEnumerable<KeyValuePair<string, TValue>> origin, Func<TValue, TRender> display)
        {
            var destination = new SortedDictionary<string, object>();
            origin.Each(kv => destination[kv.Key] = display(kv.Value));
            return destination;
        }

    }
}
