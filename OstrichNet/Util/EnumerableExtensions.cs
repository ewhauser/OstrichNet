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

namespace OstrichNet.Util
{
    public static class EnumerableExtensions
    {

        /// <summary>
        /// Executes the action for each item in the source. This function causes side effects 
        /// to the global state and should be used accordingly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static void Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException("source");

            foreach (T target in source)
                action(target);
        }

        /// <summary>
        /// Executes the action for each item in the source and supplies the index to the action. This 
        /// function causes side effects to the global state and should be used accordingly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        public static void EachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null) throw new ArgumentNullException("source");

            for (int i = 0; i < source.Count(); i++)
            {
                action(source.ElementAt(i), i);
            }
        }

        /// <summary>
        /// Executes the action for each item in the source. This function causes side effects 
        /// to the global state and should be used accordingly.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static void Each<K,V>(this IDictionary<K,V> source, Action<K,V> action)
        {
            if (source == null) throw new ArgumentNullException("source");

            foreach (KeyValuePair<K, V> target in source)
                action(target.Key, target.Value);
        }
    }
}
