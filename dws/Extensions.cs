using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace dws
{
    public static class Ext
    {
        #region objects
        public static T Random<T>(this IEnumerable<T> thing)
        {
            return thing.ElementAt(new Random().Next(thing.Count()));
        }
        public static T Random<T>(this IReadOnlyCollection<T> thing)
        {
            return thing.ElementAt(new Random().Next(thing.Count()));
        }
        public static string ToPretty(this DateTime time)
            => $"{time.ToUniversalTime().ToShortTimeString()} on {time.ToUniversalTime().ToShortDateString()} UTC";
        public static string ToPretty(this DateTimeOffset time)
            => ToPretty(time.UtcDateTime);

        public static T Get<T>(this IServiceProvider PROVIDER)
            => (T)PROVIDER.GetService(typeof(T));
        public static string ToPretty(this TimeSpan Length)
        {
            if (Length >= TimeSpan.FromDays(1))
                return Length.ToString(@"dd\:hh\:mm\:ss");
            if (Length >= TimeSpan.FromHours(1))
                return Length.ToString(@"hh\:mm\:ss");
            return Length.ToString(@"mm\:ss");
        }
        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> values, int groupSize, int? maxCount = null)
        {
            List<List<T>> result = new List<List<T>>();
            // Quick and special scenario
            if (values.Count() <= groupSize)
            {
                result.Add(values.ToList());
            }
            else
            {
                List<T> valueList = values.ToList();
                int startIndex = 0;
                int count = valueList.Count;
                int elementCount = 0;

                while (startIndex < count && (!maxCount.HasValue || (maxCount.HasValue && startIndex < maxCount)))
                {
                    elementCount = (startIndex + groupSize > count) ? count - startIndex : groupSize;
                    result.Add(valueList.GetRange(startIndex, elementCount));
                    startIndex += elementCount;
                }
            }
            return result;
        }
        public static void RemoveRange<T>(this List<T> values, List<T> toremove)
        {
            foreach (var thing in toremove)
            {
                values.Remove(thing);
            }
        }
        public static IEnumerator GetEnumerator(this object obj)
        {
            if (obj as IEnumerable<object> == null && obj as List<object> == null)
            {
                IEnumerable<KeyValuePair<string, string>> tmp =
                    obj.GetType()
                    .GetProperties()
                    .Select(pi => new KeyValuePair<string, string>(pi.Name.SplitCamelCase(), pi.GetGetMethod().Invoke(obj, null).ToString()));

                return tmp.GetEnumerator();
            }
            else return null;
        }
        public static TimeSpan Avg(this List<TimeSpan> spans)
        {
            double doubleAverageTicks = spans.Average(timeSpan => timeSpan.Ticks);
            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);

            return new TimeSpan(longAverageTicks);
        }
        #endregion
        #region Discord
        public static async Task<SocketMessage> NextMessageAsync(this SocketCommandContext context)
        {
            InteractiveService _ = new InteractiveService(context.Client);
            return await _.NextMessageAsync(context, timeout: new TimeSpan(0, 1, 0));
        }
        public static async Task<IUserMessage> PagedReplyAsync(this SocketCommandContext c, IEnumerable<EmbedBuilder> embed)
        {
            InteractiveService _ = new InteractiveService(c.Client);
            return await _.SendPaginatedMessageAsync(c, new PaginatedMessage()
            {
                Pages = embed
            });
        }
        public static EmbedBuilder AddInlineField(this EmbedBuilder embed, string name, object value)
        {
            var em = new EmbedFieldBuilder();
            em.IsInline = true;
            em.Value = value;
            em.Name = name;
            embed.AddField(em);
            return embed;
        }
        public static string GetFirstAttachment(this IUserMessage message)
        {
            if (message.Attachments.Count != 0)
                return message.Attachments.First().Url;
            else return null;
        }
        public static EmbedBuilder RemoveFields(this EmbedBuilder builder, List<EmbedFieldBuilder> embedField)
        {
            for (int i = 0; i < embedField.Count; ++i)
            {
                builder.Fields.Remove(embedField[i]);
            }
            return builder;
        }
        public static async Task<IUserMessage> EmbedAsync(this ISocketMessageChannel chan, Embed embed, string msg = "")
           => await chan.SendMessageAsync(msg, embed: embed);
        public static async Task<IUserMessage> DM(this IUser user, string text)
            => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: new EmbedBuilder().WithColor(Color.Magenta).WithDescription(text).Build());
        public static async Task<IUserMessage> DM(this IUser user, Embed embed)
            => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: embed);
        public static string Usage(this CommandInfo info)
            => info.Remarks;
        public static EmbedBuilder AddFields(this EmbedBuilder embed, IEnumerable<EmbedFieldBuilder> builder)
        {
            foreach (var field in builder)
            {
                embed.AddField(field);
            }
            return embed;
        }
        public static EmbedBuilder AddFields(this EmbedBuilder embed, IEnumerable<EmbedField> builder)
        {
            foreach (var field in builder)
            {
                embed.AddField(field.Name, field.Value, field.Inline);
            }
            return embed;
        }
        #endregion
        #region string
        public static bool IsApproximateTo(this string orig, string compareto)
        {
            if (orig == compareto)
                return true;
            if (StringCompare(orig, compareto) >= 75)
                return true;
            return false;
        }
        static double StringCompare(string a, string b)
        {
            if (a == b) //Same string, no iteration needed.
                return 100;
            if ((a.Length == 0) || (b.Length == 0)) //One is empty, second is not
            {
                return 0;
            }
            double maxLen = a.Length > b.Length ? a.Length : b.Length;
            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int sameCharAtIndex = 0;
            for (int i = 0; i < minLen; i++) //Compare char by char
            {
                if (a[i] == b[i])
                {
                    sameCharAtIndex++;
                }
            }
            return sameCharAtIndex / maxLen * 100;
        }
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
        // https://stackoverflow.com/users/1145669/thundergr
        public static double GetSimilarityRatio(this String FullString1, String FullString2, out double WordsRatio, out double RealWordsRatio)
        {
            double theResult = 0;
            String[] Splitted1 = FullString1.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            String[] Splitted2 = FullString2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (Splitted1.Length < Splitted2.Length)
            {
                String[] Temp = Splitted2;
                Splitted2 = Splitted1;
                Splitted1 = Temp;
            }
            int[,] theScores = new int[Splitted1.Length, Splitted2.Length];//Keep the best scores for each word.0 is the best, 1000 is the starting.
            int[] BestWord = new int[Splitted1.Length];//Index to the best word of Splitted2 for the Splitted1.

            for (int loop = 0; loop < Splitted1.Length; loop++)
            {
                for (int loop1 = 0; loop1 < Splitted2.Length; loop1++) theScores[loop, loop1] = 1000;
                BestWord[loop] = -1;
            }
            int WordsMatched = 0;
            for (int loop = 0; loop < Splitted1.Length; loop++)
            {
                String String1 = Splitted1[loop];
                for (int loop1 = 0; loop1 < Splitted2.Length; loop1++)
                {
                    String String2 = Splitted2[loop1];
                    int LevenshteinDistance = Compute(String1, String2);
                    theScores[loop, loop1] = LevenshteinDistance;
                    if (BestWord[loop] == -1 || theScores[loop, BestWord[loop]] > LevenshteinDistance) BestWord[loop] = loop1;
                }
            }

            for (int loop = 0; loop < Splitted1.Length; loop++)
            {
                if (theScores[loop, BestWord[loop]] == 1000) continue;
                for (int loop1 = loop + 1; loop1 < Splitted1.Length; loop1++)
                {
                    if (theScores[loop1, BestWord[loop1]] == 1000) continue;//the worst score available, so there are no more words left
                    if (BestWord[loop] == BestWord[loop1])//2 words have the same best word
                    {
                        //The first in order has the advantage of keeping the word in equality
                        if (theScores[loop, BestWord[loop]] <= theScores[loop1, BestWord[loop1]])
                        {
                            theScores[loop1, BestWord[loop1]] = 1000;
                            int CurrentBest = -1;
                            int CurrentScore = 1000;
                            for (int loop2 = 0; loop2 < Splitted2.Length; loop2++)
                            {
                                //Find next bestword
                                if (CurrentBest == -1 || CurrentScore > theScores[loop1, loop2])
                                {
                                    CurrentBest = loop2;
                                    CurrentScore = theScores[loop1, loop2];
                                }
                            }
                            BestWord[loop1] = CurrentBest;
                        }
                        else//the latter has a better score
                        {
                            theScores[loop, BestWord[loop]] = 1000;
                            int CurrentBest = -1;
                            int CurrentScore = 1000;
                            for (int loop2 = 0; loop2 < Splitted2.Length; loop2++)
                            {
                                //Find next bestword
                                if (CurrentBest == -1 || CurrentScore > theScores[loop, loop2])
                                {
                                    CurrentBest = loop2;
                                    CurrentScore = theScores[loop, loop2];
                                }
                            }
                            BestWord[loop] = CurrentBest;
                        }

                        loop = -1;
                        break;//recalculate all
                    }
                }
            }
            for (int loop = 0; loop < Splitted1.Length; loop++)
            {
                if (theScores[loop, BestWord[loop]] == 1000) theResult += Splitted1[loop].Length;//All words without a score for best word are max failures
                else
                {
                    theResult += theScores[loop, BestWord[loop]];
                    if (theScores[loop, BestWord[loop]] == 0) WordsMatched++;
                }
            }
            int theLength = (FullString1.Replace(" ", "").Length > FullString2.Replace(" ", "").Length) ? FullString1.Replace(" ", "").Length : FullString2.Replace(" ", "").Length;
            if (theResult > theLength) theResult = theLength;
            theResult = (1 - (theResult / theLength)) * 100;
            WordsRatio = ((double)WordsMatched / Splitted2.Length) * 100;
            RealWordsRatio = ((double)WordsMatched / Splitted1.Length) * 100;
            return theResult;
        }
        public static string Randomize(this string f, int length = 5)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string Random(this string f)
                => f = $"{Guid.NewGuid()}";
        public static string ToPretty(this string str)
            => str.ToLower().Replace("_", " ").FirstLetterToUpper();

        public static string Remove(this string strin, string toemr)
            => strin.Replace(toemr, "");
        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
        public static string SplitCamelCase(this string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
        #endregion
    }
}
