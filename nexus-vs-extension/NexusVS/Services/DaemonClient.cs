using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
// Manual JSON construction for minimal dependencies in VSIX (avoiding System.Web.Script or Newtonsoft)

namespace NexusVS.Services
{
    public class DaemonClient
    {
        private readonly HttpClient _httpClient;
        public const string BaseUrl = "http://localhost:5050";

        public DaemonClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> CompileContextAsync(string taskType, string solutionPath, string[] targets)
        {
            // Manual JSON construction to avoid dependency issues in this minimal MVP
            // targets json array
            var targetsJson = "[" + string.Join(",", System.Linq.Enumerable.Select(targets, t => $"\"{t}\"")) + "]";
            var json = $@"{{
                ""TaskType"": ""{taskType}"",
                ""SolutionPath"": ""{solutionPath.Replace("\\", "\\\\")}"",
                ""Targets"": {targetsJson}
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/context/compile", content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString; // Returns raw JSON for now, can parse if needed
        }
    }
}
