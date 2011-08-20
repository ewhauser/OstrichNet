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
using System.Configuration;

namespace OstrichNet.Service
{
    public interface IServiceConfiguration
    {
        int Port { get; }
    }

    public class ServiceConfiguration : ConfigurationSection, IServiceConfiguration
    {
        [ConfigurationProperty("myAttrib1", DefaultValue = "7400", IsRequired = true)]
        [IntegerValidator(MinValue = 1024, MaxValue = 65535)]
        public int Port
        {
            get { return Convert.ToInt32(this["port"]); }
            set { this["port"] = value; }
        }
    }

    public class StaticServiceConfiguration : IServiceConfiguration
    {
        public StaticServiceConfiguration()
        {
            Port = 7000;
        }

        public StaticServiceConfiguration(int port)
        {
            Port = port;
        }

        public int Port { get; private set; }
    }
}
