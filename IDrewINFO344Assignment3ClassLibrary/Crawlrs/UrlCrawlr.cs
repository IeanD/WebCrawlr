using HtmlAgilityPack;
using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using IDrewINFO344Assignment3ClassLibrary.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;

namespace IDrewINFO344Assignment3ClassLibrary.Crawlrs
{
    public class UrlCrawlr
    {


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
                                && !data.QueuedUrls.Contains(currHref)
                                && validDateIfExists)
                            {
                                CloudQueueMessage urlMsg = new CloudQueueMessage(currHref);
                                storage.UrlQueue.AddMessage(urlMsg);
                                data.QueuedUrls.Add(currHref);
                                data.NumUrlsQueued++;
                            }
                        }
                    }

                    FoundUrl finishedUrl = new FoundUrl(urlPageTitle, (urlLastMod != null ? urlLastMod.ToString() : "NULL"), url);
                    TableOperation insertUrl = TableOperation.Insert(finishedUrl);
                    storage.UrlTable.Execute(insertUrl);
                    if (data.LastTenUrls.Count == 10)
                    {
                        data.LastTenUrls.Pop();
                    }
                    data.LastTenUrls.Push(url);
                }
                catch (Exception ex)
                {
                    ErrorUrl errorUrl = new ErrorUrl(url, ex.ToString());
                    TableOperation insertErrorUrl = TableOperation.Insert(errorUrl);
                    storage.ErrorTable.Execute(insertErrorUrl);
                }

            }
        }


        private static bool IsInProperDomain(string currHref)
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
    }
}
