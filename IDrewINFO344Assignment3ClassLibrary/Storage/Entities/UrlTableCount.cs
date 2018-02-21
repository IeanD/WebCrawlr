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
        public int NumUrlsInTable { get; set; }

        public UrlTableCount(int numUrlsInTable)
        {
            this.PartitionKey = "UrlTableCount";
            this.RowKey = "UrlTableCount";

            this.NumUrlsInTable = numUrlsInTable;
        }

        public UrlTableCount()
        {

        }
    }
}
