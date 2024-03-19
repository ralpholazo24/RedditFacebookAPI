using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RedditFacebookAPI
{
    public class RedditClient
    {
        public async Task<RedditResponse> GetRedditPosts(string groupName)
        {            
            HttpClient client = new HttpClient();

            // Set a user-agent header
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyRedditClient/1.0");

            HttpResponseMessage response = await client.GetAsync($"https://www.reddit.com/r/{groupName}.json");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize JSON response
            var redditResponse = JsonSerializer.Deserialize<RedditResponse>(responseBody);
      
            return redditResponse;
        }
    }


    public class RedditResponse
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("data")]
        public RedditData Data { get; set; }
    }

    public class RedditData
    {
        [JsonPropertyName("children")]
        public List<RedditChild> Children { get; set; }
    }

    public class RedditChild
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("data")]
        public RedditChildData Data { get; set; }
    }

    public class RedditChildData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("created_utc")]
        public double Created_UTC { get; set; }


        public DateTime Created
        {
            get
            {
                DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                DateTime dateTime = epochTime.AddSeconds(this.Created_UTC);
                // To convert UTC to local time, if necessary
                DateTime localDateTime = dateTime.ToLocalTime();

                return localDateTime;
            }
        }
    }
}
