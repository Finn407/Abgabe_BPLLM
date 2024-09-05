using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlazorApp1.Data
{
    public class LLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "Enter your Key here";

        public LLMService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GetResponseFromChatGPT(string prompt)
        {
            //baue Requestbody
            var requestBody = new
            {
                //wähle Modell
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
            }
            //extrahiere Antwort als String
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic responseJson = JsonConvert.DeserializeObject(responseBody);
            return responseJson.choices[0].message.content;
        }
    }
}
