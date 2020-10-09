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

namespace dws.Modules
{
    public class SubmissionModule : InteractiveBase<SocketCommandContext>
    {
        public TwitterService TwitterService { get; set; }

        public SubmissionModule()
        {
            if (!File.Exists(authorizedAgentsFile))
            {
                File.WriteAllText(authorizedAgentsFile, JsonConvert.SerializeObject(authorizedAgents));
            }
            else
            {
                authorizedAgents = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(authorizedAgentsFile));
            }
        }

        private List<ulong> authorizedAgents = new List<ulong>()
        {
            404793286130270208 //hi dione
        };

        private readonly string authorizedAgentsFile = "admins.json";
        private static string _queue = "images.json";
        [Command("r", RunMode = RunMode.Async)]
        public async Task JustReplyAsync()
        {
            await ReplyAsync(TwitterService.GetRandomReply());
        }

        [Command("t")]
        public async Task test()
        {
            await ReplyAsync("hi");
        }
        [Command("addreply", RunMode = RunMode.Async)]
        [Alias("ar")]
        public async Task AddReplyAsync([Remainder] string reply)
        {
            if (authorizedAgents.Contains(Context.User.Id))
            {
                TwitterService.AddReply(reply);
                await Context.Channel.SendMessageAsync($"Reply has been added to list! 🔥");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"U cant do that");
            }
        }
        [Command("authusers")]
        public async Task AuthorizedUsers()
        {
            List<string> q = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(authorizedAgentsFile));
            string list = "";
            foreach (var b in q)
            {
                list += b + "\n";
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
        public async Task AddUser(IUser user)
        {
            if (authorizedAgents.Contains(Context.User.Id))
            {
                authorizedAgents.Add(user.Id);
                File.WriteAllText(authorizedAgentsFile, JsonConvert.SerializeObject(authorizedAgents));
                await Context.Channel.SendMessageAsync($"{user.Mention} has been added to list 🔥");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"U cant do that");
            }
        }
        [Command("cm"), Alias("checkmentions")]
        public async Task CheckMentions()
        {
            if (authorizedAgents.Contains(Context.User.Id))
            {
                await TwitterService.checkMentions();
                await Context.Channel.SendMessageAsync($" 🔥");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"U cant do that");
            }
        }
        [Command("submitphoto", RunMode = RunMode.Async)]
        [Alias("sp", "addpic", "ap")]
        public async Task AddPicAsync()
        {
            if (authorizedAgents.Contains(Context.User.Id))
            {
                int cnt = 0;

                var q = GetQueue();
                foreach (var ok in Context.Message.Attachments)
                {
                   if (ok.Url.EndsWith("png") || ok.Url.EndsWith("jpg") || ok.Url.EndsWith("jpeg"))
                   {
                       q.Add(ok.Url);
                   }
                   else { }
                   cnt += 1;
                }
                File.WriteAllText(_queue, JsonConvert.SerializeObject(q));
                await Context.Channel.SendMessageAsync($"{cnt} image(s) have been added to the queue! 🔥");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"U cant do that");
            }
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
        public async Task ForcePostAsync()
        {
            if (authorizedAgents.Contains(Context.User.Id))
            {
                var q = GetQueue();
                var o = q.First();
                q.Remove(q.First());
                File.WriteAllText("images.json", JsonConvert.SerializeObject(q));
                await TwitterService.PostImage(o);
                File.WriteAllText("multi.txt", "0");
                await Context.Channel.SendMessageAsync("ok");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"U cant do that");
            }
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task CheckQueueAsync()
        {
            List<string> q = GetQueue();

            List<EmbedBuilder> embeds = new List<EmbedBuilder>();
            int p = 1;
            foreach (var item in q)
            {
                embeds.Add(new EmbedBuilder()
                {
                    Title = "dog",
                    ImageUrl = item,
                    Description = $"Image {p}"
                });
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
            if (!(list.Length > 1999))
            {
                await Ext.EmbedAsync(Context.Channel, new EmbedBuilder()
                {
                    Title = "Responses",
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
                            Title = "Responses",
                            Description = list
                        });
                    }
                }
                await PagedReplyAsync(f, false);
            }
        }

        [Command("ri", RunMode = RunMode.Async), Alias("removeimage", "rq")]
        public async Task removeFromQ(int index)
        {
            var v = GetQueue();
            var img = v[index - 1];
            await ReplyAsync(img);
            await ReplyAsync("Remove this? (Y/N)");
            var msg = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
            if (msg.Content == "Y")
            {
                v.Remove(img);
                File.WriteAllText(_queue, JsonConvert.SerializeObject(v));
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
        private List<string> GetQueue()
        {
            List<string> q = new List<string>();
            if (File.Exists(_queue))
            {
                q = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_queue));
            }
            else { }
            return q;
        }
    }
}
