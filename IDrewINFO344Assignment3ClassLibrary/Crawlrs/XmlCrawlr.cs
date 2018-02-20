using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Xml.Linq;

namespace IDrewINFO344Assignment3ClassLibrary.Crawlrs
{
    public class XmlCrawlr
    {
        public static void CrawlXml(ref CrawlrDataHelper data, ref CrawlrStorageManager storage, string xml)
        {
            if (data.ChkIfUriAllowed(xml))
            {
                if (xml.EndsWith(".xml"))
                {
                    XElement sitemap = XElement.Load(xml);
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
                            if (!data.QueuedXmls.Contains(currLocValue))
                            {
                                CloudQueueMessage x = new CloudQueueMessage(currLocValue);
                                storage.XmlQueue.AddMessage(x);
                                data.QueuedXmls.Add(currLocValue);
                                data.NumXmlsQueued++;
                            }
                        }
                        else
                        {
                            if (data.QueuedUrls.Contains(currLocValue))
                            {
                                CloudQueueMessage url = new CloudQueueMessage(currLocValue);
                                storage.UrlQueue.AddMessage(url);
                                data.QueuedUrls.Add(currLocValue);
                                data.NumUrlsQueued++;
                            }
                        }

                    }
                }
            }

        }
    }
}
