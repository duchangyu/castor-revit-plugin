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
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"Initializing WebServiceBroker with ApiUrl: {_settingsService.ApiUrl}");

        if (string.IsNullOrEmpty(_settingsService.ApiUrl))
        {
            throw new InvalidOperationException("ApiUrl is not set in the settings.");
        }

        client.BaseAddress = CreateBaseUri(_settingsService.ApiUrl);
        client.Timeout = TimeSpan.FromSeconds(60);

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
            var requestUri = CreateRequestUri(endpoint);
            var response = await client.GetAsync(requestUri, cancellationToken);
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
            var requestUri = CreateRequestUri(endpoint);
            var fullUrl = GetFullUrl(endpoint);
            Log.Information($"POST {fullUrl} - Body: {GetLoggableBody(requestUri, json)}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUri, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync();
            Log.Information($"POST response status: {response.StatusCode}, body: {GetLoggableBody(requestUri, responseBody)}");

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"HTTP error: {response.StatusCode} for URL: {fullUrl}");
                throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
            }

            return responseBody;
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"HTTP Request error: {ex.Message}");
            throw;
        }
    }

    public static async Task<string> SendMultipartFileRequestAsync(
        string endpoint,
        byte[] fileBytes,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = CreateRequestUri(endpoint);
            var fullUrl = GetFullUrl(endpoint);
            Log.Information($"POST {fullUrl} - Multipart file: {fileName}, mimeType: {mimeType}, bytes: {fileBytes.Length}");

            using (var form = new MultipartFormDataContent())
            using (var fileContent = new ByteArrayContent(fileBytes))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                form.Add(fileContent, "file", fileName);

                var response = await client.PostAsync(requestUri, form, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();
                Log.Information($"POST multipart response status: {response.StatusCode}, body: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"HTTP error: {response.StatusCode} for URL: {fullUrl}");
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
                }

                return responseBody;
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"HTTP Multipart request error: {ex.Message}");
            throw;
        }
    }

    public static string GetFullUrl(string endpoint)
    {
        return new Uri(client.BaseAddress, CreateRequestUri(endpoint)).ToString();
    }

    private static Uri CreateBaseUri(string apiUrl)
    {
        var normalized = apiUrl.EndsWith("/", StringComparison.Ordinal) ? apiUrl : apiUrl + "/";
        return new Uri(normalized);
    }

    private static string CreateRequestUri(string endpoint)
    {
        return endpoint.TrimStart('/');
    }

    private static string GetLoggableBody(string requestUri, string json)
    {
        return requestUri.StartsWith("auth/", StringComparison.OrdinalIgnoreCase) ? "<redacted>" : json;
    }
}
