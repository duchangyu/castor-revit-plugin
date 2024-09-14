using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class WebServiceBroker
{
    private static readonly HttpClient client = new HttpClient();

    static WebServiceBroker()
    {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public static async Task<string> SendGetRequestAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode(); // Throws if not successful
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> SendPostRequestAsync(string url, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data,options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode(); // Throws if not successful
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
            return null;
        }
    }
}
