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
        static void Main(string[] args) => new Program();

        string inputToken;
        string inputServerName;

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
                $"Press Enter to log into discord.\n");

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

                    case ConsoleKey.Enter: //if the key pressed is enter then log into discord (done after switch statement)
                        Console.WriteLine("Enter- Logging in.");
                        File.WriteAllLines(configFile, configLines); //save any possible changes to the config file
                        loop = false;
                        break;
                }
            }

            //The rest of the program methods will be called here (for better error handling)

            Login().GetAwaiter().GetResult(); //start login sequence
            Pregame().GetAwaiter().GetResult(); //start pregame sequence
        }

        public DiscordSocketClient _client;
        private IServiceProvider _services;
        public SocketGuild _server;
        bool ready = false;

        async Task Login()
        {
            _client = new DiscordSocketClient(); //create discord client
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
                    await HandlePregameReaction(arg3);
                    break;
            }
        }


        //Pregame stuffs

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

            var usersJoinedMessage = await channel.SendMessageAsync($"> { GetPlayersJoined() }"); //post the users joined message

            while(true) //TODO: Don't make this infinite.
            {
                await Task.Delay(1000);
                await usersJoinedMessage.ModifyAsync(m => m.Content = $"> { GetPlayersJoined() }");

            }
        }

        async Task HandlePregameReaction(SocketReaction reaction)
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

        string GetPlayersJoined()
        {
            string builder = "";

            foreach (Player player in players) //for each player in the game, add them to the list.
            {
                builder += ", " + player.discordUser.Username;
            }

            if (builder.Length >= 2) builder = builder.Substring(2); //trim the start off

            return "Players Joined: " + builder;
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
    }
}