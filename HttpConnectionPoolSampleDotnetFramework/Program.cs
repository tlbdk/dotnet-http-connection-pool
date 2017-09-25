using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HttpConnectionPoolSampleDotnetFramework
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            Console.WriteLine("Started application");
            ConcurrentQueue<string> payloads = new ConcurrentQueue<string>();
			for (int i = 0; i < 10000; i++)
			{
				payloads.Enqueue(i.ToString());
			}
            var test = new ConnectionTest();
            test.StartOldStyle("https://api.staging.connectedcars.io/healthz", 100, payloads);
        }
    }
}