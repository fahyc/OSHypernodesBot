using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System.Text;
using DSharpPlus.Entities;
using System.Threading.Channels;

namespace MyDiscordBot
{

	public class ChannelData : Hyperthetical.User
	{
		public List<string> Conversation { get; set; } = new List<string>();
		public bool IsAsleep { get; set; }
		public DateTime LastPollTime { get; set; }
		public MessageCreateEventArgs eventArgs { get; set; }

		public ChannelData (Hyperthetical.Client client) : base(client) {
			Conversation = new List<string>();
		}

	}

	public class DiscordBotConfig
	{
		public string DiscordToken { get; set; }
		public string SelectedGraphUsername { get; set; }
		public string SelectedGraph { get; set; }
		public string AccountKey { get; set; }
		public GraphNodeData graphObj { get; set; }
	}

	public static class Program
	{
		public static Dictionary<ulong, ChannelData> channelData = new Dictionary<ulong, ChannelData>();
		public static DiscordBotConfig Config;
		public static readonly string ApiBaseUrl = "https://www.hyperthetical.dev";
		static DiscordClient discord;
		public static Hyperthetical.Client hypernodes;

		static async Task Main (string[] args) {
			Config = LoadConfig();

			DiscordConfiguration dConfig = new DiscordConfiguration {
				Token = Config.DiscordToken,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged |
					DiscordIntents.GuildMessages |
					DiscordIntents.MessageContents
			};

			discord = new DiscordClient(dConfig);
			discord.UseInteractivity(new InteractivityConfiguration());

			CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration {
				StringPrefixes = new[] { "!" },
			};

			CommandsNextExtension commands = discord.UseCommandsNext(commandsConfig);
			discord.MessageCreated += OnMessageCreated;
			commands.RegisterCommands<Commands>();


			hypernodes = new Hyperthetical.Client(Config.AccountKey);
			hypernodes.AddClientFunction("say", sayFunction);
			hypernodes.AddClientFunction("embed", embedFunction);
			hypernodes.AddClientFunction("changeNickname", changeNicknameFunction);
			hypernodes.AddClientFunction("reaction", reactionFunction);
			hypernodes.AddClientFunction("mute", muteFunction);
			hypernodes.AddClientFunction("unmute", unmuteFunction);


			await discord.ConnectAsync();
			await Task.Delay(-1);

		}

		static DiscordBotConfig LoadConfig () {
			var configString = File.ReadAllText("config.json");
			return JsonConvert.DeserializeObject<DiscordBotConfig>(configString);
		}

		public static async Task SetGraph (ulong key) {
			await channelData[key].SetGraphAsync(Config.SelectedGraph, Config.SelectedGraphUsername);
			List<Hyperthetical.GraphNodeData> graphs = await hypernodes.GetAvailableGraphsAsync(Config.AccountKey, Config.SelectedGraphUsername);
			Hyperthetical.GraphNodeData? graph = graphs.Find((item) => { return item.Name == Config.SelectedGraph; });
			if (graph != null && channelData[key].eventArgs != null) {
				await channelData[key].eventArgs.Message.RespondAsync(graph.Description);
			}
		}

		private static async Task OnMessageCreated (DiscordClient sender, MessageCreateEventArgs e) {
			Console.Out.WriteLine("Message recieved + " + e.Message);
			if (e.Author.IsBot || e.Message.Content.StartsWith('!'))
				return;

			if (!channelData.ContainsKey(e.Channel.Id)) {
				channelData[e.Channel.Id] = new ChannelData(hypernodes);
				channelData[e.Channel.Id].eventArgs = e;
				await SetGraph(e.Channel.Id);
				return;
			}
			if (!channelData[e.Channel.Id].IsAsleep) {
				var channel = channelData[e.Channel.Id];
				channel.eventArgs = e;
				var conversationHistory = channel.Conversation;
				var message = e.Message.Content;

				if (channel.ready()) {

					conversationHistory.Add($"[{e.Message.Id}] {e.Author.Username}#{e.Author.Discriminator}:{message}");

					List<string> simpleHistory = new List<string>();
					foreach (string realHistory in conversationHistory) {
						simpleHistory.Add(realHistory.Split(':', 2)[1]);
					}
					string response = await channel.ChatAsync(simpleHistory, new List<List<string>>() { conversationHistory });
					if (!string.IsNullOrEmpty(response)) {
						await e.Message.RespondAsync(response);
					}
				}
				else {
					await e.Message.RespondAsync("The bot is misconfigured, you need to add funding key and graph name in the config file.");
					return;
				}
			}
		}

		static async Task sayFunction (List<List<string>> args, Hyperthetical.User userData) {
			ChannelData? data = userData as ChannelData;

			var sayText = args[0][0];
			var botMessage = await data.eventArgs.Message.RespondAsync(sayText);
			data.Conversation.Add($"[{botMessage.Id}] {discord.CurrentUser.Username}#{discord.CurrentUser.Discriminator} : {sayText}");
		}

		static async Task embedFunction (List<List<string>> args, Hyperthetical.User userData) {

			ChannelData? data = userData as ChannelData;
			var embed = new DiscordEmbedBuilder {
				Title = args[0][0],
				Description = args[0][1],
				Color = new DiscordColor(args[0][2]),
			};

			for (int i = 3; i < args[0].Count; i += 2) {
				embed.AddField(args[0][i], args[0][i + 1]);
			}

			await data.eventArgs.Message.RespondAsync(embed: embed);
		}
		static async Task changeNicknameFunction (List<List<string>> args, Hyperthetical.User userData) {
			ChannelData? data = userData as ChannelData;
			var user = (await data.eventArgs.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == args[0][0]);
			var member = await data.eventArgs.Guild.GetMemberAsync(user.Id);
			await member.ModifyAsync(x => x.Nickname = args[0][1]);
		}
		static async Task reactionFunction (List<List<string>> args, Hyperthetical.User userData) {

			ChannelData? data = userData as ChannelData;
			var message = await data.eventArgs.Channel.GetMessageAsync(Convert.ToUInt64(args[0][0]));
			var emoji = DiscordEmoji.FromName(discord, args[0][1]);

			await message.CreateReactionAsync(emoji);
		}
		static async Task muteFunction (List<List<string>> args, Hyperthetical.User userData) {
			ChannelData? data = userData as ChannelData;
			var muteUser = (await data.eventArgs.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == args[0][0]);
			var muteRole = data.eventArgs.Guild.Roles.Values.FirstOrDefault(r => r.Name.ToLower() == "muted");

			if (muteRole != null)
				await muteUser.GrantRoleAsync(muteRole);
		}

		static async Task unmuteFunction (List<List<string>> args, Hyperthetical.User userData) {
			ChannelData? data = userData as ChannelData;

			var unmuteUser = (await data.eventArgs.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == args[0][0]);
			var unmuteRole = data.eventArgs.Guild.Roles.Values.FirstOrDefault(r => r.Name.ToLower() == "muted");

			if (unmuteRole != null)
				await unmuteUser.RevokeRoleAsync(unmuteRole);
		}
    }
}
public class RunGraphRequest
{
    public string GraphName { get; set; }
    public string GraphUserName { get; set; }
    public string FundingKey { get; set; }
    public Dictionary<string, List<string>> InputData { get; set; }
    public string PollingGuid { get; set; }
}

public class ClientFunction
{
    public string Name { get; set; }
    public List<List<string>> Args { get; set; }
    public bool Finished { get; set; }
}