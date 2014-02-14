using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Web.Hosting;
using System.Text.RegularExpressions;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]

    [ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        public static Trie dict = new Trie();
        private System.Diagnostics.PerformanceCounter ramCounter;

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Search(string Input)
        {
            List<string> result = dict.SearchPrefix(Input.ToLower());
            return new JavaScriptSerializer().Serialize(result.ToArray());
        }

        [WebMethod]
        public string filter()
        {
            string path1 = Server.MapPath("/file/wiki");
            string path2 = Server.MapPath("/file/wiki.txt");
            string regex = @"^([A-Za-z_]+)$";
            StringBuilder sb = new StringBuilder();
            string line;

            using (StreamReader reader = new StreamReader(path1, Encoding.ASCII))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, regex))
                    {
                        string correctString = line.Replace("_", " ");
                        sb.AppendLine(correctString.ToLower());
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(path2))
            {
                writer.Write(sb.ToString());
            }

            return "success";
        }

        [WebMethod]
        public string buildTree()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("testcon");
            String path = HostingEnvironment.ApplicationPhysicalPath + @"\wiki.txt";

            string tmp;

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        try
                        {
                            using (var fileStream = System.IO.File.OpenWrite(HostingEnvironment.ApplicationPhysicalPath + @"\wiki.txt"))
                            {
                                blob.DownloadToStream(fileStream);
                            }
                        }
                        catch
                        {
                            Debug.WriteLine("Download Error");
                        }
                    }
                }
            }

            ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            using (StreamReader sr = new StreamReader(path))
            {
                Double mem = getAvailableRAM();
                while (((tmp = sr.ReadLine()) != null) && (mem > 50))
                {
                    dict.Add(tmp.ToLower());
                    try
                    {
                        mem = getAvailableRAM();
                    }
                    catch (OutOfMemoryException e)
                    {
                        Debug.WriteLine("not enough memory");
                        break;
                    }
                }
            }
            return "sucess";
        }
        public double getAvailableRAM()
        {
            return ramCounter.NextValue();
        }
    }
}
