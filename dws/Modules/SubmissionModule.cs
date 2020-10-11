using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Discord.Addons.Interactive;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using dws.S;

namespace dws.Modules
{
    public class SubmissionModule : InteractiveBase<SocketCommandContext>
    {
        public TwitterService TwitterService { get; set; }
        public ImageService ImageService { get; set; }

        public SubmissionModule()
        {

        }

        [Command("r", RunMode = RunMode.Async)]
        public async Task JustReplyAsync()
        {
            await ReplyAsync(TwitterService.GetRandomReply());
        }

        [Command("addreply", RunMode = RunMode.Async)]
        [Alias("ar")]
        [RequireBotMod]
        public async Task AddReplyAsync([Remainder] string reply)
        { 
                TwitterService.AddReply(reply);
                await Context.Channel.SendMessageAsync($"Reply has been added to list! 🔥");
        }

        [Command("addreply", RunMode = RunMode.Async)]
        [Alias("ar")]
        [RequireBotMod]
        public async Task AddReplyAsync()
        {
            if (Context.Message.Attachments == null)
            {
                await Context.Channel.SendMessageAsync($"wheres the image fucktard");
                return;
            }
            TwitterService.AddReply(Context.Message.GetFirstAttachment());
            await Context.Channel.SendMessageAsync($"Reply has been added to list! 🔥");
        }

        [Command("authusers")]
        public async Task AuthorizedUsers()
        {
            List<string> q = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("admins.json"));
            string list = "";
            foreach (var b in q)
            {
                list += "<@"+b +">"+ "\n";
            }
            if (!(list.Length > 1999))
            {
                await Ext.EmbedAsync(Context.Channel, new EmbedBuilder()
                {
                    Title = "Admins",
                    Description = list
                }.Build());
            }
            else
            {
                var f = new List<EmbedBuilder>();
                foreach (var b in q.ChunkBy(15))
                {
                    list = "";
                    foreach (var e in b)
                    {
                        list += e + "\n";
                        f.Add(new EmbedBuilder()
                        {
                            Title = "Admins",
                            Description = list
                        });
                    }
                }
                await PagedReplyAsync(f, false);
            }
        }

        [Command("addauth", RunMode = RunMode.Async)]
        [Alias("aa")]
        [RequireBotMod]
        public async Task AddUser(IUser user)
        {
                await Context.Channel.SendMessageAsync($"{user.Mention} has been added to list 🔥");
        }

        [Command("cm"), Alias("checkmentions")]
        public async Task CheckMentions()
        {
                await TwitterService.checkMentions();
                await Context.Channel.SendMessageAsync($" 🔥");
        }

        [Command("submitphoto", RunMode = RunMode.Async)]
        [RequireBotMod]
        [Alias("sp", "addpic", "ap")]
        public async Task AddPicAsync()
        {
            if (Context.Message.Attachments == null)
            {
                await Context.Channel.SendMessageAsync($"wheres the image fucktard");
                return;
            }
            Classes.Image img = new Classes.Image()
            {
                ImageUrl = Context.Message.GetFirstAttachment()
            };
            await ReplyAsync("Img source? (\"unknown\" if unknown");
            var msg = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
            img.Source = msg.Content;
            if (img.ImageUrl.EndsWith(".mp4"))
            {
                img.ImageType = Classes.ImageType.Video;
            }
            if (img.Source.EndsWith(".gif"))
            {
                img.ImageType = Classes.ImageType.GIF;
            }
            ImageService.AddImage(img);
            await Context.Channel.SendMessageAsync($"Item has been added to the queue! 🔥");
        }

        [Command("submitphoto", RunMode = RunMode.Async)]
        [RequireBotMod]
        [Alias("sp", "addpic", "ap")]
        public async Task AddPicAsync(string url)
        {
            Classes.Image img = new Classes.Image()
            {
                ImageUrl = url
            };
            await ReplyAsync("Img source? (\"unknown\" if unknown");
            var msg = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
            img.Source = msg.Content;
            if (img.Source.EndsWith(".mp4"))
            {
                img.ImageType = Classes.ImageType.Video;
            }
            if (img.Source.EndsWith(".gif"))
            {
                img.ImageType = Classes.ImageType.GIF;
            }
        
            ImageService.AddImage(img);
            await Context.Channel.SendMessageAsync($"Item has been added to the queue! 🔥");
        }

