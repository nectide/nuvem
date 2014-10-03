using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace Nuvem.AutoScaler
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("Nuvem.AutoScaler is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Nuvem.AutoScaler has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Nuvem.AutoScaler is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Nuvem.AutoScaler has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => ProcessJobQueue(cancellationToken), cancellationToken);
        }

        private void ProcessJobQueue(CancellationToken cancellationToken)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccountConnectionString"]);
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference("nuvem-job-queue");

            var managementClient = new ComputeManagementClient(new CertificateCloudCredentials("989b78f7-9b3a-4143-9061-33215a8aa09b", new X509Certificate2(@"C:\Charles-Azure.cer")));
            var isDeployed = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var queueCount = queue.ApproximateMessageCount;

                    if (queueCount > 0 && !isDeployed)
                    {
                        isDeployed = true;

                        //--

                        //--
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
