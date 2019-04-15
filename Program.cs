/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace graph_pim_dotnet
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
        private const string SUBSCRIPTIONID = "b697fa44-1b50-43bd-8b36-e93333d56d25";

        //Get the service principal based on its appId:
        // az ad sp show --id http://myappname
        // Use the objectId as the GUID value for principalId
        private const string PRINCIPALID = "73084239-67a6-4f3d-b53e-c32a00656f33";

        //See https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles for built in roles
        //Contributor: b24988ac-6180-42a0-ab88-20f7382dd24c    
        private const string ROLEDEFINITIONID = "b24988ac-6180-42a0-ab88-20f7382dd24c";

        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync()
        {
            var result = await ProtectedApiCallHelper.GetAccessToken();

            //Can be subscription scope or resource group scope
            string scope = string.Format("subscriptions/{0}", SUBSCRIPTIONID);
            //string scope = "subscriptions/b697fa44-1b50-43bd-8b36-e93333d56d25/resourceGroups/myResourceGroup";

            if (result != null)
            {
                await ListRoleAssignments(scope, result.AccessToken);

                string roleAssignmentGuid = await GrantAccess(scope, PRINCIPALID, SUBSCRIPTIONID, ROLEDEFINITIONID, result.AccessToken);

                await DeleteAccess(scope, roleAssignmentGuid,result.AccessToken);
            }
        }

        private static async Task ListRoleAssignments(string scope, string accessToken)
        {
            var httpClient = new HttpClient();
            var apiCaller = new ProtectedApiCallHelper(httpClient);
            //await apiCaller.CallWebApiAndProcessResultASync("https://graph.microsoft.com/v1.0/users", result.AccessToken, Display);
            
            string url = string.Format("https://management.azure.com/{0}/providers/Microsoft.Authorization/roleAssignments?api-version=2015-07-01",scope);
            await apiCaller.CallWebApiAndProcessResultASync(url, accessToken, Display);            
        }

        private static async Task<string> GrantAccess(string scope, string principalId, string subscriptionId, string roleDefinitionId, string accessToken)
        {
                var httpClient = new HttpClient();
                
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                
                string roleAssignmentGuid = Guid.NewGuid().ToString();

                string url = string.Format("https://management.azure.com/{0}/providers/Microsoft.Authorization/roleAssignments/{1}?api-version=2015-07-01",scope, roleAssignmentGuid);
                RoleDefintion roleDefinion = new RoleDefintion();
                roleDefinion.properties = new Properties();
                roleDefinion.properties.roleDefinitionId = string.Format("/subscriptions/{0}/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c", subscriptionId);
                roleDefinion.properties.principalId = principalId;
                
                string body = JsonConvert.SerializeObject(roleDefinion);
                System.Diagnostics.Debug.WriteLine(body);
                await apiCaller.PutWebApiAndProcessResultASync(url, accessToken, Display, body);

                //Return the GUID of the new role assignment
                return roleAssignmentGuid;
        }

        private static async Task DeleteAccess(string scope, string roleAssignmentGuid, string accessToken)
        {
            var httpClient = new HttpClient();
            
            var apiCaller = new ProtectedApiCallHelper(httpClient);
            
            string url = string.Format("https://management.azure.com/{0}/providers/Microsoft.Authorization/roleAssignments/{1}?api-version=2015-07-01",scope, roleAssignmentGuid);
            
            await apiCaller.DeleteWebApiAndProcessResultASync(url, accessToken, Display);
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JObject result)
        {
            if(null != result)
            {
                foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
                {
                    Console.WriteLine($"{child.Name} = {child.Value}");
                }
            }
        }

    }
}