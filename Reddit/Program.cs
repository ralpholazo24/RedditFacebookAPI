
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reddit
{
    public class Program
    {
        static async Task Main()
        {
            try
            {
                FacebookClient facebookClient = new FacebookClient();
                await facebookClient.ProcessScheduledPost();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
    }





}
