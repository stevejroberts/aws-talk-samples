using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using Amazon.ECS;
using Amazon.ECS.Model;

namespace MiniMeConsole.Pages
{
    public class ECSModel : PageModel
    {

        private ILogger<ECSModel> Logger { get; }

        public Dictionary<string, List<Cluster>> Clusters { get; }
        public Dictionary<string, List<Service>> Services { get; }

        private List<Amazon.RegionEndpoint> Regions = new List<Amazon.RegionEndpoint>
        {
            Amazon.RegionEndpoint.USEast1,
            Amazon.RegionEndpoint.USEast2,
            Amazon.RegionEndpoint.USWest1,
            Amazon.RegionEndpoint.USWest2
        };

        public ECSModel(ILogger<ECSModel> logger)
        {
            Logger = logger;
            Clusters = new Dictionary<string, List<Cluster>>();
        }

        public async System.Threading.Tasks.Task OnGet()
        {
            Clusters.Clear();

            foreach (var region in Regions)
            {
                var client = new AmazonECSClient(region);

                try
                {
                    string nextToken = null;
                    do
                    {
                        var listResponse = await client.ListClustersAsync(new ListClustersRequest
                        {
                            MaxResults = 100, // max that DescribeClusters will yield at a time
                            NextToken = nextToken
                        });

                        nextToken = listResponse.NextToken;

                        var clustersResponse = await client.DescribeClustersAsync(new DescribeClustersRequest
                        {
                            Clusters = listResponse.ClusterArns
                        });

                        Clusters.Add(region.DisplayName, clustersResponse.Clusters);

                    } while (!string.IsNullOrEmpty(nextToken));
                }
                catch (AmazonECSException e)
                {
                    Logger.LogError(e, $"Failed to access ECS API in region {region.SystemName}");
                }
                finally
                {
                }
            }
        }
    }
}
