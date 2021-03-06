﻿using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace IDrewINFO344Assignment3ClassLibrary.Crawlrs
{
    /// <summary>
    ///     Helper class for crawling a robots.txt URL.
    ///     This class will load a robots.txt from a given URL, place all relevant sitemaps into queue,
    ///     and build a list of disallowed strings.
    /// </summary>
    public class RobotsTxtCrawlr
    {
        /// <summary>
        ///     Crawls a given robots.txt, adding all sitemaps to queue.
        /// </summary>
        /// <param name="data">
        ///     Crawler data helper. Ref.
        /// </param>
        /// <param name="storage">
        ///     Crawler azure storage helper. Ref.
        /// </param>
        public static void CrawlRobotsTxt(ref CrawlrDataHelper data, ref CrawlrStorageManager storage)
        {
            string url = storage.GetCurrentRobotsTxt();
            CrawlSpecificRobotsTxt(url, ref data, ref storage);

            // Include bleacherreport.com (formerly cnn.com/sports) if crawling cnn.com
            if (storage.GetCurrentRobotsTxt().Contains("cnn"))
            {
                CrawlSpecificRobotsTxt("http://www.bleacherreport.com/robots.txt", ref data, ref storage);
            }
        }

        private static void CrawlSpecificRobotsTxt(string url, ref CrawlrDataHelper data, ref CrawlrStorageManager storage)
        {
            string tempPath = Path.GetTempFileName();
            WebClient wc = new WebClient();
            wc.DownloadFile(url, tempPath);
            StreamReader input = new StreamReader(tempPath);
            string currLine = "";
            string currUserAgent = "";
            List<string> sitemaps = new List<string>();
            while ((currLine = input.ReadLine()) != null)
            {
                var splitLine = currLine.Split(' ');
                if (splitLine[0].ToLower() == "sitemap:")
                {
                    bool pass = false;
                    if (url.Contains("bleacherreport"))
                    {
                        if (splitLine[1].Contains("nba"))
                        {
                            pass = true;
                        }
                    }
                    else
                    {
                        pass = true;
                    }
                    if (pass)
                    {
                        sitemaps.Add(splitLine[1]);
                        data.QueuedXmls.Add(splitLine[1]);
                        CloudQueueMessage msg = new CloudQueueMessage(splitLine[1]);
                        storage.XmlQueue.AddMessage(msg);
                        data.NumXmlsQueued++;

                    }
                }
                else if (splitLine[0].ToLower() == "user-agent:")
                {
                    currUserAgent = splitLine[1];
                }
                else if (splitLine[0].ToLower() == "disallow:" && currUserAgent == "*")
                {
                    data.DisallowedStrings.Add(splitLine[1]);
                }

            }
        }
    }
}
