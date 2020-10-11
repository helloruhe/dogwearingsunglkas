﻿using System;
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
using dws.S;
using dws.Classes;
using Image = dws.Classes.Image;
using Tweetinvi.Streaming;

namespace dws
{
    public class TwitterService
    {
        private static string _alreadyRepliedFile = "replied.json";
        private static string _replies = "replies.json";
        private static List<long> _alreadyReplied = new List<long>();
        private static List<string> replies = new List<string>();
        private TwitterClient twitterClient = new TwitterClient(null);
        private IFilteredStream mentionStream;
        private IFilteredStream randomStream;

        public ImageService ImageService { get; set; }


        public TwitterService(IServiceProvider p)
        {
            mentionStream = twitterClient.Streams.CreateFilteredStream();
            randomStream = twitterClient.Streams.CreateFilteredStream();
            if (File.Exists(_alreadyRepliedFile))
                _alreadyReplied = JsonConvert.DeserializeObject<List<long>>(File.ReadAllText(_alreadyRepliedFile));

            if (File.Exists(_replies))
                replies = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_replies));
            ImageService = p.Get<ImageService>();

        }

        public void AddReply(string reply)
        {
            replies.Add(reply);
            File.WriteAllText(_replies, JsonConvert.SerializeObject(replies));
        }

        public async Task PostImage(Image frog)
        {
             IMedia uploadedImage = null;
            if (frog.ImageUrl.EndsWith(".mp4"))
            {
                frog.ImageType = ImageType.Video;
            }
            var img = GetImage(frog.ImageUrl, frog.ImageType);
            if (frog.ImageType is ImageType.Image)
            {
                uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(img);
            }
            if (frog.ImageType is ImageType.GIF)
            {
                uploadedImage = await twitterClient.Upload.UploadBinaryAsync(img);
            }
            if (frog.ImageType is ImageType.Video)
            {
                uploadedImage = await PostVideo(img);
            }
            var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters("dog wearing sunglasses")
            {
                Medias = { uploadedImage }
            });
            await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters($"@Dogwearingsun source: {frog.Source}")
            {
                InReplyToTweet = t
            });
            ImageService.RemoveImage(frog);
        }
        private byte[] GetImage(string iconPath, ImageType type)
        {
            using (WebClient client = new WebClient())
            {
                byte[] pic = client.DownloadData(iconPath);
                string checkPath = @"C:\Users\cowboy\Documents";
                if (type is ImageType.Image && !iconPath.EndsWith(".mp4"))
                {
                    checkPath += @"\1.png";
                }
                if (type is ImageType.Video || iconPath.EndsWith(".mp4"))
                {
                    checkPath.Remove(@"\1.png");
                    checkPath += @"\1.mp4";
                }
                if (type is ImageType.GIF || iconPath.EndsWith(".gif"))
                {
                    checkPath.Remove(@"\1.png");
                    checkPath.Remove(@"\1.mp4");
                    checkPath += @"\1.gif";
                }
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

        public async Task StartStreams()
        {
            mentionStream.AddTrack("@Dogwearingsun");
            mentionStream.AddTrack("Dogwearingsun/status/");
            mentionStream.MatchingTweetReceived += async (sender, eventReceived) =>
            {
                await HandleMention(eventReceived.Tweet);
            };
            randomStream.AddTrack("dog wearing sunglasses");

            randomStream.MatchingTweetReceived += async (sender, eventReceived) =>
            {
                    string ats = $"@{eventReceived.Tweet.CreatedBy.ScreenName} ";
                    foreach (var user in eventReceived.Tweet.UserMentions)
                    {
                        if (user.ScreenName != "Dogwearingsun")
                            ats += $"@{user.ScreenName}";
                    }

                    await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats + $" hi"));

            };

            await mentionStream.StartMatchingAllConditionsAsync();
            await randomStream.StartMatchingAllConditionsAsync();
        }

        public async Task HandleMention(ITweet tweet)
        {
            var talky = GetRandomReply();
            byte[] data = null;

            if (!_alreadyReplied.Contains(tweet.Id) && tweet.CreatedBy.ScreenName != "Dogwearingsun")
            {
                string ats = $"@{tweet.CreatedBy.ScreenName} ";
                foreach (var user in tweet.UserMentions)
                {
                    if (user.ScreenName != "Dogwearingsun")
                        ats += $"@{user.ScreenName} ";
                }
                if (talky.EndsWith(".mp4") || talky.EndsWith(".png")
                    || talky.EndsWith(".jpg") || talky.EndsWith(".jpeg"))
                {
                    data = GetImage(talky, ImageType.Image);
                    var uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(data);
                    var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats)
                    {
                        Medias = { uploadedImage }
                    });
                }
                if (talky.EndsWith(".mp4"))
                {
                    data = GetImage(talky, ImageType.Video);
                    var uploadedImage = await PostVideo(data);
                    var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats)
                    {
                        Medias = { uploadedImage }
                    });
                }
                else
                {
                    var reply = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats + $" {talky}")
                    {
                        InReplyToTweet = tweet
                    });
                }
            }
            _alreadyReplied.Add(tweet.Id);
            File.WriteAllText(_alreadyRepliedFile, JsonConvert.SerializeObject(_alreadyReplied));

        }
    
        public async Task checkMentions() //OBSOLETE?
        {

                var mentions = await twitterClient.Timelines.GetMentionsTimelineAsync();
                    foreach (ITweet tweet in mentions)
                    {
                        var talky = GetRandomReply();
                byte[] data = null;

                if (!_alreadyReplied.Contains(tweet.Id) && tweet.CreatedBy.ScreenName != "Dogwearingsun")
                        {
                            string ats = $"@{tweet.CreatedBy.ScreenName} ";
                            foreach (var user in tweet.UserMentions)
                            {
                               if (user.ScreenName != "Dogwearingsun")
                                ats += $"@{user.ScreenName} ";
                            }
                    if (talky.EndsWith(".png") || talky.EndsWith(".jpg") || talky.EndsWith(".jpeg"))
                    {
                        data = GetImage(talky, ImageType.Image);
                        var uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(data);
                        var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats)
                        {
                            Medias = { uploadedImage }
                        });
                    }
                    if (talky.EndsWith(".mp4"))
                    {
                        data = GetImage(talky, ImageType.Video);
                        var uploadedImage = await PostVideo(data);
                        var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats)
                        {
                            Medias = { uploadedImage }
                        });
                    }
                    if (talky.EndsWith(".gif"))
                    {
                        data = GetImage(talky, ImageType.GIF);
                        var uploadedImage = await twitterClient.Upload.UploadBinaryAsync(data);
                        var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats)
                        {
                            Medias = { uploadedImage }
                        });
                    }
                    else
                    {
                        var reply = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters(ats + $" {talky}")
                        {
                            InReplyToTweet = tweet
                        });
                    }
                }
                _alreadyReplied.Add(tweet.Id);

            }
            File.WriteAllText(_alreadyRepliedFile, JsonConvert.SerializeObject(_alreadyReplied));
            
        }

        public async Task PostImage()
        {
            IMedia uploadedImage = null;
            var frog = ImageService.GetImage(0);
            var img = GetImage(frog.ImageUrl, frog.ImageType);
            if (frog.ImageType is ImageType.Image)
            {
                 uploadedImage = await twitterClient.Upload.UploadTweetImageAsync(img);
            }
            if (frog.ImageType is ImageType.GIF)
            {
                uploadedImage = await twitterClient.Upload.UploadBinaryAsync(img);
            }            
            if (frog.ImageType is ImageType.Video)
            {
                uploadedImage = await PostVideo(img);
            }
            var t = await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters("dog wearing sunglasses")
            {
                Medias = { uploadedImage }
            });
            await twitterClient.Tweets.PublishTweetAsync(new PublishTweetParameters($"@Dogwearingsun source: {frog.Source}")
            {
                InReplyToTweet = t
            });
            ImageService.RemoveImage(0);
        }

        private async Task<IMedia> PostVideo(byte[] videoBinary)
        {
            var uploadedVideo = await twitterClient.Upload.UploadTweetVideoAsync(videoBinary);
            await twitterClient.Upload.WaitForMediaProcessingToGetAllMetadataAsync(uploadedVideo);
            return uploadedVideo;
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
                string retur = replies[r];
                if (!retur.EndsWith(".mp4") || !retur.EndsWith(".png")
                    || !retur.EndsWith(".jpg") || !retur.EndsWith(".jpeg"))
                {
                    replies[r].Remove(@"\");
                    replies[r].Remove(@"\\");
                }
                return retur;
            }
            catch
            {
                return "music";
            }
            
        }
    }
}
