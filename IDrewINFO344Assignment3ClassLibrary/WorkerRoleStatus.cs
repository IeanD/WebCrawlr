using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDrewINFO344Assignment3ClassLibrary
{
    public class WorkerRoleStatus
    {
        public string CurrStatus { get; set; }
        public int CpuUsed { get; set; }
        public int RamAvailable { get; set; }
        public int NumUrlsCrawled { get; set; }
        public List<string> LastTenUrls { get; set; }

        public WorkerRoleStatus(string currStatus, int cpuUsed, int ramAvailable, int numUrlsCrawled, List<string> lastTenUrls)
        {
            this.CurrStatus = currStatus;
            this.CpuUsed = cpuUsed;
            this.RamAvailable = ramAvailable;
            this.NumUrlsCrawled = numUrlsCrawled;
            this.LastTenUrls = lastTenUrls;
        }

        public override string ToString()
        {
            string result = "";
            result += CurrStatus + "|";
            result += CpuUsed.ToString() + "|";
            result += RamAvailable.ToString() + "|";
            result += NumUrlsCrawled.ToString() + "|";
            if (LastTenUrls.Count < 10)
            {
                foreach (var url in LastTenUrls)
                {
                    result += url;
                    if (LastTenUrls.IndexOf(url) != LastTenUrls.Count -1)
                    {
                        result += ",";
                    }
                }
            }
            else
            {
                for (int currIndex = LastTenUrls.Count - 11; currIndex < LastTenUrls.Count -1; currIndex++)
                {
                    result += LastTenUrls[currIndex];
                    if (currIndex != LastTenUrls.Count - 1)
                    {
                        result += ",";
                    }
                }
            }

            return result;
        }
    }
}
