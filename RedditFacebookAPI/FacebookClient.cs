using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace RedditFacebookAPI
{    
    public class FacebookClient
    {
        private readonly string FACEBOOK_ACCESS_TOKEN = "<your-access-token>";
        private readonly string FACEBOOK_API_VERSION = "v19.0";
        private readonly string FACEBOOK_BASE_ADDRESS = "https://graph.facebook.com/";
        private readonly string REDDIT_GROUP_NAME = "Programming";

        public async Task<GetPostResponse> GetPosts()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(FACEBOOK_BASE_ADDRESS);

                var parameters = new Dictionary<string, string>
                {
                    ["fields"] = "message,status_type",
                    ["access_token"] = FACEBOOK_ACCESS_TOKEN
                };

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                foreach (var param in parameters)
                {
                    queryString[param.Key] = param.Value;
                }

                var requestUri = $"{FACEBOOK_API_VERSION}/me/posts?{queryString}";

                try
                {
                    var response = await client.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode(); // Throw if not a success code.
                    var responseBody = await response.Content.ReadAsStringAsync();                   
                    var result = JsonSerializer.Deserialize<GetPostResponse>(responseBody);

                    return result;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request exception: {e.Message}");
                    throw e;
                }
            }
        }

        public async Task<GetPostResponse> GetScheduledPosts()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(FACEBOOK_BASE_ADDRESS);                
                var requestUri = $"{FACEBOOK_API_VERSION}/me/scheduled_posts?fields=scheduled_publish_time,status_type,id,message&access_token={FACEBOOK_ACCESS_TOKEN}";

                try
                {
                    var response = await client.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode(); // Throw if not a success code.
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GetPostResponse>(responseBody);                    
                    return result;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request exception: {e.Message}");
                    throw e;
                }
            }
        }

        public async Task PublishedScheduledPost(PublishedScheduledPostRequest request)
        {
            using (var client = new HttpClient())
            {
                
                client.BaseAddress = new Uri(FACEBOOK_BASE_ADDRESS);

                var parameters = new Dictionary<string, string>
                {
                    ["caption"] = request.Caption,
                    ["url"] = request.PhotoUrl,
                    ["scheduled_publish_time"] = request.UnixTimePublishDate,
                    ["published"] = "false",
                    ["access_token"] = FACEBOOK_ACCESS_TOKEN
                };

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                foreach (var param in parameters)
                {
                    queryString[param.Key] = param.Value;
                }

                var requestUri = $"{FACEBOOK_API_VERSION}/me/photos?{queryString}";

                try
                {
                    var response = await client.PostAsync(requestUri, null);
                    response.EnsureSuccessStatusCode(); // Throw if not a success code.
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request exception: {e.Message}");
                    throw e;
                }           
            }
        }

        public async Task ProcessScheduledPost()
        {
            using (var client = new HttpClient())
            {
                var existing_posts = await GetPosts();
                var scheduled_posts = await GetScheduledPosts();
                
                // merge all existing posts and scheduled posts
                existing_posts.Data.AddRange(scheduled_posts.Data);

                RedditClient redditClient = new RedditClient();
                var redditResponse = await redditClient.GetRedditPosts(REDDIT_GROUP_NAME);
                
                var currentDate = DateTime.UtcNow;
                TimeSpan ts = new TimeSpan(15, 00, 0);
                currentDate = currentDate.Date + ts;

                var lastScheduledPost = scheduled_posts.Data?.OrderByDescending(e => e.UnixScheduledPublishTime).FirstOrDefault();

                if (lastScheduledPost != null)
                {
                    currentDate = DateTimeOffset.FromUnixTimeSeconds(lastScheduledPost.UnixScheduledPublishTime).UtcDateTime;
                }

                if (redditResponse?.Data.Children != null)
                {                   
                    foreach (var child in redditResponse.Data.Children)
                    {
                        if (!existing_posts.Data.Any(e => e.StatusType == "added_photos" && e.Message == child.Data.Title) && !child.Data.Url.Contains("gallery"))
                        {                           
                            currentDate = currentDate.AddHours(4);

                            var request = new PublishedScheduledPostRequest
                            {
                                Caption = child.Data.Title,
                                PhotoUrl = child.Data.Url,
                                UnixTimePublishDate = ((DateTimeOffset)currentDate).ToUnixTimeSeconds().ToString()
                            };

                            Console.WriteLine($"Schedule Date: {currentDate.Date.ToShortDateString()}, Time: {currentDate.TimeOfDay}");
                            Console.WriteLine($"Title: {child.Data.Title}, Image: {child.Data.Url}\n");

                            await PublishedScheduledPost(request);

                            Console.WriteLine("Published Done.");
                            Console.WriteLine("------------------------------------------------------------------------------- >>\n\n\n");

                        }                        
                    }
                }
            }
        }

        public class GetPostResponse
        {
            [JsonPropertyName("data")]
            public List<PostData> Data { get; set; }
        }

        public class PostData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("scheduled_publish_time")]
            public long UnixScheduledPublishTime { get; set; }

            [JsonPropertyName("status_type")]
            public string StatusType { get; set; }

        }

        public class PublishedScheduledPostRequest
        {
            public string Caption { get; set; }
            public string PhotoUrl { get; set; }
            public string UnixTimePublishDate { get; set; }
        }
    }
}
