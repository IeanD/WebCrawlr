using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace IDrewINFO344Assignment3ClassLibrary
{
    public class DisallowUrl : TableEntity
    {
        public string UserAgent { get; set; }
        public string Domain { get; set; }
        public string Url { get; set; }

        public DisallowUrl(string userAgent, string domain, string url)
        {
            this.PartitionKey = url.GetHashCode().ToString();
            this.RowKey = Guid.NewGuid().ToString();

            this.UserAgent = userAgent;
            this.Domain = domain;
            this.Url = url;
        }

        public DisallowUrl() { }
    }
}
