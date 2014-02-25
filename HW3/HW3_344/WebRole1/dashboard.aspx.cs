using HtmlAgilityPack;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    public partial class dashboard : System.Web.UI.Page
    {      
        public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
        
        public static List<string> disallow_words = new List<string>();
        // Create the queue client
        public static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
         // Retrieve a reference to a queue
        public static CloudQueue queueURL;

        public static CloudQueue queueCommand;

        public static CloudQueue queueData;
        public static CloudQueue queueNo;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // Create the queue client
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queueCommand = queueClient.GetQueueReference("command");
            queueCommand.CreateIfNotExists();

            queueURL = queueClient.GetQueueReference("url");
            queueURL.CreateIfNotExists();

            queueData = queueClient.GetQueueReference("data");
            queueData.CreateIfNotExists();

            queueNo = queueClient.GetQueueReference("num");
            queueNo.CreateIfNotExists();
        }
        protected void start_btn_Click(object sender, EventArgs e)
        {

            string str = "Start";
            CloudQueue queueCommand = queueClient.GetQueueReference("command");
            queueCommand.CreateIfNotExists();
            CloudQueueMessage deleteMsg = queueCommand.GetMessage();
            try
            {
                queueCommand.DeleteMessage(deleteMsg);
            }
            catch { }
                
            CloudQueueMessage message = new CloudQueueMessage(str);
            queueCommand.AddMessage(message);
            
        }

        protected void stop_btn_Click(object sender, EventArgs e)
        {

            string str = "Stop";
            CloudQueue queueCommand = queueClient.GetQueueReference("command");
            queueCommand.CreateIfNotExists();
            CloudQueueMessage deleteMsg = queueCommand.GetMessage();
            try
            {
                queueCommand.DeleteMessage(deleteMsg);
            }
            catch { }

            CloudQueueMessage message = new CloudQueueMessage(str);
            queueCommand.AddMessage(message);
            title_lbl.Text = "title";
            queueSize_lbl.Text = "0";
            recent_lbl.Text = "0";
            crawled_lbl.Text = "0";
            url_txtbox.Text = "";
        }

        protected void update_btn_Click(object sender, EventArgs e)
        {
            // get memory available
            PerformanceCounter ramCounter;
            PerformanceCounter cpuCounter;
            ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            Double mem = ramCounter.NextValue();
            mem_lbl.Text = mem.ToString();

            // get cpu available
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            dynamic cpu = cpuCounter.NextValue() + "%";
            cpu_lbl.Text = cpu;

            string inputs = "";
            queueURL.FetchAttributes();
            // Retrieve the cached approximate message count.
            int? cachedMessageCount = queueURL.ApproximateMessageCount;
            queueSize_lbl.Text = Server.HtmlEncode(cachedMessageCount.ToString());

            queueCommand = queueClient.GetQueueReference("command");
            queueCommand.CreateIfNotExists();

            CloudQueueMessage peekedMessage = null;

            try{
                peekedMessage = queueCommand.PeekMessage();
                inputs = peekedMessage.AsString;
            }
            catch { 
            
            }
            state_lbl.Text = Server.HtmlEncode(inputs);

            //clear all
            if (inputs == "IDLE")
            {
                title_lbl.Text = "title";
                queueSize_lbl.Text = "0";
                recent_lbl.Text = "0";
                crawled_lbl.Text = "0";
                url_txtbox.Text = ""; 
            }

            queueData = queueClient.GetQueueReference("data");
            queueData.CreateIfNotExists();
            queueNo = queueClient.GetQueueReference("num");
            queueNo.CreateIfNotExists();

            CloudQueueMessage No;
            CloudQueueMessage recent;
            try{
                No = queueNo.PeekMessage();
                crawled_lbl.Text = Server.HtmlEncode(No.AsString);

                recent = queueData.PeekMessage();
                string[] words = recent.AsString.Split(',');
                StringBuilder builder = new StringBuilder();
	            foreach (string word in words)
	            {
                    builder.AppendLine(word);
                    builder.Append(Environment.NewLine);
	            }

                recent_lbl.Text = (builder.ToString());
            }
            catch {
                recent = null;
            }
        }

        protected void ok_btn_Click(object sender, EventArgs e)
        {
            string URL = url_txtbox.Text;

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("data");
            TableOperation retrieveOperation = TableOperation.Retrieve<CEntity>("cnn.com", HttpUtility.UrlEncode(URL));
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
                title_lbl.Text = (((CEntity)retrievedResult.Result).title);
            else
                title_lbl.Text = ("The title could not be retrieved.");
        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {


        }

        public class CEntity : TableEntity
        {
            public CEntity(string par, string row)
            {
                this.PartitionKey = par;
                this.RowKey = row;
            }
            public CEntity() { }

            public string url { get; set; }
            public string date { get; set; }
            public string title { get; set; }
        }

    }

}