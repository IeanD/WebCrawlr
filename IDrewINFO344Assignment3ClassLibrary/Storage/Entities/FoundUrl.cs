using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace IDrewINFO344Assignment3ClassLibrary.Storage.Entities
{
    /// <summary>
    ///     A TableEntity for a crawler URL table; stores a URL's domain, page title, date (if
    ///     found) and URL itself. RowKey is a hash of the URL for quick look up.
    /// </summary>
    public class FoundUrl : TableEntity
    {
        public string  PageTitle { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }

        public FoundUrl(string pageTitle, string date, string url)
        {
            Uri uri = new Uri(url);
            this.PartitionKey = uri.Host;
            this.RowKey = url.GetHashCode().ToString();

            this.PageTitle = pageTitle;
            this.Date = date;
            this.Url = url;
        }

        public FoundUrl() { }
    }
}
