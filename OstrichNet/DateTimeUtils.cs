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

namespace OstrichNet
{
    public static class DateTimeUtils
    {
        private const int NANOS_IN_A_MILLISECOND = 1000000;

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long epochTicks = epoch.Ticks;

        public static string ToIso8601(this long millisFromEpoch, DateTimePrecision precision)
        {
            var date = new DateTime(epochTicks + millisFromEpoch*TimeSpan.TicksPerMillisecond);
            return ToIso8601(date, precision);
        }

        public static string ToIso8601(this DateTime date, DateTimePrecision precision)
        {
            switch (precision)
            {
                case DateTimePrecision.Seconds:
                    return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                default:
                    throw new InvalidOperationException();
            }
        }

        public static long MillisFromEpoch(this DateTime dateTime)
        {
            var t = dateTime - epoch;
            return (long)t.TotalMilliseconds;
        }

        public static long NanosFromEpoch(this DateTime dateTime)
        {
            return MillisFromEpoch(dateTime) / NANOS_IN_A_MILLISECOND;
        }

        public static long CurrentTimeMillis
        {
            get { return DateTime.UtcNow.MillisFromEpoch(); }
        }

        public static long CurrentTimeNanos
        {
            get { return DateTime.UtcNow.NanosFromEpoch() / NANOS_IN_A_MILLISECOND; }
        }

        public static DateTime Epoch
        {
            get { return epoch; }
        }
    }


    public enum DateTimePrecision
    {
        Seconds
    }
}
