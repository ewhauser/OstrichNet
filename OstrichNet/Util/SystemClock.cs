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

namespace OstrichNet.Util
{
    public static class SystemClock
    {
        public static Func<DateTime> Now = () => DateTime.Now;
        public static Func<DateTime> UtcNow = () => DateTime.UtcNow;

        public static long Minutes()
        {
            return Convert.ToInt64((UtcNow() - DateTimeUtils.Epoch).TotalMinutes);
        }

        public static long Seconds()
        {
            return Convert.ToInt64((UtcNow() - DateTimeUtils.Epoch).TotalSeconds);
        }

        public static long Millis()
        {
            return Convert.ToInt64((UtcNow() - DateTimeUtils.Epoch).TotalMilliseconds);
        }

        public static DateTime Minus(TimeSpan ts)
        {
            return UtcNow() - ts;
        }

        public static void Advance(TimeSpan timeSpan)
        {
            var current = Now();
            Now = () => current + timeSpan;
        }

        public static void WaitFor(long millis)
        {
            var until = UtcNow() + TimeSpan.FromMilliseconds(millis);
            while (until < UtcNow())
            {
                //spin
            }
        }

        public static void WithFrozen(Action action)
        {
            using (Freeze())
            {
                action();
            }
        }

        public static IDisposable Freeze()
        {
            var now = UtcNow();
            UtcNow = () => now;
            return new DisposableAction(() => { UtcNow = () => DateTime.UtcNow; });
        }
    }
}
