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
﻿using System.Threading;

namespace OstrichNet
{
    public class AtomicLong
    {
        public static readonly AtomicLong MinValue = new AtomicLong(long.MinValue);
        public static readonly AtomicLong MaxValue = new AtomicLong(long.MaxValue);

        private long value;

        public AtomicLong(long value)
        {
            this.value = value;
        }

        public long Value
        {
            get { return Interlocked.Read(ref value); }
        }

        public AtomicLong Increment()
        {
            Interlocked.Increment(ref value);
            return this;
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref value);
        }

        public AtomicLong Increment(int n)
        {
            Interlocked.Add(ref value, n);
            return this;
        }

        public long IncrementAndGet(int n)
        {
            return Interlocked.Add(ref value, n);
        }

        public bool Equals(AtomicLong other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.value == value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (AtomicLong)) return false;
            return Equals((AtomicLong) obj);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}
