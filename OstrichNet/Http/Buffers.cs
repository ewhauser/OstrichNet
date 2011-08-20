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
using System.Text;

namespace OstrichNet.Http
{
    public static partial class Extensions
    {
        static int MaxHeaderLength = 1024 * 10;
        static int BufferSize = 1024 * 2;

        internal static IEnumerable<byte> GetBytes(this IEnumerable<ArraySegment<byte>> buffers)
        {
            foreach (var seg in buffers)
                for (int i = seg.Offset; i < seg.Offset + seg.Count; i++)
                    yield return seg.Array[i];
        }

        internal static string GetString(this IEnumerable<ArraySegment<byte>> buffers)
        {
            return buffers.GetString(Encoding.UTF8);
        }

        internal static string GetString(this IEnumerable<ArraySegment<byte>> buffers, Encoding encoding)
        { 
            var sb = new StringBuilder();

            foreach (var b in buffers)
                sb.Append(encoding.GetString(b.Array, b.Offset, b.Count));

            return sb.ToString();
        }

        internal static string GetString(this ArraySegment<byte> b)
        {
            return b.GetString(Encoding.UTF8);
        }

        internal static string GetString(this ArraySegment<byte> b, Encoding encoding)
        {
            return encoding.GetString(b.Array, b.Offset, b.Count);
        }
    }
}
