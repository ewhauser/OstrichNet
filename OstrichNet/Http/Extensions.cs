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

namespace OstrichNet.Http
{
    public static partial class Extensions
    {
        public static Action<Action<T>, Action<Exception>> AsContinuation<T>(this IEnumerable<object> enumerable)
        {
            return enumerable.GetEnumerator().AsContinuation<T>(null);
        }

        public static Action<Action<T>, Action<Exception>> AsContinuation<T>(this IEnumerable<object> enumerable, Action<Action> trampoline)
        {
            return enumerable.GetEnumerator().AsContinuation<T>(trampoline);
        }

        public static Action<Action<T>, Action<Exception>> AsContinuation<T>(this IEnumerator<object> enumerator)
        {
            return enumerator.AsContinuation<T>(null);
        }

        public static Action<Action<T>, Action<Exception>> AsContinuation<T>(this IEnumerator<object> enumerator, Action<Action> trampoline)
        {
            return (result, exception) =>
                Continuation.Enumerate<T>(enumerator, result, exception, trampoline);
        }

        public static ContinuationState<T> AsCoroutine<T>(this IEnumerable<object> iteratorBlock)
        {
            return iteratorBlock.GetEnumerator().AsContinuationState<T>(null);
        }

        public static ContinuationState<T> AsCoroutine<T>(this IEnumerable<object> iteratorBlock, Action<Action> trampoline)
        {
            return iteratorBlock.GetEnumerator().AsContinuationState<T>(trampoline);
        }

        public static ContinuationState<T> AsContinuationState<T>(this IEnumerator<object> enumerator)
        {
            return enumerator.AsContinuationState<T>(null);
        }

        public static ContinuationState<T> AsContinuationState<T>(this IEnumerator<object> enumerator, Action<Action> trampoline)
        {
            return new ContinuationState<T>(AsContinuation<T>(enumerator, trampoline));
        }
    }
}
