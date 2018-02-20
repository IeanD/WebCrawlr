using IDrewINFO344Assignment3ClassLibrary.Helpers;
using IDrewINFO344Assignment3ClassLibrary.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using System.Diagnostics;

namespace IDrewINFO344Assignment3ClassLibrary.Storage
{
    public class CrawlrStatusManager
    {
        private PerformanceCounter _memoryFree;
        private PerformanceCounter _cpuTime;

        public CrawlrStatusManager()
        {
            this._memoryFree = new PerformanceCounter("Memory", "Available MBytes");
            this._cpuTime = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        public void UpdateCrawlrStatus(string currentStatus, CrawlrDataHelper data, CrawlrStorageManager storage)
        {
            WorkerRoleStatus currStatusEntity;
            if (currentStatus == "CLEARED")
            {
                currStatusEntity = new WorkerRoleStatus(
                    currentStatus,
                    (int)_cpuTime.NextValue(),
                    (int)_memoryFree.NextValue(),
                    0,
                    new System.Collections.Generic.Stack<string>()
                );
            }
            else
            {
                currStatusEntity = new WorkerRoleStatus(
                    currentStatus,
                    (int)_cpuTime.NextValue(),
                    (int)_memoryFree.NextValue(),
                    data.NumUrlsCrawled,
                    data.LastTenUrls
                );
            }

            TableOperation insertStatus = TableOperation.InsertOrReplace(currStatusEntity);
            storage.StatusTable.Execute(insertStatus);
        }
    }
}


