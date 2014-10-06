using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Nuvem.DeploymentManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Press Enter to continue");
            Console.ReadLine();

            //Deploy().Wait();
            DeleteDeployment().Wait();
            //ChangeConfiguration().Wait();
            //DeleteRoleInstance().Wait();

            Console.WriteLine("Done");
            Console.Read();
        }

        private static async Task DeleteDeployment()
        {
            var credentials = new CertificateCloudCredentials("989b78f7-9b3a-4143-9061-33215a8aa09b", new X509Certificate2(@"C:\Charles-Azure.cer"));

            var cloudServiceName = "nuvem-test-service";
            var deploymentName = cloudServiceName + "Prod";

            var computeManagementClient = new ComputeManagementClient(credentials);

            var result = await computeManagementClient.Deployments.DeleteByNameAsync(cloudServiceName, deploymentName, true);

            var blah = result;
        }

        private static async Task ChangeConfiguration()
        {
            var credentials = new CertificateCloudCredentials("989b78f7-9b3a-4143-9061-33215a8aa09b", new X509Certificate2(@"C:\Charles-Azure.cer"));

            var configFilePath = @"C:\oss\nuvem\src\TestCloudService\bin\Release\app.publish\ServiceConfiguration.Cloud-2.cscfg";

            var cloudServiceName = "nuvem-test-service";
            var deploymentName = cloudServiceName + "Prod";

            var computeManagementClient = new ComputeManagementClient(credentials);

            var result = await computeManagementClient.Deployments.ChangeConfigurationByNameAsync(cloudServiceName, deploymentName,
                        new DeploymentChangeConfigurationParameters(File.ReadAllText(configFilePath)));

            var blah = result;
        }

        private static async Task DeleteRoleInstance()
        {
            var credentials = new CertificateCloudCredentials("989b78f7-9b3a-4143-9061-33215a8aa09b", new X509Certificate2(@"C:\Charles-Azure.cer"));

            var cloudServiceName = "nuvem-test-service";
            var deploymentName = cloudServiceName + "Prod";

            var computeManagementClient = new ComputeManagementClient(credentials);

            var result = await computeManagementClient.Deployments.DeleteRoleInstanceByDeploymentNameAsync(cloudServiceName, deploymentName,
                new DeploymentDeleteRoleInstanceParameters()
                {
                    Name = new[] {"Nuvem.Worker_IN_0"}
                });

            var blah = result;
        }

        private static async Task Deploy()
        {
            var credentials = new CertificateCloudCredentials("989b78f7-9b3a-4143-9061-33215a8aa09b", new X509Certificate2(@"C:\Charles-Azure.cer"));

            var packageFilePath = @"C:\oss\nuvem\src\TestCloudService\bin\Release\app.publish\TestCloudService.cspkg";
            var configFilePath = @"C:\oss\nuvem\src\TestCloudService\bin\Release\app.publish\ServiceConfiguration.Cloud.cscfg";

            var cloudServiceName = "nuvem-test-service";
            var deploymentName = cloudServiceName + "Prod";

            var computeManagementClient = new ComputeManagementClient(credentials);
            var storageManagementClient = new StorageManagementClient(credentials);

            // Does this only have to be created once?
            //CreateCloudService(computeManagementClient, cloudServiceName, "Southeast Asia").Wait();

            var storageConnectionString = await GetStorageAccountConnectionString(storageManagementClient, "nuvem1");
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobs = storageAccount.CreateCloudBlobClient();
            var container = blobs.GetContainerReference("deployments");
            await container.CreateIfNotExistsAsync();

            // TODO: Obviously publicly accessible storage is not what we want for this
            await container.SetPermissionsAsync(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Container
                });
            var blob = container.GetBlockBlobReference(Path.GetFileName(packageFilePath));
            await blob.UploadFromFileAsync(packageFilePath, FileMode.Open);

            var result= await computeManagementClient.Deployments.CreateAsync(cloudServiceName, DeploymentSlot.Production,
                    new DeploymentCreateParameters
                    {
                        Name = deploymentName,
                        Label = deploymentName + DateTime.UtcNow.ToString("O"),
                        PackageUri = blob.Uri,
                        Configuration = File.ReadAllText(configFilePath),
                        StartDeployment = true
                    });

            var blah = result;
        }

        private static async Task<string> GetStorageAccountConnectionString(StorageManagementClient storageManagementClient, string accountName)
        {
            var keys = await storageManagementClient.StorageAccounts.GetKeysAsync(accountName);

            return string.Format(CultureInfo.InvariantCulture,
                "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountName, keys.SecondaryKey);
        }

        private static async Task CreateCloudService(ComputeManagementClient computeManagementClient, string serviceName, string region)
        {
            await computeManagementClient.HostedServices.CreateAsync(new HostedServiceCreateParameters()
                {
                    ServiceName = serviceName,
                    Location = region
                });
        }
    }
}
