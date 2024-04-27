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
        private readonly string REDDIT_GROUP_NAME = "ProgrammerHumor";

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

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GetPostResponse>(responseBody);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(responseBody);
                        response.EnsureSuccessStatusCode(); // Throw if not a success code.
                    }


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
                    
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GetPostResponse>(responseBody);


                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(responseBody);
                        response.EnsureSuccessStatusCode(); // Throw if not a success code.
                    }

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
                
                var currentDate = DateTime.UtcNow.AddDays(1);
                TimeSpan ts = new TimeSpan(00, 00, 00);
                currentDate = currentDate.Date + ts;

                var lastScheduledPost = scheduled_posts.Data?.OrderByDescending(e => e.UnixScheduledPublishTime).FirstOrDefault();

                if (lastScheduledPost != null)
                {
                    var lastSchedulePostDate = DateTimeOffset.FromUnixTimeSeconds(lastScheduledPost.UnixScheduledPublishTime).UtcDateTime;

                    if (lastSchedulePostDate > currentDate)
                    {
                        currentDate = lastSchedulePostDate;
                    }
                }

                if (redditResponse != null)
                {
                    int ctr = 0;
                    Random random = new Random();

                    foreach (var child in redditResponse)
                    {
                        if (!existing_posts.Data.Any(e => e.StatusType == "added_photos" && e.Message.Contains(child.Data.Title)) && !child.Data.Url.Contains("gallery"))
                        {                           
                            currentDate = currentDate.AddHours(6);

                            var amazonUrls = GetAmazonUrls();
                            var url = amazonUrls[random.Next(0, amazonUrls.Length - 1)];
                            var request = new PublishedScheduledPostRequest
                            {
                                Caption = $"{child.Data.Title}\n\n{url}" ,
                                PhotoUrl = child.Data.Url,
                                UnixTimePublishDate = ((DateTimeOffset)currentDate).ToUnixTimeSeconds().ToString()
                            };

                            ctr++;
                            Console.WriteLine($"{ctr}. Schedule Date: {currentDate.Date.ToShortDateString()}, Time: {currentDate.TimeOfDay}, Caption: {request.Caption}, PostDate: {child.Data.Created}, Ups: {child.Data.Ups}, Image: {child.Data.Url}\n");
                            
                            await PublishedScheduledPost(request);

                            Console.WriteLine("Published Done.");
                            Console.WriteLine("------------------------------------------------------------------------------- >>\n\n\n");

                        }                        
                    }
                }
            }
        }

        public string[] GetAmazonUrls()
        {
            return new string[]
            {
                "https://amzn.to/4a4h93z",
                "https://amzn.to/3vgjwkH",
                "https://amzn.to/4a5uit8",
                "https://amzn.to/4a4hfbr",
                "https://amzn.to/3VwKyiu",
                "https://amzn.to/4ct0QP8",
                "https://amzn.to/3xas11e",
                "https://amzn.to/43sYKuL",
                "https://amzn.to/43uaSvo",
                "https://amzn.to/3vzKhAq",
                "https://amzn.to/43Da1Zp",
                "https://amzn.to/3TP8zQw",
                "https://amzn.to/4982yCD",
                "https://amzn.to/43DvcKU",
                "https://amzn.to/4cuPp9X",
                "https://amzn.to/4adr5I0",
                "https://amzn.to/4cADpU6",
                "https://amzn.to/4aaUbYl",
                "https://amzn.to/4axXs3J",
                "https://amzn.to/4aBjt1R",
                "https://amzn.to/3VE2Lun",
                "https://amzn.to/3VIR99D"
            };
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
