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
    /// <summary>
    ///     Worker role for webCrawlr, a URL crawler mostly focused on crawling cnn.com.
    ///     The role will start by reading a given robots.txt URL, putting all found sitemaps into
    ///     an Azure Queue, then parsing those sitemaps to build a queue of URLs to crawl. It will
    ///     then parse each URL one at a time, adding any found URLs to the queue, then adding the
    ///     parsed URLs page title and URL to an Azure Table for search. webCrawlr will do its best
    ///     to avoid sitemaps/URLs older than two months.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        // Helper for communicating with Azure storage & keeping track of seen URLs/XMLs
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
            while (_storageManager.GetCurrentCmd() != "START")
            {
                Thread.Sleep(5000);
            }

            // If start cmd given, initialize download of robots.txt
            // and populate the xmlQueue and _disallowed list
            if (_storageManager.GetCurrentCmd() == "START")
            {
                Startup();
            }

            // Recurring work
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                // Do work if current cmd is still "start"
                if (_storageManager.GetCurrentCmd() == "START")
                {
                    // Process all XMLs (sitemaps) found
                    while (_crawlrData.NumXmlsQueued > 0 && _storageManager.GetCurrentCmd() == "START")
                    {
                        CloudQueueMessage nextXmlMsg = _storageManager.XmlQueue.GetMessage();
                        string nextXml = nextXmlMsg.AsString;

                        XmlCrawlr.CrawlXml(ref _crawlrData, ref _storageManager, nextXml);

                        _storageManager.XmlQueue.DeleteMessage(nextXmlMsg);
                        _crawlrData.NumXmlsQueued--;

                        // Update worker role status
                        _statusManager.UpdateCrawlrStatus(
                            "Loading",
                            _crawlrData,
                            _storageManager
                        );
                        _statusManager.UpdateQueueSize(_storageManager, _crawlrData.NumXmlsQueued, _crawlrData.NumUrlsQueued);

                        Thread.Sleep(50);
                    }

                    // Process all URLs in queue
                    while (_crawlrData.NumUrlsQueued > 0 && _storageManager.GetCurrentCmd() == "START")
                    {
                        CloudQueueMessage nextUrlMsg = _storageManager.UrlQueue.GetMessage();
                        string nextUrl = nextUrlMsg.AsString;

                        UrlCrawlr.CrawlUrl(ref _crawlrData, ref _storageManager, nextUrl);

                        _storageManager.UrlQueue.DeleteMessage(nextUrlMsg);
                        _crawlrData.NumUrlsQueued--;

                        // Update worker role status
                        _statusManager.UpdateCrawlrStatus(
                            "Crawling",
                            _crawlrData,
                            _storageManager
                        );
                        _statusManager.UpdateQueueSize(_storageManager, _crawlrData.NumXmlsQueued, _crawlrData.NumUrlsQueued);

                        Thread.Sleep(50);
                    }
                }
                else if (_storageManager.GetCurrentCmd() == "CLEAR")
                {
                    // If the "CLEAR" command is found, clear all queues and tables.
                    _storageManager.ClearAll();
                    _statusManager.UpdateCrawlrStatus(
                        "CLEAR",
                        _crawlrData,
                        _storageManager
                    );
                    // Give Azure time to delete tables.
                    Thread.Sleep(20000);

                    try
                    {
                        // Idle while waiting for next command.
                        while (_storageManager.GetCurrentCmd() == "CLEAR")
                        {
                            Thread.Sleep(10000);
                        }
                    }
                    finally
                    {
                        // Reinitialize worker role.
                        InitializeCrawlrComponents();
                        Startup();
                    }
                }
                else
                {
                    // Idle worker role (for unimplemented 'pause' functionality).
                    _statusManager.UpdateCrawlrStatus(
                        "Idle",
                        _crawlrData,
                        _storageManager
                    );

                    Thread.Sleep(5000);
                }
            }

            Thread.Sleep(1000);
        }

        // Perform the initial robots.txt crawl.
        private void Startup()
        {
            _statusManager.UpdateCrawlrStatus(
                "Initializing",
                _crawlrData,
                _storageManager
            );
            RobotsTxtCrawlr.CrawlRobotsTxt(ref _crawlrData, ref _storageManager);
            _statusManager.UpdateQueueSize(_storageManager, _crawlrData.NumXmlsQueued, _crawlrData.NumUrlsQueued);
        }

        // Refresh storage and helpers.
        private void InitializeCrawlrComponents()
        {
            this._storageManager
                            = new CrawlrStorageManager(ConfigurationManager.AppSettings["StorageConnectionString"]);
            this._crawlrData = new CrawlrDataHelper();
            this._statusManager = new CrawlrStatusManager();
        }
    }
}
