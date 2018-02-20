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
        private CrawlrStorageManager _storageManager;
        private CrawlrStatusManager _statusManager;

        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public string StartCrawling(string robotsTxtUrl)
        {
            try
            {
                CloudTable cmdTable = new AzureTable(
                    ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrcmdtable")
                    .GetTable();

                CrawlrCmd cmd = new CrawlrCmd("START", robotsTxtUrl);

                TableOperation insert = TableOperation.InsertOrReplace(cmd);
                cmdTable.Execute(insert);

                return ("Beginning to crawl " + robotsTxtUrl);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.ToString();
            }
        }

        [WebMethod]
        public List<string> GetWorkerRoleStatus()
        {
            List<string> status = new List<string>();
            CloudTable statusTable = new AzureTable(
                ConfigurationManager.AppSettings["StorageConnectionString"], "crawlrstatustable")
                .GetTable();

            TableQuery<WorkerRoleStatus> statusQuery = new TableQuery<WorkerRoleStatus>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "WorkerRole Status")
                );

            WorkerRoleStatus result;

            foreach (var statusItem in statusTable.ExecuteQuery(statusQuery))
            {
                result = statusItem;
                status.Add("Current status: " + result.CurrStatus);
                status.Add("CPU Utilization: " + result.CpuUsed + "%");
                status.Add("RAM Available: " + result.RamAvailable + "MBytes");
                status.Add("Number of URLs Crawled: " + result.NumUrlsCrawled);
            }

            return status;
        }

        private void InitializeCrawlrComponents()
        {
            this._storageManager
                            = new CrawlrStorageManager(ConfigurationManager.AppSettings["StorageConnectionString"]);
            this._statusManager = new CrawlrStatusManager();
        }
    }
}
