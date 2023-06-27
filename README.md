# OpenHypernodesBot - A Hyperthetical Powered Discord Bot

Welcome to OpenHypernodesBot, a Discord bot that uses the power of Hyperthetical to enable deep integration of graphs into your Discord server.

## How to set up OpenHypernodesBot
To get OpenHypernodesBot running, you will need to follow these steps:

1. **Clone the repository**

   Use the `git clone` command to download the source code to your machine.

2. **Install Dependencies**

   OpenHypernodesBot is built with .NET Core. Make sure you have the .NET Core 3.1 SDK or later installed on your machine. Navigate to the project directory and run `dotnet restore` to install the necessary dependencies.

3. **Provide the configuration**

   The bot uses a `config.json` file for configuration. An example file should look like this:

```json
{
  "DiscordToken": "YourTokenHere",
  "SelectedGraphUsername": "YourSelectedGraphUsername",
  "SelectedGraph": "YourSelectedGraphName",
  "AccountKey": "YourAccountKey"
}
```

   Replace the placeholders with your actual information:

   - `YourTokenHere`: The token for your Discord Bot. You can get this from the Discord Developer Portal.
   - `YourSelectedGraphUsername`: The username associated with the graph on Hyperthetical.
   - `YourSelectedGraphName`: The name of the graph on Hyperthetical you wish to use.
   - `YourAccountKey`: Your Hyperthetical account key.

   Make sure the `config.json` file is placed in the root directory of the project.

4. **Build and run the bot**

   You can build the bot by running `dotnet build`. To run the bot, use the `dotnet run` command. 

## Features

OpenHypernodesBot includes several unique features powered by Hyperthetical. The bot responds to messages in channels and interacts with the Hyperthetical API to enable dynamic responses based on defined graphs. 

The bot can handle a variety of tasks, such as sending messages, creating rich embeds, changing user nicknames, reacting to messages, and even muting or unmuting users based on the output of the graph.

Please note, many of these features depend on the server permissions assigned to the bot. Make sure to give the bot appropriate permissions for the best experience.

## Contributing

OpenHypernodesBot is an open-source project, and contributions are welcomed. Whether it's a bug report, new feature, correction, or additional documentation, we greatly appreciate all contributions.

## License

OpenHypernodesBot is under a [MIT license](LICENSE) that supports commercial use, distribution, modification, private use, and liability.

## Contact 

For more assistance, contact the project maintainers or open an issue on GitHub.
