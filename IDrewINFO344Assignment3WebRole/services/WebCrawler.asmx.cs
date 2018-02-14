using IDrewINFO344Assignment3ClassLibrary;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
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
        private List<string> _disallowed { get; set; }

        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [WebMethod]
        public string StartCrawling(string robotsTxtUrl)
        {
            List<string> sitemaps = new List<string>();
            List<string> disallowed = new List<string>();

            string tempPath = System.IO.Path.GetTempFileName();
            try
            {
                WebClient wc = new WebClient();
                wc.DownloadFile(robotsTxtUrl, tempPath);

                //AzureQueue urlQueue = new AzureQueue(
                //    ConfigurationManager.AppSettings["StorageConnectionString"], "urlqueue");
                //AzureQueue cmdQueue = new AzureQueue(
                //    ConfigurationManager.AppSettings["StorageConnectionString"], "cmdqueue");
                AzureTable cmdTable = new AzureTable(
                    ConfigurationManager.AppSettings["StorageConnectionString"], "cmdtable");


                WorkerRoleCmd cmd = new WorkerRoleCmd()
                StreamReader input = new StreamReader(tempPath);
                string currLine = "";
                string currUserAgent = "";

                while ((currLine = input.ReadLine()) != null)
                {
                    if (currLine.StartsWith("Sitemap: "))
                    {
                        sitemaps.Add(currLine.Substring(9));
                        CloudQueueMessage msg = new CloudQueueMessage(currLine.Substring(9));
                        urlQueue.GetQueue().AddMessage(msg);
                    }
                    else if (currLine.StartsWith("User-agent: "))
                    {
                        currUserAgent = currLine.Substring(12);
                    }
                    else if (currLine.StartsWith("Disallow: "))
                    {
                        DisallowUrl disallowUrl = new DisallowUrl(currUserAgent, website, currLine.Substring(10));
                        TableOperation insertOperation = TableOperation.Insert(disallowUrl);
                        disallowTable.GetTable().Execute(insertOperation);

                        disallowed.Add(currLine.Substring(10));
                    }
                }

                _disallowed = disallowed;
            }
            catch (Exception e)
            {

                return e.ToString();
            }

            return ("Beginning to crawl " + website);
        }

        private void AddToUrlQueue(List<string> sitemaps)
        {
            throw new NotImplementedException();
        }
    }
}
