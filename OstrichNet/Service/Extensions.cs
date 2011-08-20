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
using System.Linq;
using System.Text;
using OstrichNet.Util;

namespace OstrichNet.Service
{
    public static class Extensions
    {
        public static void Write(this IDictionary<string, object> dict, StringBuilder builder, int indent)
        {
            dict.Each((key, value) =>
            {
                if (value == null) return;

                Enumerable.Range(0, indent).Each(i => builder.Append("  "));
                if (typeof(IDictionary<string, object>).IsAssignableFrom(value.GetType()))
                {
                    builder.Append(key).Append(": ").AppendLine();
                    Write((IDictionary<string, object>)value, builder, indent + 1);
                }
                else if (typeof(string) != value.GetType() && typeof(IEnumerable).IsAssignableFrom(value.GetType()))
                {
                    builder.Append(key)
                        .Append(": (")
                        .Append(string.Join(",", from object item in (IEnumerable)value select Convert.ToString(item)))
                        .Append(")")
                        .AppendLine();
                }
                else
                {
                    builder.Append(key)
                        .Append(": ")
                        .Append(value)
                        .AppendLine();
                }
            });
        }
    }
}
