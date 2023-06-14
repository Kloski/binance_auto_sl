using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Function.Domain;
using Newtonsoft.Json;

namespace Service.Service;

public abstract class HaService
{
    private static readonly string HaUrl = Environment.GetEnvironmentVariable("HA_URL")!;
    private static readonly string HaBearer = Environment.GetEnvironmentVariable("HA_Bearer")!;

    public static async Task<HttpStatusCode> SendStateToHa(IHaMessage statusMessage)
    {
        var serializeObject = JsonConvert.SerializeObject(new Dictionary<string, string> { { "state", statusMessage.ToString() } });
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HaBearer);
        var httpResponseMessage = await httpClient.PostAsync(HaUrl, new StringContent(serializeObject, Encoding.UTF8, "application/json"));
        return httpResponseMessage.StatusCode;
    }
}