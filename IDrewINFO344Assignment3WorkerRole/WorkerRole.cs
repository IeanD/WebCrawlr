using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IDrewINFO344Assignment3ClassLibrary.Crawlrs;
using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;

namespace IDrewINFO344Assignment3WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CrawlrStorageManager _storageManager;
        private CrawlrStatusManager _statusManager;
        private CrawlrDataHelper _crawlrData;

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
            // Set up queues, tables, data helper, status helper
            InitializeCrawlrComponents();

            // Get the current cmd from the cmd table;
            // Re-execute cmd query periodically until current cmd exists
            while (_storageManager.GetCurrentCmd() == null)
            {
                Thread.Sleep(5000);
            }

            // If start cmd given, initialize download of robots.txt
            // and populate the xmlQueue and _disallowed list
            if (_storageManager.GetCurrentCmd() == "START")
            {
                /// FOR DEBUG
                //ParseHtml("https://www.cnn.com/news", urlQueue, urlTable, errTable);

                RobotsTxtCrawlr.CrawlRobotsTxt(ref _crawlrData, ref _storageManager);
            }

            // Recurring work
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                // Do work if current cmd is still "start"
                if (_storageManager.GetCurrentCmd() == "START")
                {
                    while (_crawlrData.NumXmlsQueued > 0 && _storageManager.GetCurrentCmd() == "START")
                    {
                        CloudQueueMessage nextXmlMsg = _storageManager.XmlQueue.GetMessage();
                        string nextXml = nextXmlMsg.AsString;

                        XmlCrawlr.CrawlXml(ref _crawlrData, ref _storageManager, nextXml);

                        _storageManager.XmlQueue.DeleteMessage(nextXmlMsg);
                        _crawlrData.NumXmlsQueued--;

                        _statusManager.UpdateCrawlrStatus(
                            "Loading",
                            _crawlrData,
                            _storageManager
                        );

                        Thread.Sleep(50);
                    }

                    while (_crawlrData.NumUrlsQueued > 0 && _storageManager.GetCurrentCmd() == "START")
                    {
                        CloudQueueMessage nextUrlMsg = _storageManager.UrlQueue.GetMessage();
                        string nextUrl = nextUrlMsg.AsString;

                        UrlCrawlr.CrawlUrl(ref _crawlrData, ref _storageManager, nextUrl);

                        _storageManager.UrlQueue.DeleteMessage(nextUrlMsg);
                        _crawlrData.NumUrlsQueued--;
                        _crawlrData.NumUrlsCrawled++;

                        _statusManager.UpdateCrawlrStatus(
                            "Crawling",
                            _crawlrData,
                            _storageManager
                        );

                        Thread.Sleep(50);
                    }
                }
                else if (_storageManager.GetCurrentCmd() == "CLEAR")
                {
                    _storageManager.ClearAll();
                    _statusManager.UpdateCrawlrStatus(
                        "Cleared",
                        _crawlrData,
                        _storageManager
                    );
                    try
                    {
                        while (_storageManager.GetCurrentCmd() == "CLEAR")
                        {
                            Thread.Sleep(10000);
                        }
                    }
                    finally
                    {
                        InitializeCrawlrComponents();
                    }
                }
                else
                {
                    Thread.Sleep(5000);
                }
            }

            Thread.Sleep(1000);
        }

        private void InitializeCrawlrComponents()
        {
            this._storageManager
                            = new CrawlrStorageManager(ConfigurationManager.AppSettings["StorageConnectionString"]);
            this._crawlrData = new CrawlrDataHelper();
            this._statusManager = new CrawlrStatusManager();
        }
    }
}
