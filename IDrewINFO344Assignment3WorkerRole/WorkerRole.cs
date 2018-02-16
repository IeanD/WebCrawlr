using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IDrewINFO344Assignment3ClassLibrary;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace IDrewINFO344Assignment3WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private List<string> _disallowed;

        public override void Run()
        {
            Trace.TraceInformation("IDrewINFO344Assignment3WorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("IDrewINFO344Assignment3WorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("IDrewINFO344Assignment3WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("IDrewINFO344Assignment3WorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            CloudQueue urlQueue = new AzureQueue(
                ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrqueue")
                .GetQueue();

            CloudTable cmdTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "cmdtable")
                .GetTable();

            TableQuery<CrawlerCmd> cmdQuery = new TableQuery<CrawlerCmd>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "workerRoleCmd"))
                .Take(1);

            var currentCmd = cmdTable.ExecuteQuery(cmdQuery);

            while (!currentCmd.Any())
            {
                currentCmd = cmdTable.ExecuteQuery(cmdQuery);
                Thread.Sleep(5000);
            }

            if (currentCmd != null && currentCmd.First().Cmd == "START")
            {
                string tempPath = System.IO.Path.GetTempFileName();
                WebClient wc = new WebClient();
                wc.DownloadFile(currentCmd.First().Domain, tempPath);

                InitializeCrawl(tempPath, urlQueue);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                currentCmd = cmdTable.ExecuteQuery(cmdQuery);

                if (currentCmd.First().Cmd == "START")
                {
                    CloudQueueMessage nextUrlMsg = urlQueue.GetMessage();
                    string nextUrl = nextUrlMsg.AsString;
                    if (nextUrl.EndsWith(".xml"))
                    {
                        using (WebClient wc = new WebClient())
                        {
                            //var s = wc.DownloadData
                        }
                    }
                }
            }

            Thread.Sleep(1000);
        }

        private void InitializeCrawl(string filePath, CloudQueue urlQueue)
        {
            this._disallowed = new List<string>();
            StreamReader input = new StreamReader(filePath);
            string currLine = "";
            string currUserAgent = "";
            List<string> sitemaps = new List<string>();
            while ((currLine = input.ReadLine()) != null)
            {
                if (currLine.StartsWith("Sitemap: "))
                {
                    sitemaps.Add(currLine.Substring(9));
                    CloudQueueMessage msg = new CloudQueueMessage(currLine.Substring(9));
                    urlQueue.AddMessage(msg);
                }
                else if (currLine.StartsWith("User-agent: "))
                {
                    currUserAgent = currLine.Substring(12);
                }
                else if (currLine.StartsWith("Disallow: ") && currUserAgent == "*")
                {
                    _disallowed.Add(currLine.Substring(10));
                }
            }
        }
    }
}
