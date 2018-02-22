using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace IDrewINFO344Assignment3ClassLibrary.Storage.Entities
{
    /// <summary>
    ///     A TableEntity for a crawler URL table; stores how many URLs are
    ///     currently in the table.
    /// </summary>
    public class UrlTableCount : TableEntity
    {
        public int NumUrlsCrawled { get; set; }
        public int NumUrlsInTable { get; set; }

        public UrlTableCount(int numUrlsCrawled, int numUrlsInTable)
        {
            this.PartitionKey = "UrlTableCount";
            this.RowKey = "UrlTableCount";

            this.NumUrlsInTable = numUrlsInTable;
            this.NumUrlsCrawled = numUrlsCrawled;
        }

        public UrlTableCount()
        {

        }
    }
}
