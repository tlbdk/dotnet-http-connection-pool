using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace HttpConnectionPoolSampleDotnetFramework
{
    public class ConnectionTest
    {
        private int _successfulCalls = 0;
        private int _failedCalls = 0;

        public void StartOldStyle(string url, int numberOfThreads, ConcurrentQueue<string> payloads)
        {
            ThreadPool.SetMaxThreads(5000, 5000);
            ThreadPool.SetMinThreads(5000, 5000);

            ServicePointManager.DefaultConnectionLimit = numberOfThreads;
            ServicePointManager.MaxServicePointIdleTime = 60000;
            var endPoint = ServicePointManager.FindServicePoint(new Uri(url));
            // Leaks connections
            // endPoint.ConnectionLeaseTimeout = 1000;
            // endPoint.MaxIdleTime = 1000;

            endPoint.ConnectionLeaseTimeout = 2 * 60 * 1000;
            endPoint.MaxIdleTime = 60 * 1000;

            List<Task> tasks = new List<Task>();
            Console.WriteLine("Creating tasks");
            for (var i = 0; i < numberOfThreads; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Console.WriteLine("Started thread");
                    try
                    {
                        string payload;
                        while (payloads.TryDequeue(out payload))
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                            request.Method = "GET";
                            request.KeepAlive = true;

                            Stopwatch stopwatch = Stopwatch.StartNew();
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            var content = (new StreamReader(response.GetResponseStream()).ReadToEnd());
                            stopwatch.Stop();
                            Console.WriteLine("{0}: {1} - {2}", url, content, stopwatch.ElapsedMilliseconds);
                            response.Close();
                            Interlocked.Increment(ref _successfulCalls);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _failedCalls);
                    }
                }));
            }

            Console.WriteLine("Waiting from tasks");
            foreach (var task in tasks)
            {
                task.Wait();
            }
            Console.WriteLine("success: {0}, fail: {1}", _successfulCalls, _failedCalls);
        }


        public void StartNewStyle(string url, int numberOfThreads, ConcurrentQueue<string> payloads)
        {
            ThreadPool.SetMaxThreads(5000, 5000);
            ThreadPool.SetMinThreads(5000, 5000);

            ServicePointManager.DefaultConnectionLimit = 10;
            //var endPoint = ServicePointManager.FindServicePoint(new Uri(url));
            //endPoint.ConnectionLeaseTimeout = 2 * 60 * 000;
            //endPoint.MaxIdleTime = 60 * 1000;

            HttpClient httpClient = new HttpClient();
            List<Task> tasks = new List<Task>();
            Console.WriteLine("Creating tasks");
            for (var i = 0; i < numberOfThreads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    Console.WriteLine("Started thread");
                    try
                    {
                        string payload;
                        while (payloads.TryDequeue(out payload))
                        {
                            Stopwatch stopwatch = Stopwatch.StartNew();
                            var response = await httpClient.GetAsync(url);
                            var content = response.Content.ReadAsStringAsync();
                            stopwatch.Stop();
                            Console.WriteLine("{0}: {1} - {2}", url, content, stopwatch.ElapsedMilliseconds);
                            Interlocked.Increment(ref _successfulCalls);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _failedCalls);
                    }
                }));
            }

            Console.WriteLine("Waiting from tasks");
            foreach (var task in tasks)
            {
                task.Wait();
            }
            Console.WriteLine("success: {0}, fail: {1}", _successfulCalls, _failedCalls);
        }
    }
}
