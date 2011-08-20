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
using System.Collections.Generic;

namespace OstrichNet
{
    public class Histogram : IEnumerable<int>
    {
        private static readonly int[] bucketOffsets = new[]
                                    {
                                        1, 2, 3, 4, 5, 7, 9, 11, 14, 18, 24, 31, 40, 52, 67, 87, 113, 147, 191, 248,
                                        322, 418, 543, 706, 918, 1193, 1551, 2016, 2620, 3406, 4428, 5757, 7483,
                                        9728, 12647, 16441, 21373, 27784, 36119, 46955, 61041, 79354, 103160, 134107,
                                        174339, 226641, 294633, 383023, 497930, 647308, 841501, 1093951
                                    };
        private static readonly int bucketOffsetSize = bucketOffsets.Length;
        
        private readonly int[] buckets = new int[bucketOffsetSize + 1];
        public int Total;

        public Histogram()
        {
        }

        protected Histogram(int[] buckets, int total)
        {
            this.buckets = buckets;
            Total = total;
        }

        public virtual void AddToBucket(int index)
        {
            buckets[index] += 1;
            Total += 1;
        }

        public virtual void Add(int n)
        {
            AddToBucket(BucketIndex(n));
        }

        public virtual void Clear()
        {
            for (int i = 0; i < buckets.Length - 1; i++)
            {
                buckets[i] = 0;
            }
            Total = 0;
        }

        public int[] Get(bool reset = false)
        {
            var bucketLength = buckets.Length;
            var response = new int[bucketLength];
            Array.Copy(buckets, response, bucketLength);
            if (reset) Clear();
            return response;
        }

        public int GetPercentile(double percentile)
        {
            int sum = 0;
            int index = 0;
            while (sum < percentile * Total)
            {
                sum += buckets[index];
                index += 1;
            }

            if (index == 0)
            {
                return 0;
            }
            
            if (index - 1 >= bucketOffsetSize)
            {
                return Int32.MaxValue;
            }
            
            return bucketOffsets[index - 1] - 1;
        }

        public void Merge(Histogram other)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] += other.buckets[i];
            }
            Total += other.Total;
        }
       
        public static int BucketIndex(int key)
        {
            return BinarySearch(key);
        }

        private static int BinarySearch(int[] array, int key, int low, int high)
        {
            if (low > high)
                return low;

            var mid = (low + high + 1) >> 1;
            var midValue = array[mid];
            if (midValue < key)
            {
                return BinarySearch(array, key, mid + 1, high);
            }
            if (midValue > key)
            {
                return BinarySearch(array, key, low, mid - 1);
            }
            return mid + 1;
        }

        public static int BinarySearch(int key)
        {
            return BinarySearch(bucketOffsets, key, 0, bucketOffsetSize - 1);
        }

        public Histogram ImmutableCopy()
        {
            return new Histogram(Get(), Total);
        }

        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)buckets).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ImmutableHistogram : Histogram
    {
        public ImmutableHistogram(int[] buckets, int total) : base(buckets, total)
        {
        }

        public override void AddToBucket(int index)
        {
            throw new InvalidOperationException("Class is immutable");
        }

        public override void Add(int n)
        {
            throw new InvalidOperationException("Class is immutable");
        }

        public override void Clear()
        {
            throw new InvalidOperationException("Class is immutable");
        }
    }
}
