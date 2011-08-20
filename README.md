# Overview

Port Twitter's Ostrich library from Scala to .NET

# License

Apache 2.0

# Goals

- Collect runtime performance statistics on an individual machine
- View the data in real-time
- Simple and straightforward
- Low performance overhead
- Readable via HTTP

# Non-goals

- Persistence
- Cluster wide graphing (although Cacti/Munin/Ganglia/Carbon can consume the output - there is a sample CarbonWriter included)

# Dependencies

- log4net
- JSON.NET
- nunit

For ease of deployment, two Apache 2.0 licensed libraries have files embedded in the assembly under a different namespace - MiscUtil and Kayak HTTP Server.

# Running

To enable the diagnostics library within an application, you need to add the following lines at startup:

```csharp
var diagnosticsService = new HttpDiagnosticsService();
service.Start();
```

This will start a socket on the specified port that listens for HTTP requests.  The performance library contains a small embedded web server to listen for requests.  If something goes wrong in the startup of the service, it will not thrown an exception that will take your application down.  Startup exceptions are logged, but not thrown.  You should also dispose the diagnostics service at shutdown using Dispose().
 
If you are wanting to run the service in a web application, then integration can we setup through web.config.  You can add the following sections to web.config:

Integrated Pipeline:

```xml
<appSettings>
     ... <!-- Some more information at the bottom of this page about which port number to use -->
     <add key="OstrichNet.Sercice.HttpDiagnosticsService.port" value="7006" />
 </appSettings>
 
 <system.webServer> 
    ... 
   <modules>
     ...
     <add name="DiagnosticsModule preCondition="managedHandler" type="OstrichNet.Service.DiagnosticsHttpModule, OstrichNet" />
   </modules>
 </system.webserver>
```

Classic Pipeline:

```xml
<appSettings>
    ... <!-- Some more information at the bottom of this page about which port number to use -->
    <add key="OstrichNet.Service.HttpDiagnosticsService.port" value="7006" />
</appSettings>

<system.web>
    ...
    <httpModules>
        ...
        <add name="DiagnosticsModule" type="OstrichNet.Service.DiagnosticsHttpModule, OstrichNet" />
    </httpModules>
</system.web>
```

# Counters, Metrics, and Gauges

Reference the Ostrich docs, but here are some code examples in C#:

## Counters

```csharp
Stats.Incr("counter_name");
```

## Metrics

```csharp
Stopwatch stopwatch = new Stopwatch();
stopwatch.start();
...code to time...
Stats.Time("db_call", stopwatch);
```

```csharp
Stats.Time("db_call"), () => { ... });  
```

```csharp
var dbRows = Stats.Time("db_call"), () => { ... });  
```

## Gauges

```csharp
Stats.Gauge(“current_temperature”, () => tempMonitor.CalculateTemp());
```

# Viewing

## Server Info

```bash
curl http://127.0.0.1:9999/server_info
```

```javascript
{"machine":"SERVER_NAME","process":"ProcessName","app_name":"ProcessName, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","start_time":"2011-08-19T02:32:59Z","uptime":"92878085.125"}
```

We can make this easier on the eyes by adding the pretty parameter:

```bash
curl http://127.0.0.1:9999/server_info?pretty=true
```

```javascript
{
  "machine": "SERVER_NAME",
  "process": "ProcessName",
  "app_name": "ProcessName, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
  "start_time": "2011-03-24T12:31:01Z",
  "uptime": "319452.0508"
}
```

Or by adding .txt to the end of the URL:

```bash
curl http://127.0.0.1:9999/server_info.txt
```

```
server_info:
  machine: SERVER_NAME
  process: ProcessName
  app_name: ProcessName, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
  start_time: 2011-03-24T12:31:01Z
  uptime: 409848.1
```

## Stats

Stats allow you view the output of counters, metrics, and gauges.

```bash
curl http://127.0.0.1:9999/stats/
```

```javascript
{
  "metrics": {
    "process_data": {
      "average": 0.852127284825243,
      "count": 25549,
      "max": 162.0,
      "min": 0.0,
      "p0": 0,
      "p25": 0,
      "p50": 0,
      "p75": 1,
      "p9": 2,
      "p99": 6,
      "p999": 30,
      "p9999": 30,
      "standard_deviation": 0.0053312127955066554
    },
    "process_data2": {
      "average": 0.84910756223578809,
      "count": 25548,
      "max": 162.0,
      "min": 0.0,
      "p0": 0,
      "p25": 0,
      "p50": 0,
      "p75": 1,
      "p9": 2,
      "p99": 6,
      "p999": 30,
      "p9999": 30,
      "standard_deviation": 0.0053124243024031473
    }
  },
  "counters": {
    "process_data": 25549
  },
  "gauges": {
    "cpu_user_time": 2.2254095077514648,
    "clr_time_in_gc": 0.16423256695270538,
    "clr_heap_bytes": 291951040.0
  }
}
```

Counters and gauges should be self-explanatory.  For metrics, the stats contain the average, min, max, count, standard deviation, and percentiles for an individual metric. 

## Reports

Reports are a simple HTML view of the raw stats and metrics.

```bash
curl http://127.0.0.1:9999/report/
```

## Graphs

```bash
http://127.0.0.1:9999/graph/
````

Graphs allow you to view the last hour of counters, measures, and gauges.  Visiting the graph page will return an a list of available items that are being graphed.

# FAQ

## What's the performance overhead? 

Collector output writes asynchronously in a background thread every minute.  dotTrace showed that adding metrics to resource intensive application added about .2% overhead which was much faster than the performance counters they replaced.
 
## What shouldn't you do?

Each counter/metric/gauge uses O(1) memory no matter how long the code is running or how many times they are mutated.  The default collector process is an O(N) operation, so exploding the total number of metrics/counters by doing something like appending customer key to every counter name would be a bad idea.  Sampling and tracing would be a more appropriate use case for trying to monitor performance at that level.  See Google's Dapper (http://research.google.com/pubs/pub36356.html). 

## Is this safe to run inside of IIS?

The service embeds inside of IIS on a daemon thread. It has succesfully been used with IIS 6.0 and IIS 7.0 without any issues.

If you are hosting multiple worker processes on the same machine, you'll find out that the library will have trouble binding to the same port twice from different processes.  It will attempt to bind to one port number higher than the configured value in that instance.
