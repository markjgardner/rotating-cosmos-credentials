using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.WindowsAzure.Storage.Table;

namespace rotator
{
    public class Row:TableEntity
    {
        public string Text { get; set; }
    }
    public static class rotatekeys
    {
        [FunctionName("rotatekeys")]
        public static async void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, 
            [Table("MyTable", "MyPartition", "cosmoskey")]Row keyname,
            [Table("MyTable")]CloudTable table,
            [ServiceBus("mytopic", Connection = "SBCONNECTION")]IAsyncCollector<string> messages,
            ILogger log)
        {
            log.LogInformation($"Begin rotation. Active key is {keyname.Text}");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            
            var creds = SdkContext.AzureCredentialsFactory.FromSystemAssignedManagedServiceIdentity(
              Microsoft.Azure.Management.ResourceManager.Fluent.Authentication.MSIResourceType.AppService, 
              AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Authenticate(creds).WithDefaultSubscription();
            var cosmosaccount = azure.CosmosDBAccounts.GetById(Environment.GetEnvironmentVariable("COSMOSID"));
            
            //update keyvault with alternate key value
            var activekey = (keyname.Text == "primary" ? cosmosaccount.ListKeys().SecondaryMasterKey : cosmosaccount.ListKeys().PrimaryMasterKey);
            log.LogInformation($"Updating connection string to use alternate key");
            var newconnstring = $"AccountEndpoint=https://mjgtest1.documents.azure.com:443/;AccountKey={activekey};";
            var updatedkey = await kv.SetSecretAsync(Environment.GetEnvironmentVariable("KEYVAULTURI"),"cosmosconnection", newconnstring);

            //publish message to SB
            await messages.AddAsync(updatedkey.SecretIdentifier.Version);
            log.LogInformation($"Notified subscribers of new version {updatedkey.SecretIdentifier.Version}");

            //rotate cosmoskey
            cosmosaccount.RegenerateKey(keyname.Text);
            log.LogInformation($"Regenerated {keyname.Text} key");

            //flip the key to rotate next time
            keyname.Text = keyname.Text == "primary" ? "secondary" : "primary";
            log.LogInformation($"Next run will regenerate the {keyname.Text} key");
            await table.ExecuteAsync(TableOperation.Replace(keyname));
        }
    }
}
