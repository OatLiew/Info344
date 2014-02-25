using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using System.IO;
using System.Web;
using System.Text;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
       public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
          CloudConfigurationManager.GetSetting("StorageConnectionString"));

       public static List<string> disallow_words = new List<string>();
        // Create the queue client
       public static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        // Retrieve a reference to a queue
       private static HashSet<string> visitedHash = new HashSet<string>();
       private int count;
       private static Queue<string> recentUrls_queue = new Queue<string>();
	
       public static CloudQueue queueURL;
       public static CloudQueue queueCommand;
       public static CloudQueue queueData;//data queue(last 10 crawled URLS)
       CloudQueue queueNo;//No of index,
       public static string not_allow_regex = @".*(/WEB-INF|/virtual|/test|"+
           "/QUICKNEWS|/Quickcast|/quickcast|/PV|/pr|/POLLSERVER|/pointroll|/pipeline|"+
           "/partners|/NOKIA|/NewsPass|/development|/cnnintl_adspaces|/cnnbeta|/cnn_adspaces|"+
           "/cnews|/cl|/browsers|/beta|/audioselect|/audio|/aol|/ads|/editionssi|/.element).*";

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole1 entry point called", "Information");
            while (true)
            {
                queueCommand = queueClient.GetQueueReference("command");
                queueCommand.CreateIfNotExists();
                CloudQueueMessage peekedMessage = null;
                try
                {
                    peekedMessage = queueCommand.PeekMessage();
                }
                catch
                {
                    peekedMessage = null;
                }

                if (peekedMessage != null)
                {
                    string inputs = peekedMessage.AsString;
                    if (inputs == "Start")
                    {
                        CloudQueueMessage retrievedMessage = queueCommand.GetMessage();
                        queueCommand.DeleteMessage(retrievedMessage);
                        CloudQueueMessage message = new CloudQueueMessage("Loading");
                        queueCommand.AddMessage(message);
                        readRobot();

                        retrievedMessage = queueCommand.GetMessage();
                        queueCommand.DeleteMessage(retrievedMessage);
                        CloudQueueMessage message2 = new CloudQueueMessage("Crawling");
                        queueCommand.AddMessage(message2);

                    }
                    else if (inputs == "Stop")
                    {
                        queueURL = queueClient.GetQueueReference("url");
                        queueCommand = queueClient.GetQueueReference("command");
                        queueData = queueClient.GetQueueReference("data");
                        queueNo = queueClient.GetQueueReference("num");
                        queueURL.Clear();
                        queueCommand.Clear();
                        queueData.Clear();
                        queueNo.Clear();
                        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                        CloudTable table = tableClient.GetTableReference("data");
                        table.DeleteIfExists();

                        try
                        {
                            CloudQueueMessage retrievedMessage = queueCommand.GetMessage();
                            retrievedMessage = queueCommand.GetMessage();
                            queueCommand.DeleteMessage(retrievedMessage);
                        }
                        catch { }
                        CloudQueueMessage message3 = new CloudQueueMessage("IDLE");
                        queueCommand.AddMessage(message3);
                    }
                    else if (inputs == "Crawling")
                    {
                        crawlURLS();
                    }
                }
                Thread.Sleep(500);
                Trace.TraceInformation("Working", "Information");
            }
        }
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public void readRobot()
        {
            string output = "";

            //get sitemap
            WebClient client = new WebClient();
            String htmlCode = client.DownloadString("http://www.cnn.com/robots.txt");

            using (StringReader reader = new StringReader(htmlCode))
            {
                string line;
                string urls;
                while ((line = reader.ReadLine()) != null)
                {
                    //Debug.WriteLine(line);
                    Match match_site = Regex.Match(line, @"^Sitemap:.*$",
                    RegexOptions.IgnoreCase);

                    // Here we check the Match instance.
                    if (match_site.Success)
                    {
                        output = Regex.Replace(line, "Sitemap: ", "");
                        //Debug.WriteLine("sitemap  "+output);
                        urls = client.DownloadString(output);
                        getHtml(urls);
                    }
                }
            }
        }

        protected void getHtml(string urls)
        {
            List<string> sitemaps_list = new List<string>();

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();

            // There are various options, set as needed
            htmlDoc.OptionFixNestedTags = true;

            HtmlAgilityPack.HtmlDocument htmlDocInner = new HtmlAgilityPack.HtmlDocument();

            // There are various options, set as needed
            htmlDocInner.OptionFixNestedTags = true;

            // filePath is a path to a file containing the html
            htmlDoc.LoadHtml(urls);

            WebClient client = new WebClient();

            queueURL = queueClient.GetQueueReference("url");
            // Create the queue if it doesn't already exist
            queueURL.CreateIfNotExists();

            foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//loc"))
            {
                string url_site = node.ChildNodes[0].InnerHtml;
                htmlDocInner.LoadHtml(client.DownloadString(url_site));
                try
                {
                    foreach (HtmlNode nodeInner in htmlDocInner.DocumentNode.SelectNodes("//loc"))
                    {
                        string site = nodeInner.ChildNodes[0].InnerHtml.ToString();
                        Match match = Regex.Match(site, not_allow_regex);
                        if (match.Success)
                        {
                            //do nothing
                        }
                        else
                        {
                            CloudQueueMessage msg = new CloudQueueMessage(site);
                            queueURL.AddMessage(msg);
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine("1layer");
                }
            }
        }

        public void crawlURLS()
        {
            WebClient client = new WebClient();
            queueURL = queueClient.GetQueueReference("url");
            queueURL.CreateIfNotExists();
            queueData = queueClient.GetQueueReference("data");
            queueData.CreateIfNotExists();
            queueNo = queueClient.GetQueueReference("num");
            queueNo.CreateIfNotExists();


            CloudQueueMessage retrievedMessage = queueURL.PeekMessage();
            string mainurl = retrievedMessage.AsString;
            string urls = client.DownloadString(mainurl);
            string date_lastModified = "";
            string title_str = "";
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionFixNestedTags = true;
            htmlDoc.LoadHtml(urls);

            string cnn_regex = ".*.cnn.com.*";

                if (htmlDoc.DocumentNode != null)
                {
                    HtmlAgilityPack.HtmlNode node = htmlDoc.DocumentNode.SelectSingleNode("//title");
                    if (node != null)
                    {
                        title_str = node.ChildNodes[0].InnerHtml;
                    }

                    HtmlAgilityPack.HtmlNode nodetitle = htmlDoc.DocumentNode.SelectSingleNode("//meta[@http-equiv='last-modified']");
                    if (nodetitle != null)
                    {
                        HtmlAttribute desc;
                        desc = nodetitle.Attributes["content"];
                        date_lastModified = desc.Value;
                    }

                    foreach (HtmlNode nodeLink in htmlDoc.DocumentNode.SelectNodes("//a"))
                    {
                        HtmlAttribute link;
                        string link_str = "";
                        try
                        {
                            link = nodeLink.Attributes["href"];
                            link_str = link.Value;
                        }
                        catch {
                            Debug.WriteLine("link problem");
                        }

                        //avoiding a href = /cnni/
                        if (link_str.Length > 1)
                        {
                            if ((link_str[0] == '/'))
                            {
                                link_str = mainurl + link_str;
                            }
                        }
                        Match match = Regex.Match(link_str, not_allow_regex);
                        Match match2 = Regex.Match(link_str, cnn_regex);

                        if (match.Success)
                        {
                                //do nothing
                        }
                        else if (match2.Success)
                        {
                                //if URL not in hashset
                            if (!visitedHash.Contains(link_str))
                            {
                                CloudQueueMessage msg = new CloudQueueMessage(link_str);
                                queueURL.AddMessage(msg);
                            }
                        } 
                     }

                }
                // Create the table client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Create the table if it doesn't exist.
                CloudTable table = tableClient.GetTableReference("data");
                table.CreateIfNotExists();

                string rowKey = HttpUtility.UrlEncode(mainurl);
                string partition_key = HttpUtility.UrlEncode("cnn.com");

                CEntity Url = new CEntity(partition_key, rowKey);
                Url.url = rowKey;
                Url.date = date_lastModified;
                Url.title = title_str;

                try{
                    TableOperation insertOperation = TableOperation.Insert(Url);
                    table.Execute(insertOperation);
                }
                catch {
                    Debug.WriteLine("repeated urls");
                }
                //add to hashset
                visitedHash.Add(mainurl);
                count++;//increase the count
                
                if (recentUrls_queue.Count > 9)
                {
                    recentUrls_queue.Dequeue();
                    recentUrls_queue.Enqueue(mainurl);
                }
                else
                {
                    recentUrls_queue.Enqueue(mainurl);
                }

                
                CloudQueueMessage message = new CloudQueueMessage(count.ToString());
                string recentUrl_str = ConvertStringArrayToString(recentUrls_queue.ToArray());
                CloudQueueMessage message2 = new CloudQueueMessage(recentUrl_str);
                queueNo.AddMessage(message);
                queueData.AddMessage(message2);

                queueData.FetchAttributes();
                int? cachedMessageCount = queueData.ApproximateMessageCount;
                if (cachedMessageCount > 1)
                {
                    CloudQueueMessage delete = queueData.GetMessage();
                    queueData.DeleteMessage(delete);
                }

                queueNo.FetchAttributes();
                int? cachedMessageCount2 = queueNo.ApproximateMessageCount;
                if (cachedMessageCount2 > 1)
                {
                    CloudQueueMessage delete2 = queueNo.GetMessage();
                    queueNo.DeleteMessage(delete2);
                }
                  

                //delete the visited url
                CloudQueueMessage deleteMsg = queueURL.GetMessage();
                queueURL.DeleteMessage(deleteMsg);
                
        }

        static string ConvertStringArrayToString(string[] array)
        {
            //
            // Concatenate all the elements into a StringBuilder.
            //
            StringBuilder builder = new StringBuilder();
            foreach (string value in array)
            {
                builder.Append(value);
                builder.Append(",");
            }
            return builder.ToString();
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
