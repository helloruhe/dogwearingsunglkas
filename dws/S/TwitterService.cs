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
using System.Linq;
using System.Diagnostics;

namespace dws
{
    public class TwitterService
    {
        private static string _alreadyRepliedFile = "replied.json";
        private static string _replies = "replies.json";
        private static List<long> _alreadyReplied = new List<long>();
        private static List<string> replies = new List<string>();
        private TwitterClient twitterClient = new TwitterClient(null);

        public TwitterService()
        {
            if (File.Exists(_alreadyRepliedFile))
                _alreadyReplied = JsonConvert.DeserializeObject<List<long>>(File.ReadAllText(_alreadyRepliedFile));

            if (File.Exists(_replies))
                replies = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_replies));

        }

        public void AddReply(string reply)
        {
            replies.Add(reply);
            File.WriteAllText(_replies, JsonConvert.SerializeObject(replies));
        }

        public async Task PostImage(string pic)
        {
            var uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(GetImage(pic));
            var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters("dog wearing sunglasses")
            {
                Medias = { uploadedImage }
            });

        }
        private byte[] GetImage(string iconPath)
        {
            using (WebClient client = new WebClient())
            {
                byte[] pic = client.DownloadData(iconPath);
                string checkPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +@"\1.png";
                File.WriteAllBytes(checkPath, pic);
                return pic;
            }
        }


        public async Task AuthClient()
        {
            var appClient = new TwitterClient(Environment.GetEnvironmentVariable("consumerKey"), Environment.GetEnvironmentVariable("consumerSecret"));

            // Start the authentication process
            var authenticationRequest = await appClient.Auth.RequestAuthenticationUrlAsync();

            // Go to the URL so that Twitter authenticates the user and gives him a PIN code.
            Process.Start(new ProcessStartInfo(authenticationRequest.AuthorizationURL)
            {
                UseShellExecute = true
            });

            // Ask the user to enter the pin code given by Twitter
            Console.WriteLine("Please enter the code and press enter.");
            var pinCode = Console.ReadLine();

            // With this pin code it is now possible to get the credentials back from Twitter
            var userCredentials = await appClient.Auth.RequestCredentialsFromVerifierCodeAsync(pinCode, authenticationRequest);

            // You can now save those credentials or use them as followed
            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();
            twitterClient = userClient;
            Console.WriteLine("Congratulation you have authenticated the user: " + user);
        }

        public async Task checkMentions()
        {
            try
            {
                var mentions = await twitterClient.Timelines.GetMentionsTimelineAsync();

                if (File.Exists(_alreadyRepliedFile))
                {
                    foreach (ITweet tweet in mentions)
                    {
                        var talky = GetRandomReply();

                        if (!_alreadyReplied.Contains(tweet.Id))
                        {
                            string ats = $"@{tweet.CreatedBy.ScreenName} ";
                            foreach (var user in tweet.UserMentions)
                            {
                               if (user.ScreenName != "Dogwearingsun")
                                ats += $"@{user.ScreenName} ";
                            }
                                var reply = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats + $" {talky}")
                            {
                                InReplyToTweet = tweet
                            });
                            await twitterClient.Tweets.FavoriteTweetAsync(tweet);
                            _alreadyReplied.Add(tweet.Id);
                        }
                    }
                }

                else
                {
                    foreach (ITweet tweet in mentions)
                    {
                        var talky = GetRandomReply();

                        string ats = $"@{tweet.CreatedBy.ScreenName} ";
                        foreach (var user in tweet.UserMentions)
                        {

                            if (user.ScreenName != "Dogwearingsun")
                                ats += $"@{user.ScreenName} ";
                        }
                        var reply = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats + $" {talky}")
                        {
                            InReplyToTweet = tweet
                        });
                        await twitterClient.Tweets.FavoriteTweetAsync(tweet);
                        _alreadyReplied.Add(tweet.Id);
                    }
                }

                File.WriteAllText(_alreadyRepliedFile, JsonConvert.SerializeObject(_alreadyReplied));
            }
            catch { }
        }
        public async Task<string> GetLatestTweet()
        {
            var userTimelineTweets = await twitterClient.Timelines.GetUserTimelineAsync("dogwearingsungl");
            return userTimelineTweets.First(x => x.UserMentions == null).Url;
        }
        public string GetRandomReply()
        {
            try
            {
                Random rnd = new Random();
                int r = rnd.Next(replies.Count);
                return replies[r].Remove("\"/");
            }
            catch
            {
                return "music";
            }
            
        }
    }
}
