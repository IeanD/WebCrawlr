using IDrewINFO344Assignment3ClassLibrary;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using IDrewINFO344Assignment3ClassLibrary.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Script.Services;
using System.Web.Services;

namespace IDrewINFO344Assignment3WebRole.services
{
    /// <summary>
    /// Summary description for WebCrawler
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [ScriptService]
    public class WebCrawler : WebService
    {
        private CrawlrStorageManager _storageManager
            = new CrawlrStorageManager(ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static string _robotsTxtUrl;

        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public string StartCrawling(string robotsTxtUrl)
        {
            try
            {
                InitializeCrawlrComponents();
                _robotsTxtUrl = robotsTxtUrl;
                InitializeCrawlrComponents();
                _storageManager.IssueCmd("START", robotsTxtUrl);

                return ("Beginning to crawl " + robotsTxtUrl);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.ToString();
            }
        }

        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public string PauseCrawling()
        {
            try
            {
                _storageManager.IssueCmd("PAUSE", _robotsTxtUrl);

                return ("Pausing crawl of " + _robotsTxtUrl);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.ToString();
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public string StopCrawling()
        {
            try
            {
                _storageManager.IssueCmd("CLEAR", _robotsTxtUrl);

                return ("Stopping and clearing crawl of " + _robotsTxtUrl);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.ToString();
            }
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public List<string> GetErrors()
        {
            List<string> results = new List<string>();
            CloudTable errorTable = _storageManager.ErrorTable;

            TableQuery<ErrorUrl> errorQuery = new TableQuery<ErrorUrl>();
            try
            {
                foreach (var error in errorTable.ExecuteQuery(errorQuery))
                {
                    results.Add(error.Url + " | " + error.Exception);
                }
            }
            catch (Exception ex)
            {

                results.Add("WebCrawler.asmx (if you just cleared/stopped the crawler, " +
                    "this is normal) | " + ex.ToString());
            }

            return results;
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public List<string> GetLastTenUrls()
        {
            List<string> result = new List<string>();
            CloudTable statusTable = _storageManager.StatusTable;

            TableQuery<WorkerRoleStatus> statusQuery = new TableQuery<WorkerRoleStatus>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "WorkerRole Status")
                );

            string unparsedString = "";

            foreach (var statusItem in statusTable.ExecuteQuery(statusQuery))
            {
                unparsedString = statusItem.LastTenUrls;
            }

            string[] parsedString = unparsedString.Split(',');

            foreach (string s in parsedString)
            {
                result.Add(s);
            }
            result.Reverse();

            return result;
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public int GetNumCrawledUrls()
        {
            CloudTable urlTable = _storageManager.UrlTable;

            TableQuery<UrlTableCount> urlNumberQuery = new TableQuery<UrlTableCount>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "UrlTableCount")
                );

            int result = -1;
            foreach (var statusItem in urlTable.ExecuteQuery(urlNumberQuery))
            {
                result = statusItem.NumUrlsInTable;
            }

            return result;
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public List<int> GetQueueSizes()
        {
            CloudTable statusTable = _storageManager.StatusTable;

            TableQuery<CrawlrQueueSize> statusQuery = new TableQuery<CrawlrQueueSize>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Queue Size")
                );

            List<int> result = new List<int>();
            foreach (var statusItem in statusTable.ExecuteQuery(statusQuery))
            {
                result.Add(statusItem.XmlQueueSize);
                result.Add(statusItem.UrlQueueSize);
            }

            return result;
        }

        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public List<string> GetWorkerRoleStatus()
        {
            List<string> status = new List<string>();
            CloudTable statusTable = _storageManager.StatusTable;

            TableQuery<WorkerRoleStatus> statusQuery = new TableQuery<WorkerRoleStatus>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "WorkerRole Status")
                );

            WorkerRoleStatus result;

            foreach (var statusItem in statusTable.ExecuteQuery(statusQuery))
            {
                result = statusItem;
                string currStatus = result.CurrStatus;
                if (currStatus == "CLEAR")
                {
                    currStatus = "Idle (Cleared)";
                }
                status.Add("Current status: " + currStatus);
                status.Add("CPU Utilization: " + result.CpuUsed + "%");
                status.Add("RAM Available: " + result.RamAvailable + "MBytes");
                status.Add("Number of URLs Crawled: " + result.NumUrlsCrawled);
            }

            return status;
        }

        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public List<string> SearchForUrlTitle(string url)
        {
            CloudTable urlTable = _storageManager.UrlTable;

            TableQuery<FoundUrl> urlTitleQuery = new TableQuery<FoundUrl>()
                .Where(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, url.GetHashCode().ToString())
                );

            List<string> result = new List<string>();
            foreach (var statusItem in urlTable.ExecuteQuery(urlTitleQuery))
            {
                result.Add(statusItem.PageTitle);
            }

            return result;
        }

        private void InitializeCrawlrComponents()
        {
            this._storageManager
                    = new CrawlrStorageManager(ConfigurationManager.AppSettings["StorageConnectionString"]);
        }
    }
}
