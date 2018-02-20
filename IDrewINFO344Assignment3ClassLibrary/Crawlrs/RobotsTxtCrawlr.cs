using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace IDrewINFO344Assignment3ClassLibrary.Crawlrs
{
    public class RobotsTxtCrawlr
    {
        public static void CrawlRobotsTxt(ref CrawlrDataHelper data, ref CrawlrStorageManager storage)
        {
            string tempPath = Path.GetTempFileName();
            WebClient wc = new WebClient();
            wc.DownloadFile(storage.GetCurrentCmd(), tempPath);
            StreamReader input = new StreamReader(tempPath);
            string currLine = "";
            string currUserAgent = "";
            List<string> sitemaps = new List<string>();
            while ((currLine = input.ReadLine()) != null)
            {
                if (currLine.StartsWith("Sitemap: "))
                {
                    sitemaps.Add(currLine.Substring(9));
                    data.QueuedXmls.Add(currLine.Substring(9));
                    CloudQueueMessage msg = new CloudQueueMessage(currLine.Substring(9));
                    storage.XmlQueue.AddMessage(msg);
                    data.NumXmlsQueued++;
                }
                else if (currLine.StartsWith("User-agent: "))
                {
                    currUserAgent = currLine.Substring(12);
                }
                else if (currLine.StartsWith("Disallow: ") && currUserAgent == "*")
                {
                    data.DisallowedStrings.Add(currLine.Substring(10));
                }
            }
        }
    }
}
