﻿using HtmlAgilityPack;
using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using IDrewINFO344Assignment3ClassLibrary.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDrewINFO344Assignment3ClassLibrary.Crawlrs
{
    /// <summary>
    ///     Helper class for webCrawlr.
    ///     This class can crawl a given URL, placing all found URLs into queue and placing the
    ///     given URLS page title and URL into Azure table storage for later queries.
    /// </summary>
    public class UrlCrawlr
    {
        /// <summary>
        ///     Crawls a given URL, queueing all found URLs and storing information about
        ///     the given URL for later querying.
        /// </summary>
        /// <param name="data">
        ///     Crawler data helper. Ref.
        /// </param>
        /// <param name="storage">
        ///     Crawler azure storage helper. Ref.
        /// </param>
        /// <param name="url">
        ///     The given URL to crawl.
        /// </param>
        public static void CrawlUrl(ref CrawlrDataHelper data, ref CrawlrStorageManager storage, string url)
        {
            if (data.ChkIfUriAllowed(url))
            {
                ///*  Unsure if necessary.  */
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
                        .Where(y => y.Attributes["name"].Value == "pubdate")
                        .ToList();

                    DateTime? urlLastMod = null;
                    if (urlLastModNode.Count > 0)
                    {
                        urlLastMod = DateTime.Parse(
                            urlLastModNode.First().Attributes["content"].Value);
                    }

                    List<string> urlsToQueue = new List<string>();

                    foreach (var urlNode in urlNodes)
                    {
                        if (urlNode.Attributes.Contains("href"))
                        {
                            urlsToQueue.Add(urlNode.Attributes["href"].Value);
                        }
                    }

                    foreach (string newUrl in urlsToQueue)
                    {
                        ChkAndAddUrl(newUrl, url, urlLastMod, ref data, ref storage);
                    }

                    if (!data.AddedUrls.Contains(url))
                    {
                        data.AddedUrls.Add(url);
                        data.NumUrlsIndexed++;
                    }
                    data.NumUrlsCrawled++;
                    FoundUrl finishedUrl = new FoundUrl(urlPageTitle, (urlLastMod != null ? urlLastMod.ToString() : "NULL"), url);
                    UrlTableCount newCount = new UrlTableCount(data.NumUrlsCrawled, data.NumUrlsIndexed);
                    TableOperation insertUrl = TableOperation.InsertOrReplace(finishedUrl);
                    TableOperation insertCount = TableOperation.InsertOrReplace(newCount);
                    storage.UrlTable.Execute(insertUrl);
                    storage.UrlTable.Execute(insertCount);
                    if (data.LastTenUrls.Count == 10)
                    {
                        data.LastTenUrls.Dequeue();
                    }
                    data.LastTenUrls.Enqueue(url);
                }
                catch (Exception ex)
                {
                    ErrorUrl errorUrl = new ErrorUrl(url, ex.ToString());
                    TableOperation insertErrorUrl = TableOperation.InsertOrReplace(errorUrl);
                    storage.ErrorTable.Execute(insertErrorUrl);
                }

            }
        }

        private static bool IsInProperDomain(string currHref)
        {
            if (currHref.Contains("cnn.com") || currHref.Contains("bleacherreport.com/nba"))
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

        /// <summary>
        ///     Checks to see if a given URL has already been queued/parsed; if not, adds to queue.
        /// </summary>
        /// <param name="currHref">
        ///     URL to check.
        /// </param>
        /// <param name="currUri">
        ///     Domain space of URL.
        /// </param>
        /// <param name="urlLastMod">
        ///     URLs lastmod/published date, if known. Nullable.
        /// </param>
        /// <param name="data">
        ///     Crawler data helper. Ref.
        /// </param>
        /// <param name="storage">
        ///     Crawler azure storage helper. Ref.
        /// </param>
        public static void ChkAndAddUrl(string currHref, string currUri, 
            DateTime? urlLastMod, ref CrawlrDataHelper data, ref CrawlrStorageManager storage)
        {
            bool validDateIfExists = true;
            string domain = new Uri(currUri).Host;
            if (currHref.StartsWith(@"//"))
            {
                currHref = @"http:" + currHref;
            }
            else if (currHref.StartsWith(@"/"))
            {
                currHref = @"http://" + domain + currHref;
            }
            if (urlLastMod != null)
            {
                validDateIfExists = (urlLastMod >= DateTime.Now - TimeSpan.FromDays(62));
            }
            if (IsInProperDomain(currHref)
                && !data.QueuedUrls.Contains(currHref)
                && !data.AddedUrls.Contains(currHref)
                && validDateIfExists)
            {
                CloudQueueMessage urlMsg = new CloudQueueMessage(currHref);
                storage.UrlQueue.AddMessage(urlMsg);
                data.QueuedUrls.Add(currHref);
                data.NumUrlsQueued++;
            }
        }
    }
}
