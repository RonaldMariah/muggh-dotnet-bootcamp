using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using Sku = Microsoft.Azure.Management.Storage.Models.Sku;

namespace MUGGH_DOTNET_BOOTCAMP
{
    class StorageDetail
    {
        public string Name { get; set; }

        public string Location { get; set; }
    }

    public static class Functions
    {
        [FunctionName("Functions")]
        public static async Task Run([QueueTrigger("items", Connection = "QueueConnection")]string myQueueItem, ILogger log)
        {
            var detail = JsonConvert.DeserializeObject<StorageDetail>(myQueueItem);

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            var credentialId = (await keyVaultClient.GetSecretAsync("https://muggh-vault.vault.azure.net/secrets/credentialId")).Value;
            var credentialSecret = (await keyVaultClient.GetSecretAsync("https://muggh-vault.vault.azure.net/secrets/credentialSecret")).Value;

            var clientCredential = new ClientCredential(credentialId, credentialSecret);

            var authenticationContext =
                new AuthenticationContext("https://login.windows.net/c0ea6916-3598-4bac-aa1b-a8c92c985ada");

            var accessToken = (await authenticationContext.AcquireTokenAsync("https://management.azure.com/", clientCredential)).AccessToken;

            var storageManagementClient = new StorageManagementClient(new TokenCredentials(accessToken))
            {
                SubscriptionId = "fedac4e9-b5e9-4a01-b68c-d3375b1c0ca5"
            };

            await storageManagementClient.StorageAccounts.CreateAsync("storage-accounts", detail.Name,
                new StorageAccountCreateParameters
                {
                    Sku = new Sku("Standard_LRS"),
                    Kind = Kind.StorageV2,
                    Location = detail.Location,
                });
        }
    }
}
