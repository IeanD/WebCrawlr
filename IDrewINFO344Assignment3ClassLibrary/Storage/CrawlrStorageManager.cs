using IDrewINFO344Assignment3ClassLibrary.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace IDrewINFO344Assignment3ClassLibrary.Storage
{
    public class CrawlrStorageManager
    {
        private string _connectionString;
        private TableQuery<CrawlrCmd> _cmdQuery;
        private CloudTable _cmdTable;
        public CloudQueue XmlQueue { get; private set; }
        public CloudQueue UrlQueue { get; private set; }
        public CloudTable ErrorTable { get; private set; }
        public CloudTable UrlTable { get; private set; }
        public CloudTable StatusTable { get; private set; }

        public CrawlrStorageManager(string connString)
        {
            this._connectionString = connString;

            this._cmdQuery = new TableQuery<CrawlrCmd>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "workerRoleCmd"))
                .Take(1);

            this._cmdTable = new AzureTable(
                _connectionString, "crawlrcmdtable")
                .GetTable();

            this.XmlQueue = new AzureQueue(
                _connectionString, "crawlrxmlqueue")
                .GetQueue();

            this.UrlQueue = new AzureQueue(
                _connectionString, "crawlrurlqueue")
                .GetQueue();

            this.ErrorTable = new AzureTable(
                _connectionString, "crawlrerrortable")
                .GetTable();

            this.UrlTable = new AzureTable(
                _connectionString, "crawlrurltable")
                .GetTable();

            this.StatusTable = new AzureTable(
                _connectionString, "crawlrstatustable")
                .GetTable();
        }

        public void IssueCmd(string cmd, string robotsTxt)
        {

        }

        public string GetCurrentCmd()
        {
            var currentCmd = _cmdTable.ExecuteQuery(_cmdQuery);

            if (currentCmd.Any())
            {

                return currentCmd.First().Cmd;
            }
            else
            {

                return null;
            }
        }

        public void ClearAll()
        {
            XmlQueue.Clear();
            UrlQueue.Clear();
            UrlTable.Delete();
            ErrorTable.Delete();
        }
        
        //public bool CmdExists()
        //{
        //    var cmd = CmdTable.ExecuteQuery(_cmdQuery);

        //    return cmd.Any();
        //}
    }
}
