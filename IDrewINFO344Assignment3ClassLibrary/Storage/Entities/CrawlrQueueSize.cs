﻿using Microsoft.WindowsAzure.Storage.Table;

namespace IDrewINFO344Assignment3ClassLibrary.Storage.Entities
{
    public class CrawlrQueueSize : TableEntity
    {
        public int XmlQueueSize { get; set; }
        public int UrlQueueSize { get; set; }

        public CrawlrQueueSize(int xmlQueueSize, int urlQueueSize)
        {
            this.PartitionKey = "Queue Size";
            this.RowKey = "Queue Size";

            this.XmlQueueSize = xmlQueueSize;
            this.UrlQueueSize = urlQueueSize;
        }

        public CrawlrQueueSize()
        {

        }
    }
}
