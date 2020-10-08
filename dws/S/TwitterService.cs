using System;
using System.Collections.Generic;
using System.IO;
using Tweetinvi;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using System.Net;
using System.Drawing.Imaging;
using System.Drawing;

namespace dws
{
    public class TwitterService
    {
        private static string _alreadyRepliedFile = "replied.json";
        private static string _replies = "replies.json";
        private static List<long> _alreadyReplied = new List<long>();
        private static List<string> replies = new List<string>();
        private TwitterClient twitterClient;

        public TwitterService(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            if (File.Exists(_alreadyRepliedFile))
                _alreadyReplied = JsonConvert.DeserializeObject<List<long>>(File.ReadAllText(_alreadyRepliedFile));

            if (File.Exists(_replies))
                replies = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_replies));

            var userClient = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
        }

        public void AddReply(string reply)
        {
            replies.Add(reply);
            File.WriteAllText(_replies, JsonConvert.SerializeObject(replies));
        }

        public async Task PostImage(string pic)
        {
            string format = ".png";
            if (pic.EndsWith(".jpeg") || pic.EndsWith(".jpg"))
            {
                format = ".jpg";
                SaveImage(ImageFormat.Jpeg, pic);
            }
            else
            {
                SaveImage(ImageFormat.Png, pic);
            }

            var tweetinviLogoBinary = File.ReadAllBytes("image" + format);
            var uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(tweetinviLogoBinary);
            var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters("dog wearing sunglasses")
            {
                Medias = { uploadedImage }
            });

        }

        private void SaveImage(ImageFormat format, string imageUrl)
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(imageUrl);
            Bitmap bitmap; bitmap = new Bitmap(stream);

            if (bitmap != null)
            {
                bitmap.Save("image", format);
            }

            stream.Flush();
            stream.Close();
            client.Dispose();
        }

        public async Task AuthClient()
        {
            var user = await twitterClient.Users.GetAuthenticatedUserAsync();
        }

        public async Task checkMentions()
        {
            var mentions = await twitterClient.Timelines.GetMentionsTimelineAsync();
            var talky = GetRandomReply();

            if (File.Exists(_alreadyRepliedFile))
            {
                foreach (ITweet tweet in mentions)
                {
                    if (!_alreadyReplied.Contains(tweet.Id))
                    {
                        talky.InReplyToTweet = tweet;
                        await twitterClient.Tweets.FavoriteTweetAsync(tweet);
                        var reply = await twitterClient.Tweets.PublishTweetAsync(talky);
                        _alreadyReplied.Add(tweet.Id);
                    }
                }
            }

            else
            {
                foreach (ITweet tweet in mentions)
                {
                    talky.InReplyToTweet = tweet;
                    await twitterClient.Tweets.FavoriteTweetAsync(tweet);
                    var reply = await twitterClient.Tweets.PublishTweetAsync(talky);
                    _alreadyReplied.Add(tweet.Id);
                }
            }

            File.WriteAllText(_alreadyRepliedFile, JsonConvert.SerializeObject(_alreadyReplied));
        }

        private PublishTweetParameters GetRandomReply()
        {
            Random rnd = new Random();
            int r = rnd.Next(replies.Count);
            return new PublishTweetParameters(replies[r]);
        }
    }
}
