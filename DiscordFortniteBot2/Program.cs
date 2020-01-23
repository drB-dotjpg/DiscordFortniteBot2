using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DiscordFortniteBot2
{
    class Program
    {
        #region Runs on Startup

        static void Main(string[] args) => new Program();

        string inputToken;
        string inputServerName;

        bool debug = false;

        Program() //runs on startup: Gets token and server data before logging into discord.
        {
            //check the token data stored on the machine
            string configFile = "config.txt";
            if (!File.Exists(configFile) || File.ReadAllLines(configFile).Length < 2) //if the config file does not exist then create it.
            {
                File.Create(configFile).Close();
                File.WriteAllText(configFile, "EDIT TOKEN\nEDIT SERVER");
            }

            string[] configLines = @File.ReadAllLines(configFile); //get the current token and server name from the file
            inputToken = configLines[0];
            inputServerName = @configLines[1];

            Console.WriteLine($"The token is {inputToken.Substring(0, 10)}... [Press T to change].\n" +
                $"The server is named {inputServerName} [Press S to change].\n" +
                $"Press Enter to log into discord.\n" +
                $"Press D to Login with debug on.\n");

            bool loop = true;
            while (loop)
            {
                Console.WriteLine("Awaiting input:");
                switch (Console.ReadKey().Key) //get keypress from user
                {
                    case ConsoleKey.T: //if the key pressed is T then change the token
                        Console.Write("- Enter Token: ");
                        string newToken = Console.ReadLine();
                        configLines[0] = newToken;
                        inputToken = newToken;
                        break;

                    case ConsoleKey.S: //if the key pressed is S then change the server name
                        Console.Write("- Enter Server Name (case sensitive): ");
                        string newServerName = Console.ReadLine();
                        configLines[1] = newServerName;
                        inputServerName = newServerName;
                        break;

                    case ConsoleKey.D: //if the key pressed is D then enable debug and login
                        Console.Write("- Debug on.");
                        debug = true;
                        goto Login;

                    case ConsoleKey.Enter: //if the key pressed is enter then log into discord (done after switch statement)
                        Console.Write("Enter- Logging in.");
                        goto Login;

                    Login:
                        File.WriteAllLines(configFile, configLines); //save any possible changes to the config file
                        loop = false;
                        break;
                }
            }

            //The rest of the program methods will be called here (for better error handling)

            Login().GetAwaiter().GetResult(); //start login sequence
            Pregame().GetAwaiter().GetResult(); //start pregame sequence
        }

        #endregion

        #region Discord Related

        public DiscordSocketClient _client;
        private IServiceProvider _services;
        public SocketGuild _server;
        bool ready = false;

        async Task Login()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig { ExclusiveBulkDelete = true }); //create discord client
            _services = new ServiceCollection().AddSingleton(_client).BuildServiceProvider();

            _client.Log += Log; //subscribe to discord events
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;
            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, inputToken);  //login using token & start
            await _client.StartAsync();

            while (!ready) await Task.Delay(30); //wait for login to finish

            _server = _client.Guilds.First(g => g.Name == inputServerName); //get the server by name
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            ready = true;
            return Task.CompletedTask;
        }

        private Task MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (_server.GetUser(arg3.UserId).IsBot) return;

            switch (phase)
            {
                case Phase.Pregame:
                    HandlePregameReaction(arg3);
                    break;
            }
        }

        #endregion

        #region Pregame

        Phase phase = Phase.Pregame;
        SocketTextChannel channel;

        List<Player> players = new List<Player>();

        async Task Pregame()
        {
            Console.WriteLine("Entering pregame phase.");

            //Get a channel to post in

            string channelName = "fortnite-bot-2";
            try
            {
                Console.WriteLine($"Attempting to get the channel {channelName} in {_server.Name}.");

                channel = _server.TextChannels.First(c => c.Name == channelName); //try to get the channel (throws if none is found).

                Console.WriteLine("Channel found.");

                await channel.DeleteMessagesAsync(await channel.GetMessagesAsync().FlattenAsync()); //delete preexisting messages.
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Channel not found. Attempting to create one.");

                var newChannel = await _server.CreateTextChannelAsync(channelName); //if one is not found, attempt to create one (assuming perms are not an issue right now).
                OverwritePermissions permissions = new OverwritePermissions(addReactions: PermValue.Deny, sendMessages: PermValue.Deny);
                await newChannel.AddPermissionOverwriteAsync(_server.EveryoneRole, permissions);

                channel = _server.GetTextChannel(newChannel.Id); //you can't convert RestTextChannel to SocketTextChannel for some reason.
            }

            //await PostMap(channel, new Map());

            //Prompt users to join

            try
            {
                await channel.SendFileAsync("FortniteBot2.png", null); //try to send the epic logo (c# is weird with this)
            }
            catch (FileNotFoundException)
            {
                string imageDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + Path.DirectorySeparatorChar; //try again
                await channel.SendFileAsync(imageDir + "FortniteBot2.png", null);
            }

            var joinPrompt = await channel.SendMessageAsync($"> Click {Emotes.joinGame} to hop on the Battle Bus.");
            await joinPrompt.AddReactionAsync(Emotes.joinGame);

            int seconds = 15;

            var usersJoinedMessage = await channel.SendMessageAsync($"`Starting...`");    //post the users joined message (And has the timer)

            while (seconds > 0) //while the timer is running
            {
                await Task.Delay(1000); //wait 1 second

                if (players.Count >= 2 || debug) seconds--; //only move timer if there are 2 or more players (or debug is on)

                string joinedPlayers = GetPlayersJoined();
                string timeLeft = players.Count >= 2 || debug
                    ? seconds / 60 + ":" + (seconds % 60).ToString("00")
                    : "*Starts when 2 or more players have joined.*";

                await usersJoinedMessage.ModifyAsync(m => m.Content = $"> **Players Joined**: { joinedPlayers }\n" +
                    $" > **Time Left**: { timeLeft }");
            }
        }

        void HandlePregameReaction(SocketReaction reaction)
        {
            if (reaction.Emote.Name == Emotes.joinGame.Name) //if the reaction is the one needed to join game.
            {
                SocketUser reactionUser = _server.GetUser(reaction.UserId); //get the discord user.
                if (!HasPlayerJoined(reactionUser)) //if they have not joined the game.
                {
                    players.Add(new Player(reactionUser)); //add them to the game.
                    Console.WriteLine($"Added {reactionUser.Username} to the game.");
                }
            }
        }

        async Task PostMap(SocketTextChannel channel, Map map)
        {
            string emoteMap = "";
            for (int x = 0; x < Map.mapWidth; x++)
            {
                for (int y = 0; y < Map.mapHeight; y++)
                {
                    switch (map.mapGrid[x, y].type)
                    {
                        case TileType.Grass:
                            emoteMap += "🌳";
                            break;
                        case TileType.Water:
                            emoteMap += "🌊";
                            break;
                        case TileType.House:
                            emoteMap += "🏠";
                            break;
                    }
                }
                emoteMap += "\n";
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.AddField("Map", emoteMap);
            await channel.SendMessageAsync("", false, builder.Build());
        }

        string GetPlayersJoined()
        {
            string builder = "";

            if (players.Count > 0) //if there are more than 0 players
            {
                foreach (Player player in players) //for each player in the game, add them to the list.
                {
                    builder += "`" + player.discordUser.Username + "`";
                }
            }
            else //if there are no players
            {
                builder = "None";
            }

            return builder;
        }

        bool HasPlayerJoined(SocketUser user)
        {
            foreach (Player player in players) //for each player in the game.
            {
                if (player.discordUser == user) //if the user is found in the list of players.
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}