        [Command("status", RunMode = RunMode.Async)]
        [Alias("cs")]
        public async Task CheckStatusAsync()
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks);

            List<EmbedFieldBuilder> f = new List<EmbedFieldBuilder>();
            f.Add(new EmbedFieldBuilder()
            {
                Name = "Last Posted",
                Value = $"{(short.Parse(File.ReadAllText("multi.txt")) * 300) / 60} Minutes Ago, ",
            });

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Stats",
                Footer = new EmbedFooterBuilder()
                {
                    Text = "twitter.com/dogwearingsun"
                },
                Fields = f

            };
            await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
        }
        
        [Command("forcepost", RunMode = RunMode.Async)]
        [Alias("fp")]
        [RequireBotMod]
        public async Task ForcePostAsync()
        {
            await TwitterService.PostImage();
            File.WriteAllText("multi.txt", "0");
            await Context.Channel.SendMessageAsync("ok");
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task CheckQueueAsync()
        {
            List<Classes.Image> q = ImageService.GetQueue();

            List<EmbedBuilder> embeds = new List<EmbedBuilder>();
            int p = 1;
            foreach (var item in q)
            {
                if (item.ImageType is Classes.ImageType.Image)
                {
                    embeds.Add(new EmbedBuilder()
                    {
                        Title = "dog",
                        ImageUrl = item.ImageUrl
                    });
                }
                else
                {
                    embeds.Add(new EmbedBuilder()
                    {
                        Title = "dog",
                        Description = item.ImageUrl
                    });
                }
                p += 1;
            }

            await PagedReplyAsync(embeds);
        }

        [Command("replies", RunMode = RunMode.Async), Alias("gr")]
        public async Task GetRepliesAsync()
        {
            List<string> q = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("replies.json"));
            string list = "";
            foreach (var b in q)
            {
                list += b + "\n";
            }
            var f = new List<EmbedBuilder>();
            var nonImage = new List<string>();
            foreach (var ok in q)
            {
                if (!ok.EndsWith(".png") || !ok.EndsWith(".gif") 
                    || !ok.EndsWith(".jpg") || !ok.EndsWith(".jpeg"))
                {
                    nonImage.Add(ok);
                }
            }
            foreach (var pee in nonImage)
            {
                q.Remove(pee);
            }
            foreach (var b in nonImage.ChunkBy(15))
            {
                    list = "";
                    foreach (var e in b)
                    {
                        list += e + "\n";
                    }
                    f.Add(new EmbedBuilder()
                    {
                        Title = "Responses",
                        Description = list
                    });
            }
            foreach (var b in q)
            {
                f.Add(new EmbedBuilder()
                {
                    Title = "Responses",
                    ImageUrl = b
                });
            }
            await PagedReplyAsync(f, false);
            
        }

        [Command("ri", RunMode = RunMode.Async), Alias("removeimage", "rq")]
        [RequireBotMod]
        public async Task removeFromQ(int index)
        {
            var img = ImageService.GetImage(index-1);
            await ReplyAsync(img.ImageUrl);
            await ReplyAsync("Remove this? (Y/N)");
            var msg = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
            if (msg.Content == "Y")
            {
                ImageService.RemoveImage(index - 1);
                await ReplyAsync("Removed!");
            }
            if (msg.Content == "N")
            {
                await ReplyAsync("ok");
            }
            else
            {
                await ReplyAsync("?");
            }
        }

        [Command("info")]
        [Alias("about", "whoami", "owner")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync();

            await ReplyAsync(
                $"music sharks snacks.\n\n" +
                $"{Format.Bold("Info")}\n" +
                $"- Author: {app.Owner} ({app.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture} " +
                    $"({RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture})\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()}MiB\n" +
                $"- Guilds: {Context.Client.Guilds.Count}\n" +
                $"- Channels: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n");
        }
        
        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();


    }
}
