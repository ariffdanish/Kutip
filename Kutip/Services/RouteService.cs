using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kutip.Services
{
    public class RouteService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "5b3ce3597851110001cf62482239c9cb6c974be0b3929e0b71635efc";
        private const string BaseUrl = "https://api.openrouteservice.org/v2/directions/driving-car ";

        public RouteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);
        }

        public async Task<RouteResponse> GetRouteAsync(double[] start, double[] end)
        {
            try
            {
                // Create the request body without 'format_out'
                var body = new
                {
                    coordinates = new[] { start, end }
                };

                var json = JsonSerializer.Serialize(body);
                Console.WriteLine($"Request Body: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(BaseUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode} - {errorContent}");
                    throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<RouteResponse>();
                return responseJson;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching route: {ex.Message}");
                throw;
            }
        }
    }

    // Models matching OpenRouteService response structure
    public class RouteResponse
    {
        public Route[] Routes { get; set; }
    }

    public class Route
    {
        public Summary Summary { get; set; }
    }

    public class Summary
    {
        public double Distance { get; set; } // meters
        public double Duration { get; set; } // seconds
    }
}