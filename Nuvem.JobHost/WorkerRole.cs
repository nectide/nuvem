using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace Nuvem.JobHost
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("Nuvem.JobHost is running");

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

            Trace.TraceInformation("Nuvem.JobHost has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Nuvem.JobHost is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Nuvem.JobHost has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => ProcessJobQueue(cancellationToken), cancellationToken);
        }

        private void ProcessJobQueue(CancellationToken cancellationToken)
        {
            var currentInterval = 0;
            var maxInterval = 15;

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageAccountConnectionString"]);
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference("nuvem-job-queue");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = queue.GetMessage();
                    if (message != null)
                    {
                        currentInterval = 0;
                        if (message.DequeueCount <= 3)
                        {
                            var item = message.AsString;
                            // Start processing item

                            Trace.TraceInformation("{0} -> Nuvem.JobHost is processing item: {1}", RoleEnvironment.CurrentRoleInstance.Id, item);

                            Thread.Sleep(TimeSpan.FromSeconds(15));

                            // End Processing item
                            queue.DeleteMessage(message);
                        }
                        else
                        {
                            if (currentInterval < maxInterval)
                            {
                                currentInterval++;
                            }
                            Trace.TraceInformation("[{0}] waiting for {1} seconds", DateTime.Now.TimeOfDay, currentInterval);
                            Thread.Sleep(TimeSpan.FromSeconds(currentInterval));
                        }
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
