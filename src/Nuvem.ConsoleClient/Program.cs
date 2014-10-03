using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Nuvem.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Enter item to add to queue:");
                var item = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(item))
                    break;
                AddItemToQueue(item);
                Console.WriteLine("added");
            }
        }

        private static void AddItemToQueue(string item)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccountConnectionString"]);

            var client = storageAccount.CreateCloudQueueClient();

            var queue = client.GetQueueReference("nuvem-job-queue");

            queue.AddMessage(new CloudQueueMessage(item));
        }
    }
}
