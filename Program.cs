using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System.Text;
using DSharpPlus.Entities;

namespace MyDiscordBot
{
	public class ChannelData
	{
		public List<string> Conversation { get; set; } = new List<string>();
		public string ConversationKey { get; set; }
		public bool IsAsleep { get; set; }
		public string PollingGuid { get; set; }
		public DateTime LastPollTime { get; set; }
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

			if (Config.graphObj == null) {
				await SetGraphAsync(Config.SelectedGraphUsername, Config.SelectedGraph);
				Console.Out.WriteLine("successfully set graph to " + Config.graphObj.Name);
			}
			await discord.ConnectAsync();
			await Task.Delay(-1);

		}

		static DiscordBotConfig LoadConfig () {
			var configString = File.ReadAllText("config.json");
			return JsonConvert.DeserializeObject<DiscordBotConfig>(configString);
		}


	public async static Task SetGraphAsync (string username, string graphName) {
			List<GraphNodeData> availableGraphs = await GetAvailableGraphsAsync(username);
			int index = availableGraphs.FindIndex((g) => graphName == g.Name);

			if (index >= 0) {
				Config.graphObj = availableGraphs[index];
				Config.SelectedGraphUsername = username;
			}
			else {
                Console.Out.WriteLine("could not find graph with name: " + graphName + ". availible graphs in user: " + username);
                foreach(GraphNodeData graph in availableGraphs) {
                    Console.Out.WriteLine(graph.Name);
                }
			}
		}
		private static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            Console.Out.WriteLine("Message recieved + " + e.Message);
            if (e.Author.IsBot || e.Message.Content.StartsWith('!'))
                return;

