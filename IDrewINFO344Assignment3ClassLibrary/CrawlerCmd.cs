using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDrewINFO344Assignment3ClassLibrary
{
    class CrawlerCmd : TableEntity
    {
        public string Cmd { get; set; }
        public string Domain { get; set; }

        public CrawlerCmd(string cmd, string domain)
        {
            this.PartitionKey = "webCrawlr";
            this.RowKey = "workerRoleCmd";

            this.Cmd = cmd;
            this.Domain = domain;
        }
    }
}
