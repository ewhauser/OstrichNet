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
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using OstrichNet.IO;
using OstrichNet.Util;

namespace OstrichNet.Service
{
    public static class CarbonWriterFactory
    {
        //TODO Internal configuration classes can't be used here, so this needs work.
        private static readonly Lazy<CarbonWriter> carbonWriter = new Lazy<CarbonWriter>(() => new CarbonWriter(null), LazyThreadSafetyMode.ExecutionAndPublication);

        public static CarbonWriter Instance()
        {
            return carbonWriter.Value;
        }
    }

    public class CarbonWriter : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BlockingCollection<string> sendQueue = new BlockingCollection<string>(1000);
        private readonly Thread senderThread;

        private readonly StatsListener listener;
        private readonly Timer collectionTimer;
        private readonly object locker = new object();
        private CarbonWriterConfiguration config;
        private TcpClient client;

        public CarbonWriter(CarbonWriterConfiguration config)
        {
            listener = StatsListener.Default;
            collectionTimer = new Timer(Periodic, null, Timeout.Infinite, 60000);
            collectionTimer.Change(10000, 60000);
            senderThread = new Thread(Sender) { Name = "carbon-sender", IsBackground = true };
            OnConfigChange(config);
        }

        private void OnConfigChange(CarbonWriterConfiguration configuration)
        {
            lock (locker)
            {
                config = configuration;
                if (client != null)
                {
                    client.Close();
                    client = null;
                }

                if (config.Enabled && !senderThread.IsAlive)
                {
                    collectionTimer.Change(0, config.BufferMillis);
                    senderThread.Start();
                }
            }
        }

        private void Periodic(object state)
        {
            if (!Monitor.TryEnter(locker)) return;

            try
            {
                StringBuilder metrics = new StringBuilder();
                listener.GetCounters().Each(kv => metrics.Append(BuildCounter(kv.Key, kv.Value.Value)));
                listener.GetGauges().Each(kv => metrics.Append(BuildGauges(kv.Key, kv.Value.Value)));
                listener.GetMetrics().Each(kv => metrics.Append(BuildTiming(kv.Key, kv.Value)));
                AppendToQueue(metrics.ToString());
            }
            catch (Exception e)
            {
                logger.Error("Error processing stats", e);
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        private void AppendToQueue(string item)
        {
            try
            {
                sendQueue.TryAdd(item, TimeSpan.FromSeconds(30));
            } catch (InvalidOperationException) {}
        }

        private void Sender()
        {
            while (!sendQueue.IsCompleted)
            {
                string data = null;
                try
                {
                    data = sendQueue.Take();
                }
                catch (InvalidOperationException) { }

                Flush(data);
            }

            var items = sendQueue.ToArray();
            if (items.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                items.Each(i => sb.Append(i));
                Flush(sb.ToString());
            }
        }

        private void Flush(String message)
        {
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                var client = Connect();

                if (client == null) return;

                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, client.GetStream(), Encoding.UTF8))
                    writer.Write(message);
            } 
            catch (Exception e)
            {
                logger.Error("Unable to flush buffer - this is not a fatal error", e);
            }
            finally
            {
                client.Close();
                client = null;
            }
        }

        private TcpClient Connect()
        {
            try
            {
                if (client == null)
                {
                    client = new TcpClient();
                    client.Connect(config.ReceiverHost, config.ReceiverPort);
                    client.Client.SendTimeout = 2000;
                } 
                else if (!client.Connected)
                {
                    client.Connect(config.ReceiverHost, config.ReceiverPort);    
                }
            }
            catch (Exception e)
            {
                logger.Error("Unable to connect to " + config.ReceiverHost + ":" + config.ReceiverPort, e);
            }
            return client;
        }

        private string BuildCounter(string key, long value)
        {
            key = SanitizeKey(key);
            return config.Prefix + ".counters." + key + " " + value + " " + DateTimeUtils.CurrentTimeMillis / 1000 + "\n";
        }

        private string BuildGauges(string key, double value)
        {
            key = SanitizeKey(key);
            return config.Prefix + ".gauges." + key + " " + value + " " + DateTimeUtils.CurrentTimeMillis / 1000 + "\n";
        }

        private string BuildTiming(string key, Metric value)
        {
            key = SanitizeKey(key);
            var ts = DateTimeUtils.CurrentTimeMillis / 1000;
            var timing = new StringBuilder();
            timing.Append(config.Prefix + ".timers." + key + ".mean " + value.Mean + ' ' + ts + "\n");
            timing.Append(config.Prefix + ".timers." + key + ".upper " + value.Max + ' ' + ts + "\n");
            timing.Append(config.Prefix + ".timers." + key + ".upper_99 " + value.Histogram.GetPercentile(0.99d) + ' ' + ts + "\n");
            timing.Append(config.Prefix + ".timers." + key + ".lower " + value.Mean + ' ' + ts + "\n");
            timing.Append(config.Prefix + ".timers." + key + ".count " + value.Count + ' ' + ts + "\n");
            return timing.ToString();
        }

        public static string SanitizeKey(string key)
        {
            key = Regex.Replace(key, @"\s+", "_");
            key = key.Replace('/', '-');
            key = Regex.Replace(key, @"[^a-zA-Z_\-0-9\.]", "");
            return key.Length > 0 ? key.ToLower() : "1";
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            try
            {
                collectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                collectionTimer.Dispose();
                sendQueue.CompleteAdding();
                if (senderThread != null && senderThread.IsAlive) senderThread.Join(2000);
// ReSharper disable EmptyGeneralCatchClause
            } 
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
                //ignore
            }
        }

        ~CarbonWriter()
        {
            Dispose(false);
        }
    }

    public class CarbonWriterConfiguration
    {
        public CarbonWriterConfiguration()
        {
            Prefix = "stats";
            ReceiverPort = 2003;
            BufferMillis = 10000;
            Enabled = false;
        }

        public string ReceiverHost { get; set; }
        public int ReceiverPort { get; set; }
        public string Prefix { get; set; }
        public int BufferMillis { get; set; }
        public bool Enabled { get; set; }
    }
}
