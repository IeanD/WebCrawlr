using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace IDrewINFO344Assignment3ClassLibrary.Storage.Entities
{
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
