using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace MiniMeConsole.Pages
{
    public class EC2Model : PageModel
    {
        private ILogger<EC2Model> Logger { get; }

        public Dictionary<string, List<Instance>> Instances { get; }

        private List<Amazon.RegionEndpoint> Regions = new List<Amazon.RegionEndpoint>
        {
            Amazon.RegionEndpoint.USEast1,
            Amazon.RegionEndpoint.USEast2,
            Amazon.RegionEndpoint.USWest1,
            Amazon.RegionEndpoint.USWest2
        };

        public EC2Model(ILogger<EC2Model> logger)
        {
            Logger = logger;
            Instances = new Dictionary<string, List<Instance>>();
        }

        public async Task OnGet()
        {
            Instances.Clear();

            foreach (var region in Regions)
            {
                var client = new AmazonEC2Client(region);
                var instances = new List<Instance>();

                try
                {
                    var response = await client.DescribeInstancesAsync();

                    // EC2 returns the instances grouped by reservation
                    if (response.Reservations.Count != 0)
                    {
                        foreach (var reservation in response.Reservations)
                        {
                            instances.AddRange(reservation.Instances);
                        }
                    }
                }
                catch (AmazonEC2Exception e)
                {
                    Logger.LogError(e, $"Failed to access EC2 API in region {region.SystemName}");
                }
                finally
                {
                    Instances.Add(region.DisplayName, instances);
                }
            }
        }
    }
}