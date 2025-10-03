using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using static System.Net.WebRequestMethods;

namespace SignalRTestClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration from appsettings.json or environment variables
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)                 
                .Build();

            // Retrieve the hub URL from configuration
            //var hubUrl = configuration["deiceResourceHubURL"];

            var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI3MDk2NzFkNy1jNTBlLTRkNDgtYmIyZi01YmIxN2M3MzUwMTYiLCJvaWQiOiI3MDk2NzFkNy1jNTBlLTRkNDgtYmIyZi01YmIxN2M3MzUwMTYiLCJqdGkiOiJjNjdiYzNhMC1lNmUxLTQ1ZmEtOWMxYi00OWE1ZTJlNjUxYTQiLCJwcm9kdWN0IjoiQWlyc2lkZU9wdGltaXplciIsIm5hbWUiOiJzaGl2YW0uYmFuc2FsQHNpdGEuYWVybyIsInJvbGVzIjpbIkFkbWluIiwiU3RhbmRSZWFkIiwiU3RhbmRXcml0ZSIsIlNpdGU6WlJIIl0sIm5iZiI6MTc1OTQ4MDk1MSwiZXhwIjoxNzU5NDgyNjkxLCJpc3MiOiJodHRwczovL3pyaC5kZXYudGVzdC5zaXRhLXRhbS5hZXJvL2FpcnNpZGVvcHRpbWl6ZXIiLCJhdWQiOiJlYzljYTc2Yy04Yzg2LTRlODctYmE1Zi00MzI4MDRjYTVjYzEifQ.PXznUCf7b940Nkdzx7K9nj4aZ04aHj3_UZRD0JkaCHA";
            var hubUrl = "https://localhost:7252/deiceresourcehub";

            if (string.IsNullOrEmpty(hubUrl))
            {
                Console.WriteLine("Hub URL is not configured. Please set it in appsettings.json or environment variables.");
                return;
            }

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(accessToken);
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

            connection.On<object>("TruckImpairment", audit =>
            {
                Console.WriteLine("Received TruckImpairment event:");
                Console.WriteLine(audit);
            });

            connection.On<object>("LaneImpairment", audit =>
            {
                Console.WriteLine("Received LaneImpairment event:");
                Console.WriteLine(audit);
            });

            await connection.StartAsync();
            Console.WriteLine("Connected!");

            await connection.InvokeAsync("JoinGroup", "ZRH");

            Console.WriteLine("JoinGroup invoked. Press any key to exit.");
            Console.ReadKey();
            await connection.StopAsync();
        }
    }                   
}
