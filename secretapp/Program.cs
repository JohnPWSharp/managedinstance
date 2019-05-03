using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace secretapp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Step 1: Get a token from the local (URI) Managed Service Identity endpoint, which in turn fetches it from Azure AD
            //var token = GetToken();

            // Step 2: Fetch the secret value from your key vault
            //System.Console.WriteLine(FetchSecretValueFromKeyVault(token));
            
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            GetSecretFromKeyVault(azureServiceTokenProvider).Wait();
        }

        static string GetToken()
        {
            WebRequest request = WebRequest.Create("http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net");
            request.Headers.Add("Metadata", "true");
            WebResponse response = request.GetResponse();
            return ParseWebResponse(response, "access_token");
        }

        static string FetchSecretValueFromKeyVault(string token)
        {
            WebRequest kvRequest = WebRequest.Create("https://jpwskeyvault.vault.azure.net/secrets/jabberwocky?api-version=2016-10-01");
            kvRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse kvResponse = kvRequest.GetResponse();
            return ParseWebResponse(kvResponse, "value");
        }

        private static string ParseWebResponse(WebResponse response, string tokenName)
        {
            string token = String.Empty;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();

                JObject joResponse = JObject.Parse(responseString);
                JValue ojObject = (JValue)joResponse[tokenName];
                token = ojObject.Value.ToString();
            }
            return token;
        }
        
        private static async Task GetSecretFromKeyVault(AzureServiceTokenProvider azureServiceTokenProvider)
        {
            KeyVaultClient keyVaultClient =
                new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            var keyVaultName = "jpwskeyvault";
            var secret = "jabberwocky";

            try
            {
                var secret = await keyVaultClient
                    .GetSecretAsync($"https://{keyVaultName}.vault.azure.net/secrets/{secret}")
                    .ConfigureAwait(false);

                Console.WriteLine($"Secret: {secret.Value}");

            }
            catch (Exception exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
        }
    }
}
