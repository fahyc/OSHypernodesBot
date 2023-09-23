using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace MyDiscordBot
{
    public class Commands : BaseCommandModule
    {


		[Command("setdatakey")]
		public async Task SetDataKeyAsync (CommandContext ctx, string dataKey) {
			ulong id = ctx.Channel.Id;
			CheckChannelData(id);
			Program.channelData[ctx.Channel.Id].SetDataKey(dataKey);
			Console.Out.WriteLine("In commmand DataKey");
			await ctx.RespondAsync($"Data key is now '{dataKey}.'");
		}


		[Command("getkey")]
		public async Task GetKeyAsync (CommandContext ctx) {
			ulong id = ctx.Channel.Id;
			await CheckChannelData(id);
			Console.Out.WriteLine("In commmand getKey");
			await ctx.RespondAsync($"Graph data key is  '{Program.channelData[ctx.Channel.Id].fundingKey}.'");
		}

		[Command("forget")]
        public async Task ForgetAsync(CommandContext ctx)
        {
            ulong id = ctx.Channel.Id;
            await CheckChannelData(id);
            Console.Out.WriteLine("In commmand forget");
            int messages = Program.channelData[id].Conversation.Count;
            Program.channelData[id].Conversation = new List<string>();
            await ctx.RespondAsync($"history forgotten. number of messages forgotten: {messages}");
        }


        [Command("wake")]
        public async Task WakeAsync(CommandContext ctx)
        {
            ulong id = ctx.Channel.Id;
            await CheckChannelData(id);
            Console.Out.WriteLine("In command wake");
            if (Program.channelData[ctx.Channel.Id].IsAsleep)
            {
                Program.channelData[ctx.Channel.Id].IsAsleep = false;
                await ctx.RespondAsync("I'm awake now. Ready to process commands.");
            }
            else
            {
                await ctx.RespondAsync("I'm already awake. Ready to process commands.");
            }
        }

        [Command("sleep")]
        public async Task SleepAsync(CommandContext ctx)
        {
            ulong id = ctx.Channel.Id;
            await CheckChannelData(id);
            Console.Out.WriteLine("In command sleep");
            if (!Program.channelData[ctx.Channel.Id].IsAsleep)
            {
                Program.channelData[ctx.Channel.Id].IsAsleep = true;
                await ctx.RespondAsync("Going to sleep now. I won't respond to anything other than commands.");
            }
            else
            {
                await ctx.RespondAsync("I'm already asleep. I won't respond to anything other than commands.");
            }
        }


        static async Task CheckChannelData(ulong id)
        {
            if (!Program.channelData.ContainsKey(id))
            {
                Program.channelData[id] = new ChannelData(Program.hypernodes);
                await Program.SetGraph(id);
			}
        }
    }
}

public class GraphNodeData
{
    public string Type { get; set; }
    public string Name { get; set; }
    public List<string> Inputs { get; set; } = new List<string>();
    public List<string> Outputs { get; set; } = new List<string>();
    public string Description { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
}
public class KeyData
{
    [JsonProperty("key")]
    public string Key { get; set; }
    [JsonProperty("tokenCount")]
    public int TokenCount { get; set; }
    [JsonProperty("username")]
    public string Username { get; set; }
    [JsonProperty("paymentLink")]
    public string PaymentLink { get; set; }
}
