﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDrewINFO344Assignment3ClassLibrary
{
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
