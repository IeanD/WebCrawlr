using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
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
        private HashSet<string> _seenUrls;
        private HashSet<string> _addedUrls;
        private HashSet<string> _seenXmls;
        private Stack<string> _lastTenUrls;
        private int numXml = 0;
        private int numUrl = 0;
        private int numUrlsCrawled = 0;

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
            // Set up queues and tables
            CloudQueue xmlQueue = new AzureQueue(
                ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrxmlqueue")
                .GetQueue();

            CloudQueue urlQueue = new AzureQueue(
                ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrurlqueue")
                .GetQueue();

            CloudTable cmdTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "cmdtable")
                .GetTable();

            CloudTable errTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "errtable")
                .GetTable();

            CloudTable urlTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "urltable")
                .GetTable();

            CloudTable statusTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrstatustable")
                .GetTable();

            TableQuery<CrawlerCmd> cmdQuery = new TableQuery<CrawlerCmd>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "workerRoleCmd"))
                .Take(1);


            // Get the current cmd from the cmd table
            var currentCmd = cmdTable.ExecuteQuery(cmdQuery);

            // Re-execute cmd query periodically until current cmd exists
            while (!currentCmd.Any())
            {
                currentCmd = cmdTable.ExecuteQuery(cmdQuery);
                Thread.Sleep(5000);
            }

            // If start cmd given, initialize download of robots.txt
            // and populate the xmlQueue and _disallowed list
            if (currentCmd != null && currentCmd.First().Cmd == "START")
            {
                string tempPath = System.IO.Path.GetTempFileName();
                WebClient wc = new WebClient();
                wc.DownloadFile(currentCmd.First().Domain, tempPath);
                this._disallowed = new List<string>();
                this._addedUrls = new HashSet<string>();
                this._seenUrls = new HashSet<string>();
                this._seenXmls = new HashSet<string>();
                this._lastTenUrls = new Stack<string>(10);
                //ParseHtml("https://www.cnn.com/news", urlQueue, urlTable, errTable);

                InitializeCrawl(tempPath, xmlQueue);
            }

            // Recurring work
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                // Check the current cmd on each loop
                currentCmd = cmdTable.ExecuteQuery(cmdQuery);

                // Do work if current cmd is still "start"
                if (currentCmd.First().Cmd == "START")
                {
                    while (numXml > 0 && currentCmd.First().Cmd == "START")
                    {
                        currentCmd = cmdTable.ExecuteQuery(cmdQuery);

                        CloudQueueMessage nextXmlMsg = xmlQueue.GetMessage();
                        string nextXml = nextXmlMsg.AsString;
                        bool xmlIsValid = true;
                        foreach (string s in _disallowed)
                        {
                            if (nextXml.Contains(s))
                            {
                                xmlIsValid = false;
                            }
                        }

                        if(xmlIsValid)
                        {
                            if (nextXml.EndsWith(".xml"))
                            {
                                XElement sitemap = XElement.Load(nextXml);
                                string sitemapType = sitemap.Name.LocalName;
                                string nameSpace = sitemap.GetDefaultNamespace().ToString();
                                XName loc = XName.Get("loc", nameSpace);
                                XName elementSelector;
                                if (sitemapType == "sitemapindex")
                                {
                                    elementSelector = XName.Get("sitemap", nameSpace);
                                }
                                else
                                {
                                    elementSelector = XName.Get("url", nameSpace);
                                }

                                foreach (var element in sitemap.Elements(elementSelector))
                                {
                                    var currLocElement = element.Element(loc);
                                    string currLocValue = currLocElement.Value;
                                    if (currLocValue.Contains(".xml"))
                                    {
                                        if (!_seenXmls.Contains(currLocValue))
                                        {
                                            CloudQueueMessage x = new CloudQueueMessage(currLocValue);
                                            xmlQueue.AddMessage(x);
                                            _seenXmls.Add(currLocValue);
                                            numXml++;
                                        }
                                    }
                                    else
                                    {
                                        if (!_seenUrls.Contains(currLocValue))
                                        {
                                            CloudQueueMessage url = new CloudQueueMessage(currLocValue);
                                            urlQueue.AddMessage(url);
                                            _seenUrls.Add(currLocValue);
                                            numUrl++;
                                        }
                                    }

                                    Thread.Sleep(50);
                                }
                            }
                        }
                        xmlQueue.DeleteMessage(nextXmlMsg);
                        numXml--;
                        PerformanceCounter perf = new PerformanceCounter("Memory", "Available MBytes");
                        PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        WorkerRoleStatus currStatus = new WorkerRoleStatus(
                            "Loading",
                            (int)cpu.NextValue(),
                            (int)perf.NextValue(),
                            numUrlsCrawled,
                            _lastTenUrls
                            );
                        TableOperation insertStatus = TableOperation.InsertOrReplace(currStatus);
                        statusTable.Execute(insertStatus);

                        Thread.Sleep(50);
                    }



                    while (numUrl > 0 && currentCmd.First().Cmd == "START")
                    {
                        currentCmd = cmdTable.ExecuteQuery(cmdQuery);

                        CloudQueueMessage nextUrlMsg = urlQueue.GetMessage();
                        string nextUrl = nextUrlMsg.AsString;
                        bool urlIsAllowed = true;
                        foreach (string s in _disallowed)
                        {
                            if (nextUrl.Contains(s))
                            {
                                urlIsAllowed = false;
                            }
                        }

                        if (urlIsAllowed)
                        {
                            ParseHtml(nextUrl, urlQueue, urlTable, errTable);
                        }

                        urlQueue.DeleteMessage(nextUrlMsg);
                        numUrl--;
                        numUrlsCrawled++;
                        Thread.Sleep(100);
                    }
                }
            }

            Thread.Sleep(1000);
        }

        private void ParseHtml(string url, CloudQueue urlQueue, CloudTable urlTable, CloudTable errorTable)
        {

            //if (!url.Contains(".htm"))
            //{
            //    if (!url.Contains(".jpg") && !url.Contains(".png"))
            //    {
            //        if (url.EndsWith(@"/"))
            //        {
            //            url += "index.html";
            //        }
            //        else
            //        {
            //            url += @"/index.html";
            //        }
            //    }
            //}
            try
            {
                var web = new HtmlWeb();
                var currDoc = web.Load(url);
                var urlNodes = currDoc.DocumentNode.Descendants("a")
                    .ToList();
                var urlPageTitle = currDoc.DocumentNode.Descendants("title")
                    .First()
                    .InnerText;
                var urlLastModNode = currDoc.DocumentNode.Descendants("meta")
                    .Select(y => y)
                    .Where(y => y.Attributes.Contains("name"))
                    .Where(y => y.Attributes["name"].Value == "lastmod")
                    .ToList();

                DateTime? urlLastMod = null;
                if (urlLastModNode.Count > 0)
                {
                    urlLastMod = DateTime.Parse(
                        urlLastModNode.First().Attributes["content"].Value);
                }


                foreach (var urlNode in urlNodes)
                {
                    if (urlNode.Attributes.Contains("href"))
                    {
                        var currHref = urlNode.Attributes["href"].Value;
                        bool validDateIfExists = true;
                        if (currHref.StartsWith(@"//"))
                        {
                            string domain = new Uri(url).Host;
                            currHref = @"http:" + currHref;
                        }
                        else if (currHref.StartsWith(@"/"))
                        {
                            string domain = new Uri(url).Host;
                            currHref = @"http://" + domain + currHref;
                        }
                        if (urlLastMod != null)
                        {
                            validDateIfExists = (urlLastMod >= DateTime.Now - TimeSpan.FromDays(62));
                        }
                        if (IsInProperDomain(currHref) 
                            && !_seenUrls.Contains(currHref)
                            && validDateIfExists)
                        {
                            CloudQueueMessage urlMsg = new CloudQueueMessage(currHref);
                            urlQueue.AddMessage(urlMsg);
                            _seenUrls.Add(currHref);
                            numUrl++;
                        }
                    }
                }

                FoundUrl finishedUrl = new FoundUrl(urlPageTitle, (urlLastMod != null ? urlLastMod.ToString() : "NULL"), url);
                TableOperation insertUrl = TableOperation.Insert(finishedUrl);
                urlTable.Execute(insertUrl);
                if (_lastTenUrls.Count == 10)
                {
                    _lastTenUrls.Pop();
                }
                _lastTenUrls.Push(url);
            }
            catch (Exception ex)
            {
                ErrorUrl errorUrl = new ErrorUrl(url, ex.ToString());
                TableOperation insertErrorUrl = TableOperation.Insert(errorUrl);
                errorTable.Execute(insertErrorUrl);
            }

        }

        private bool IsInProperDomain(string currHref)
        {
            if (currHref.Contains("cnn.com") || currHref.Contains("bleacherreport.com"))
            {

                return true;
            }
            else if (currHref.StartsWith(@"/") && !currHref.StartsWith(@"//"))
            {

                return true;
            }
            else // TODO: also bleacher report
            {
                return false;
            }

        }

        private void InitializeCrawl(string filePath, CloudQueue xmlQueue)
        {

            StreamReader input = new StreamReader(filePath);
            string currLine = "";
            string currUserAgent = "";
            List<string> sitemaps = new List<string>();
            while ((currLine = input.ReadLine()) != null)
            {
                if (currLine.StartsWith("Sitemap: "))
                {
                    sitemaps.Add(currLine.Substring(9));
                    _seenXmls.Add(currLine.Substring(9));
                    CloudQueueMessage msg = new CloudQueueMessage(currLine.Substring(9));
                    xmlQueue.AddMessage(msg);
                    numXml++;
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
