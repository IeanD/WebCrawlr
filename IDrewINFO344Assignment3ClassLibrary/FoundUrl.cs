using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDrewINFO344Assignment3ClassLibrary
{
    public class FoundUrl : TableEntity
    {
        public string  PageTitle { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }

        public FoundUrl(string pageTitle, string date, string url)
        {
            this.PartitionKey = url.GetHashCode().ToString();   // make domain
            this.RowKey = Guid.NewGuid().ToString();            // make hash

            this.PageTitle = pageTitle;
            this.Date = date;
            this.Url = url;
        }

        public FoundUrl() { }
    }
}
