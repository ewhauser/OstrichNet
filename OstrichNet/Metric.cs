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
using Newtonsoft.Json;
using OstrichNet.Util;

namespace OstrichNet
{
    public class Metric
    {
        private static readonly double[] percentiles = new[] { 0.25d, 0.5d, 0.75d, 0.9d, 0.99d, 0.999d, 0.9999d };

        private double accumulatedVariance;

        public Metric()
        {
            Histogram = new Histogram();
        }

        protected Metric(double mean, double min, double max, long count, double accumulatedVariance, Histogram histogram)
        {
            Mean = mean;
            Min = min;
            Max = max;
            Count = count;
            this.accumulatedVariance = accumulatedVariance;
            Histogram = histogram;
        }

        public double Mean { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        public long Count { get; private set; }

        public double StandardDeviation
        {
            get { return Count == 0 ? 0 : Math.Sqrt(accumulatedVariance / Count); }
        }

        public Histogram Histogram { get; private set; }
        
        /// <summary>
        /// This method updates the calculated statistics with a new logged execution time.
        /// </summary>
        /// <param name="elapsedTime">The elapsed time being used to update the statistics in milliseconds.</param>
        /// <returns>this Metric instance</returns>
        public virtual void Add(double elapsedTime)
        {
            if (elapsedTime < 0) return;

            lock (this)
            {
                Count += 1;
                Histogram.Add(Convert.ToInt32(elapsedTime));
                if (Count == 1)
                {
                    Mean = elapsedTime;
                    Min = elapsedTime;
                    Max = elapsedTime;
                }
                else
                {
                    var delta = elapsedTime - Mean;
                    Mean += (delta / Count);
                    accumulatedVariance = delta * (elapsedTime - Mean);
                    Min = Math.Min(elapsedTime, Min);
                    Max = Math.Max(elapsedTime, Max);
                }
            }
        }

        public Dictionary<string, long> GetPercentiles()
        {
            lock (this)
            {
                var answer = new Dictionary<string, long>();
                if (Histogram != null) percentiles.Each(p => answer[Convert.ToString(p)] = Histogram.GetPercentile(p));
                return answer;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                Mean = 0;
                Min = 0;
                Max = 0;
                Count = 0;
                accumulatedVariance = 0;
                Histogram.Clear();
            }
        }

        public bool Equals(Metric other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Mean.Equals(Mean) && other.Min.Equals(Min) && other.Max.Equals(Max) && other.Count == Count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Metric)) return false;
            return Equals((Metric) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Mean.GetHashCode();
                result = (result*397) ^ Min.GetHashCode();
                result = (result*397) ^ Max.GetHashCode();
                result = (result*397) ^ Count.GetHashCode();
                return result;
            }
        }

        public void WriteJson(JsonWriter jsonWriter)
        {
            lock (this)
            {
                var serializer = new JsonSerializer();
                var dict = ToDictionary();
                dict.Add("histogram", Histogram);
                serializer.Serialize(jsonWriter, ToDictionary());
            }
        }

        public IDictionary<string, object> ToDictionary()
        {
            lock (this)
            {
                return new Dictionary<string, object>
                       {
                           { "average", Mean },
                           { "count", Count },
                           { "max", Max },
                           { "min", Min },
                           { "p0",  Histogram.GetPercentile(0d) },
                           { "p25",  Histogram.GetPercentile(.25d) },
                           { "p50",  Histogram.GetPercentile(.5d) },
                           { "p75",  Histogram.GetPercentile(.75d) },
                           { "p9",  Histogram.GetPercentile(.9d) },
                           { "p99",  Histogram.GetPercentile(.99d) },
                           { "p999",  Histogram.GetPercentile(.999d) },
                           { "p9999",  Histogram.GetPercentile(.999d) },
                           { "standard_deviation", StandardDeviation }
                       };
            }
        }

        public override string ToString()
        {
            lock (this)
            {
                var percentiles = GetPercentiles().Select(kv => string.Format("{0}={1}", kv.Key, kv.Value));
                return String.Format("mean[{0}] stddev[{1}] min[{2}] max[{3}] count[{4}] p[{5}]",
                                     Mean, StandardDeviation, Min, Max, Count, string.Join(",", percentiles));
            }
        }

        public Metric Snapshot()
        {
            lock (this)
            {
                return new SnapshotMetric(Mean, Min, Max, Count, accumulatedVariance, Histogram);
            }
        }
    }

    public class FanoutMetric : Metric
    {
        private readonly HashSet<Metric> fanout = new HashSet<Metric>();

        public void AddFanout(Metric metric)
        {
            fanout.Add(metric);
        }

        public override void Add(double n)
        {
            lock (this)
            {
                fanout.Each(f => f.Add(n));
                base.Add(n);
            }
        }
    }

    public class SnapshotMetric : Metric
    {
        public SnapshotMetric(double mean, double min, double max, long count, double accumulatedVariance, Histogram histogram)
            : base(mean, min, max, count, accumulatedVariance, histogram.ImmutableCopy())
        {
        }

        public override void Add(double elapsedTime)
        {
            throw new InvalidOperationException("Snapshot metrics are immutable");
        }
    }
}
