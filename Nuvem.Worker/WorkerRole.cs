using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;

namespace Nuvem.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("Nuvem.Worker is running");

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

            Trace.TraceInformation("Nuvem.Worker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Nuvem.Worker is stopping");

            // TODO: Any non-transferred logs will be lost if they aren't transferred manually like so (not yet complete)
            // Refer: http://msdn.microsoft.com/en-us/library/azure/gg433075.aspx
            //var diagnosticManager = new DeploymentDiagnosticManager(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"), RoleEnvironment.DeploymentId);
            //var roleInstanceDiagnosticManager = diagnosticManager.GetRoleInstanceDiagnosticManager("Nuvem.Worker", RoleEnvironment.CurrentRoleInstance.Id);
            //var dataBuffersToTransfer = DataBufferName.Logs;

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Nuvem.Worker has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
        }
    }
}
