using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace IDrewINFO344Assignment3ClassLibrary.Storage.Entities
{
    /// <summary>
    ///     A TableEntity for a crawler error table; stores an error's domain, URI 
    ///     and exception.ToString().
    /// </summary>
    public class ErrorUrl : TableEntity
    {
        public string Url { get; set; }
        public string Exception { get; set; }

        public ErrorUrl(string url, string exception)
        {
            Uri uri = new Uri(url);
            this.PartitionKey = uri.Host;
            this.RowKey = url.GetHashCode().ToString();

            this.Url = url;
            this.Exception = exception;
        }

        public ErrorUrl() { }
    }
}
