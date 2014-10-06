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
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;

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

            DiagnosticMonitorConfiguration config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Information;
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);

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
            await ProcessJobQueueAsync(cancellationToken);
        }

        private async Task ProcessJobQueueAsync(CancellationToken cancellationToken)
        {
            var currentInterval = 0;
            var maxInterval = 30;

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageAccountConnectionString"));
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference("nuvem-job-queue");

            await queue.CreateIfNotExistsAsync();

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

                            Trace.TraceInformation("Nuvem.JobHost is processing item: {0}", item);

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
