using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CastorPlugin.Services.Contracts;
using Serilog;

public class WebServiceBroker
{
    private static readonly HttpClient client = new HttpClient();
    private static ISettingsService _settingsService;
    private static string _accessToken;

    public static void Initialize(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"Initializing WebServiceBroker with ApiUrl: {_settingsService.ApiUrl}");

        if (string.IsNullOrEmpty(_settingsService.ApiUrl))
        {
            throw new InvalidOperationException("ApiUrl is not set in the settings.");
        }

        client.BaseAddress = new Uri(_settingsService.ApiUrl);

        // Restore token if exists
        if (!string.IsNullOrEmpty(_settingsService.AccessToken))
        {
            SetAccessToken(_settingsService.AccessToken);
        }
    }

    public static void SetAccessToken(string token)
    {
        _accessToken = token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static void ClearAccessToken()
    {
        _accessToken = null;
        client.DefaultRequestHeaders.Authorization = null;
    }

    public static bool HasAccessToken => !string.IsNullOrEmpty(_accessToken);

    public static async Task<string> SendGetRequestAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> SendPostRequestAsync(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);
            Log.Information($"POST {client.BaseAddress}{endpoint} - Body: {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync();
            Log.Information($"POST response status: {response.StatusCode}, body: {responseBody}");
            response.EnsureSuccessStatusCode();
            return responseBody;
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"HTTP Request error: {ex.Message}");
            return null;
        }
    }

    public static string GetFullUrl(string endpoint)
    {
        return new Uri(client.BaseAddress, endpoint).ToString();
    }
}
