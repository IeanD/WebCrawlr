using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDrewINFO344Assignment3ClassLibrary
{
    public class WorkerRoleStatus : TableEntity
    {
        public string CurrStatus { get; set; }
        public int CpuUsed { get; set; }
        public int RamAvailable { get; set; }
        public int NumUrlsCrawled { get; set; }
        public string LastTenUrls { get; set; }

        public WorkerRoleStatus(string currStatus, int cpuUsed, int ramAvailable, int numUrlsCrawled, List<string> lastTenUrls)
        {
            this.PartitionKey = "WorkerRole Status";
            this.RowKey = "WorkerRole Status";

            this.CurrStatus = currStatus;
            this.CpuUsed = cpuUsed;
            this.RamAvailable = ramAvailable;
            this.NumUrlsCrawled = numUrlsCrawled;
            this.LastTenUrls = LastTenUrlsToString(lastTenUrls);            
        }

        public string LastTenUrlsToString(List<string> urls)
        {
            string result = "";

            if (urls.Count < 10)
            {
                foreach (var url in urls)
                {
                    result += url;
                    if (urls.IndexOf(url) != urls.Count -1)
                    {
                        result += ",";
                    }
                }
            }
            else
            {
                for (int currIndex = urls.Count - 11; currIndex < urls.Count -1; currIndex++)
                {
                    result += LastTenUrls[currIndex];
                    if (currIndex != urls.Count - 1)
                    {
                        result += ",";
                    }
                }
            }

            return result;
        }
    }
}
