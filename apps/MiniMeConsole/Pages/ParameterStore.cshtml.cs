using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Amazon.SimpleSystemsManagement.Model;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace MiniMeConsole.Pages
{
    public class ParameterStoreModel : PageModel
    {
        private ILogger<ParameterStoreModel> Logger { get; }

        private List<Amazon.RegionEndpoint> Regions = new List<Amazon.RegionEndpoint>
        {
            Amazon.RegionEndpoint.USEast1,
            Amazon.RegionEndpoint.USEast2,
            Amazon.RegionEndpoint.USWest1,
            Amazon.RegionEndpoint.USWest2
        };

        public Dictionary<string, Dictionary<string, string>> Parameters { get; }

        public ParameterStoreModel(ILogger<ParameterStoreModel> logger)
        {
            Logger = logger;

            Parameters = new Dictionary<string, Dictionary<string, string>>();
        }

        public async Task OnGet()
        {
            Parameters.Clear();

            foreach (var region in Regions)
            {
                var client = new AmazonSimpleSystemsManagementClient(region);
                var regionParameters = new Dictionary<string, string>();

                try
                {
                    // example of a paginated api
                    string nextPageToken = null;
                    do
                    {
                        var response = await client.DescribeParametersAsync(new DescribeParametersRequest
                        {
                            NextToken = nextPageToken
                        });

                        foreach (var parameter in response.Parameters)
                        {
                            if (parameter.Type == ParameterType.SecureString)
                            {
                                regionParameters.Add(parameter.Name, "*****Shhh, it's a secret!*****");
                            }
                            else
                            {
                                var valueResponse = await client.GetParameterAsync(new GetParameterRequest
                                {
                                    Name = parameter.Name
                                });
                                regionParameters.Add(parameter.Name, valueResponse.Parameter.Value);
                            }
                        }

                    } while (!string.IsNullOrEmpty(nextPageToken));
                }
                catch (AmazonSimpleSystemsManagementException e)
                {
                    Logger.LogError(e, "Failed to access SSM API");
                }
                finally
                {
                    Parameters.Add(region.DisplayName, regionParameters);
                }
            }
        }
    }
}