using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CurrentWeather
{

    public class Function
    {
        private const string ApiKeyParameterName = "OpenWeatherApiKeyParameterName";
        private const string ZipCodeQueryParameter = "zipcode";

        private static readonly HttpClient HttpClient = new HttpClient();

        private string OpenWeatherApiKey { get; }

        public Function()
        {
            var apiKeyName = System.Environment.GetEnvironmentVariable(ApiKeyParameterName);
            if (string.IsNullOrEmpty(apiKeyName))
            {
                throw new InvalidOperationException($"Expected name of the Parameter Store entry containing your OpenWeather API key to be supplied in environment variable named {ApiKeyParameterName}");
            }

            var ssmClient = new AmazonSimpleSystemsManagementClient();
            var response = ssmClient.GetParameterAsync(new GetParameterRequest
            {
                Name = apiKeyName,
                WithDecryption = true
            }).Result;

            OpenWeatherApiKey = response.Parameter.Value;
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> Handler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
        {
            #region Sample url and response
            /*
                example call:
                http://api.openweathermap.org/data/2.5/weather?zip=98006&APPID=<apikey>

                example response:
                {
                    "coord": {
                        "lon":-122.1552,
                        "lat":47.5614
                    },
                    "weather":[
                        {
                            "id":501,
                            "main":"Rain",
                            "description":"moderate rain",
                            "icon":"10d"
                        }
                    ],
                    "base":"stations",
                    "main":{
                        "temp":281.16,
                        "feels_like":277.15,
                        "temp_min":279.82,
                        "temp_max":283.15,
                        "pressure":1009,
                        "humidity":71
                    },
                    "visibility":10000,
                    "wind":{
                        "speed":3.6,
                        "deg":240
                    },
                    "rain":{
                        "1h":2.05
                    },
                    "clouds":{
                        "all":90
                    },
                    "dt":1616633798,
                    "sys":{
                        "type":1,
                        "id":3417,
                        "country":"US",
                        "sunrise":1616594557,
                        "sunset":1616639206
                    },
                    "timezone":-25200,
                    "id":0,
                    "name":"Bellevue",
                    "cod":200
                }
            */
            #endregion

            var zipcode = request.QueryStringParameters[ZipCodeQueryParameter];
            if (string.IsNullOrEmpty(zipcode))
            {
                throw new ArgumentException("Expected to find 'zipcode' parameter in request's query string parameters");
            }

            Console.WriteLine($"Querying current weather for zipcode {zipcode}");
            var url = $"http://api.openweathermap.org/data/2.5/weather?zip={zipcode}&APPID={OpenWeatherApiKey}&units=imperial";

            var weatherData = await HttpClient.GetStringAsync(url);
            Console.WriteLine($"Received response ${weatherData}");

            return new APIGatewayHttpApiV2ProxyResponse
            {
                Body = weatherData, // JsonConvert.SerializeObject(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