            if (!channelData.ContainsKey(e.Channel.Id))
            {
                channelData[e.Channel.Id] = new ChannelData();
            }
            if (!channelData[e.Channel.Id].IsAsleep)
            {

                if (Config.SelectedGraph != null)
                {
                    await RespondToMessageAsync(e.Channel.Id, e.Message.Content, e);
                }
                else
                {
                    if (string.IsNullOrEmpty(Config.AccountKey))
                    {
                        await e.Message.RespondAsync("You need to assign an account key to fund the bot.This will be in the bot's config file (Contact the admin)");
                        return;
                    }
                    await e.Message.RespondAsync("You need to assign a graph. This will be in the bot's config file (Contact the admin)" );
                }
            }
        }

        static async Task RespondToMessageAsync(ulong channelId, string message, MessageCreateEventArgs e)
        {
            if(Config.graphObj == null) {
                await SetGraphAsync(Config.SelectedGraphUsername, Config.SelectedGraph);
            }
            ChannelData data = channelData[channelId];
            GraphNodeData model = Config.graphObj;


            using var httpClient = new HttpClient();
            var request = new RunGraphRequest
            {
                GraphName = model.Name,
                InputData = new Dictionary<string, List<string>>()
            };
            model.Inputs.RemoveAll((s) => s == "+");

            if (data.Conversation == null)
            {
                data.Conversation = new List<string>();
            }
            var conversationHistory = data.Conversation;

            if (!string.IsNullOrEmpty(Config.SelectedGraphUsername))
            {
                request.GraphUserName = Config.SelectedGraphUsername;
            }

            if (!string.IsNullOrEmpty(Config.AccountKey)) 
            {
                request.FundingKey = Config.AccountKey; 
            }

            if (string.IsNullOrEmpty(data.PollingGuid))
            {
                data.PollingGuid = Guid.NewGuid().ToString();
            }
            request.PollingGuid = data.PollingGuid; // Added PollingGuid to request

            conversationHistory.Add($"[{e.Message.Id}] {e.Author.Username}#{e.Author.Discriminator} : {message}");
            List<string> simpleHistory = new List<string>();
            foreach (string realHistory in conversationHistory)
            {
                simpleHistory.Add(realHistory.Split(':', 2)[1]);
            }
            request.InputData.Add(model.Inputs[0], simpleHistory);

            if (model.Inputs.Count > 1)
            {
                if (string.IsNullOrEmpty(data.ConversationKey))
                {
                    data.ConversationKey = Guid.NewGuid().ToString();
                }

                string conversationKey = data.ConversationKey;
                request.InputData.Add(model.Inputs[1], new List<string>() { conversationKey });
            }

            if (model.Inputs.Count > 2)
            {
                request.InputData.Add(model.Inputs[2], conversationHistory);
            }

            // Call the rungraph API
            // Serialize the request object to a JSON string
            string jsonRequest = JsonConvert.SerializeObject(request);

            // Create a StringContent object with the JSON string and the appropriate content type
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Call the rungraph API
            var response = await httpClient.PostAsync($"{ApiBaseUrl}/rungraph", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.Out.WriteLine("Error: Unable to reach the server. Received status code: " + response.StatusCode);
                await e.Message.RespondAsync("Sorry, there was an error connecting to the server. Please try again later.");
                return;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var graphOutput = JsonConvert.DeserializeObject<List<string>>(jsonResponse);

            if (!string.IsNullOrEmpty(graphOutput[0]))
            {
                // Respond with the graph output
                var botMessage = await e.Message.RespondAsync(graphOutput[0]);

                // Add bot's username and message ID to the message. 
                Console.Out.WriteLine("Responding from graph with: " + graphOutput[0]);
                conversationHistory.Add($"[{botMessage.Id}] {discord.CurrentUser.Username}#{discord.CurrentUser.Discriminator} : {graphOutput[0]}");
            }

            // Respond with the graph output

            if (DateTime.UtcNow - data.LastPollTime > TimeSpan.FromMinutes(1))
            {
                PollForResultAsync(data, channelId, e);
            }
        }

        static async Task PollForResultAsync(ChannelData data, ulong channelId, MessageCreateEventArgs e)
        {
            data.LastPollTime = DateTime.UtcNow; // Update the last poll time
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{ApiBaseUrl}/pollresult?guid={data.PollingGuid}");

            if (!response.IsSuccessStatusCode)
            {
                Console.Out.WriteLine("Error: Unable to reach the server. Received status code: " + response.StatusCode);
                await PollForResultAsync(data, channelId, e);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var clientFunction = JsonConvert.DeserializeObject<ClientFunction>(jsonResponse);

            if (clientFunction != null)
            {
                await HandleClientFunction(clientFunction, channelId, e);

                if (!clientFunction.Finished)
                {
                    await PollForResultAsync(data, channelId, e);
                }
            }
        }


		private static async Task<List<GraphNodeData>> GetAvailableGraphsAsync (string username = null) {

			using var httpClient = new HttpClient();

			// Get the key associated with the current channel
			string key = Config.AccountKey;

			// If the username is null, use the key
			string requestUri = (username == null)
				? $"{ApiBaseUrl}/getgraphs?key={key}"
				: $"{ApiBaseUrl}/getgraphs?username={username}";

			var response = await httpClient.GetAsync(requestUri);
			response.EnsureSuccessStatusCode();

			string jsonResponse = await response.Content.ReadAsStringAsync();
            Console.Out.WriteLine("availible grpahs: ");
            Console.Out.WriteLine(jsonResponse);
			var graphsInfoList = JsonConvert.DeserializeObject<List<GraphNodeData>>(jsonResponse);

			return graphsInfoList;
		}


		private static async Task HandleClientFunction(ClientFunction function, ulong channelId, MessageCreateEventArgs e)
        {
            try
            {
                switch (function.Name.ToLower())
                {
                    case "say":
                        var sayText = function.Args[0][0];
                        var botMessage = await e.Message.RespondAsync(sayText);
                        channelData[channelId].Conversation.Add($"[{botMessage.Id}] {discord.CurrentUser.Username}#{discord.CurrentUser.Discriminator} : {sayText}");
                        break;

                    case "createembed":
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = function.Args[0][0],
                            Description = function.Args[0][1],
                            Color = new DiscordColor(function.Args[0][2]),
                        };

                        for (int i = 3; i < function.Args[0].Count; i += 2)
                        {
                            embed.AddField(function.Args[0][i], function.Args[0][i + 1]);
                        }

                        await e.Message.RespondAsync(embed: embed);
                        break;

                    case "changenickname":
                        var user = (await e.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == function.Args[0][0]);
                        var member = await e.Guild.GetMemberAsync(user.Id);
                        await member.ModifyAsync(x => x.Nickname = function.Args[0][1]);
                        break;

                    case "reaction":
                        var message = await e.Channel.GetMessageAsync(Convert.ToUInt64(function.Args[0][0]));
                        var emoji = DiscordEmoji.FromName(discord, function.Args[0][1]);

                        await message.CreateReactionAsync(emoji);
                        break;

                    case "muteuser":
                        var muteUser = (await e.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == function.Args[0][0]);
                        var muteRole = e.Guild.Roles.Values.FirstOrDefault(r => r.Name.ToLower() == "muted");

                        if (muteRole != null)
                            await muteUser.GrantRoleAsync(muteRole);
                        break;

                    case "unmuteuser":
                        var unmuteUser = (await e.Guild.GetAllMembersAsync()).FirstOrDefault(m => m.Username == function.Args[0][0]);
                        var unmuteRole = e.Guild.Roles.Values.FirstOrDefault(r => r.Name.ToLower() == "muted");

                        if (unmuteRole != null)
                            await unmuteUser.RevokeRoleAsync(unmuteRole);
                        break;

                    default:
                        Console.Out.WriteLine($"Unknown function: {function.Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"Error executing function: {function.Name}. Exception: {ex.Message}");
            }
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