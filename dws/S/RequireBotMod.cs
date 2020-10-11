using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dws.S
{
    public class RequireBotMod : PreconditionAttribute
    {
        private readonly string authorizedAgentsFile = "admins.json";

        private List<ulong> authorizedAgents = new List<ulong>()
        {
            404793286130270208 //hi dione
        };

        private static Task<PreconditionResult> NotElevated = Task.FromResult(PreconditionResult.FromError("You are not the father."));
        private static Task<PreconditionResult> Elevated = Task.FromResult(PreconditionResult.FromSuccess());
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!File.Exists(authorizedAgentsFile))
            {
                File.WriteAllText(authorizedAgentsFile, JsonConvert.SerializeObject(authorizedAgents));
            }
            else
            {
                authorizedAgents = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(authorizedAgentsFile));
            }
            if (!authorizedAgents.Contains(context.User.Id)) return NotElevated;
            else return Elevated;
        }
    }
}
