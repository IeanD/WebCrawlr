using System.Collections.Generic;

namespace IDrewINFO344Assignment3ClassLibrary.Helpers
{
    public class CrawlrDataHelper
    {
        public List<string> DisallowedStrings { get; private set; }
        public HashSet<string> QueuedXmls { get; private set; }
        public HashSet<string> QueuedUrls { get; private set; }
        public HashSet<string> AddedUrls { get; private set; }
        public Stack<string> LastTenUrls { get; private set; }
        public int NumXmlsQueued { get; set; }
        public int NumUrlsQueued { get; set; }
        public int NumUrlsCrawled { get; set; }

        public CrawlrDataHelper()
        {
            this.DisallowedStrings = new List<string>();
            this.QueuedXmls = new HashSet<string>();
            this.QueuedUrls = new HashSet<string>();
            this.AddedUrls = new HashSet<string>();
            this.LastTenUrls = new Stack<string>(10);
            this.NumXmlsQueued = 0;
            this.NumUrlsQueued = 0;
            this.NumUrlsCrawled = 0;
        }

        public bool ChkIfUriAllowed(string uri)
        {
            foreach (string s in DisallowedStrings)
            {
                if (uri.Contains(s))
                {

                    return false;
                }
            }

            return true;
        }
    }
}
