using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace HttpConnectionPoolSampleDotnetFramework
{
    public class ConnectionTest
    {
        private int _successfulCalls = 0;
        private int _failedCalls = 0;

        public void Start(string url, int numberOfThreads, ConcurrentQueue<string> payloads)
        {
			ThreadPool.SetMaxThreads(1000, 1000);
			ThreadPool.SetMinThreads(1000, 1000);

			ServicePointManager.DefaultConnectionLimit = 10;
            var endPoint = ServicePointManager.FindServicePoint(new Uri(url));
            // endPoint.ConnectionLeaseTimeout = 1000;
            endPoint.MaxIdleTime = 1000;

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
                            request.KeepAlive = false;

                            Stopwatch stopwatch = Stopwatch.StartNew();
                            Console.WriteLine("Start");
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
            foreach(var task in tasks) {
                task.Wait();
            }
            Console.WriteLine("success: {0}, fail: {1}", _successfulCalls, _failedCalls);
        }
    }
}
