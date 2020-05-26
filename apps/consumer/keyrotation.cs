using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace consumer
{
    public class keyrotation
    {
        private IConfigurationRoot _config;
        public keyrotation(IConfiguration configuration) {
            this._config = configuration as IConfigurationRoot;
        }
        [FunctionName("keyrotation")]
        public void Run([ServiceBusTrigger("mytopic", "mysubscription", Connection = "SERVICEBUSCONNECTION")]string mySbMsg, ILogger log)
        {
            if (_config is null) {
                log.LogWarning("Config is null");
            }
            else {
                //whenever a message is posted to the key rotation topic we need to refresh our cached access key.
                log.LogInformation($"Key updated to version {mySbMsg ?? "null"}");
                this._config.Reload();
                log.LogInformation("Reloaded app config.");
            }
        }
    }
}
