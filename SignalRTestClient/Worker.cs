using Acdm.Common.ResourcePublishTypes;
using Acdm.PubSub;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SignalRTestClient
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;

        public Worker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var authTokenUrl = _configuration["authUrl"];
            var tenantId = _configuration["tanent_Id"];
            var clientId = _configuration["client_id"];
            var clientSecret = _configuration["client_secret"];
            var apiScope = _configuration["scope"];
            var grantType = _configuration["grant_type"];

            var authUrl = $"{authTokenUrl}{tenantId}/oauth2/v2.0/token";

            using var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, authUrl)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId!),
                    new KeyValuePair<string, string>("client_secret", clientSecret!),
                    new KeyValuePair<string, string>("scope", apiScope!),
                    new KeyValuePair<string, string>("grant_type", grantType!)
                })
            };

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tokenObj = System.Text.Json.JsonDocument.Parse(json);
                if (tokenObj.RootElement.TryGetProperty("access_token", out var accessToken))
                {
                    return accessToken.GetString();
                }
            }
            else
            {
                Console.WriteLine("Failed to get token");
            }

            return null;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hubUrl = _configuration["hubUrl"];

            var accessToken = await GetAccessTokenAsync();

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl!, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                })
                .Build();

            connection.On<string>("JoinGroupSuccess", group =>
            {
                Console.WriteLine($"Successfully joined group: {group}");
            });

            connection.On<string>("JoinGroupError", error =>
            {
                Console.WriteLine($"Failed to join group: {error}");
            });      

            foreach (DeiceResourcePublishTypes deIceEvent in Enum.GetValues(typeof(DeiceResourcePublishTypes)))
            {
                connection.On<object>(deIceEvent.ToString(), audit =>
                {
                    Console.WriteLine("--------------------------------------------------------------------------------");
                    Console.WriteLine($"Received {deIceEvent} event:");
                    Console.WriteLine(audit);
                });
            }

            foreach (ResourcePublishTypes acdmEvent in Enum.GetValues(typeof(ResourcePublishTypes)))
            {
                connection.On<object>(acdmEvent.ToString(), audit =>
                {
                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------------------------");
                    Console.WriteLine($"Received {acdmEvent} event:");
                    Console.WriteLine(audit);
                });
            }

            await connection.StartAsync(stoppingToken);
            Console.WriteLine("Connected!");

            await connection.InvokeAsync("JoinGroup", "ZRH", cancellationToken: stoppingToken);

            // Keep running until stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(100, stoppingToken);
            }

            await connection.StopAsync();
        }
    }
}