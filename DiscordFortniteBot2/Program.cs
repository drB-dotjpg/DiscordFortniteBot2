using Discord.WebSocket;
using Discord;
using Discord.Rest;
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
                $"Press D to login with debug on.\n");

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
                        Console.Write("- Debug on.\n");
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

            phase = Phase.Ingame;

            InGame().GetAwaiter().GetResult(); //start Ingame sequence
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
            if (_server.GetUser(arg3.UserId).IsBot) return; //Task.CompletedTask;

            switch (phase)
            {
                case Phase.Pregame:
                    HandlePregameReaction(arg3);
                    break;
                case Phase.Ingame:
                    await HandleIngameReaction(arg3);
                    break;
            }

            //return Task.CompletedTask;
        }

        #endregion

        #region Pre Game

        Phase phase = Phase.Pregame;
        SocketTextChannel channel;

        List<Player> players = new List<Player>();

        bool playerLimitHit = false;

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

            int seconds = !debug ? 180 : 5;

            var usersJoinedMessage = await channel.SendMessageAsync($"`Starting...`");    //post the users joined message (And has the timer)

            while (seconds > 0) //while the timer is running
            {
                await Task.Delay(1000); //wait 1 second

                if (players.Count >= 2 || debug) seconds--; //only move timer if there are 2 or more players (or debug is on)

                if (playerLimitHit && seconds > 20) seconds = 15;

                string joinedPlayers = GetPlayersJoined();
                string timeLeft = players.Count >= 2 || debug
                    ? seconds / 60 + ":" + (seconds % 60).ToString("00")
                    : "*Starts when 2 or more players have joined.*";

                await usersJoinedMessage.ModifyAsync(m => m.Content = $"> **Players Joined**: { joinedPlayers }\n\n" +
                    $" > **Time Left**: { timeLeft }");
            }
        }

        void HandlePregameReaction(SocketReaction reaction)
        {
            if (reaction.Emote.Name == Emotes.joinGame.Name && !playerLimitHit) //if the reaction is the one needed to join game and there are less than 8 players already joined.
            {
                SocketUser reactionUser = _server.GetUser(reaction.UserId); //get the discord user.
                if (!HasPlayerJoined(reactionUser)) //if they have not joined the game.
                {
                    Emoji playerIcon;
                    do
                    {
                        playerIcon = Emotes.playerIcons[new Random().Next(Emotes.playerIcons.Length)];
                    } while (IsIconTaken(playerIcon));

                    players.Add(new Player(reactionUser, playerIcon)); //add them to the game.

                    reactionUser.SendMessageAsync($"You have been added to the game! You are playing as {playerIcon}.");

                    Console.WriteLine($"Added {reactionUser.Username} to the game.");

                    if (players.Count >= 8) playerLimitHit = true;
                }
            }
        }

        string GetPlayersJoined()
        {
            string builder = players.Count + "/8 slots filled\n";

            if (players.Count > 0) //if there are more than 0 players
            {
                foreach (Player player in players) //for each player in the game, add them to the list.
                {
                    builder += player.icon + " - `" + player.discordUser.Username + "`";
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

        bool IsIconTaken(Emoji icon) //similar to HasPlayerJoined
        {
            foreach (Player player in players)
            {
                if (player.icon == icon)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region In Game

        Map map;
        int turn = 0;

        async Task InGame()
        {
            Console.WriteLine("Generating map...");
            map = new Map(debug); //generate map

            while (players.Count == 1 || debug)
            {
                int seconds = 20;
                List<IUserMessage> timerMessages = new List<IUserMessage>();

                foreach (Player player in players) //send turn breifings
                {
                    var mapMessage = await player.discordUser.SendMessageAsync(null, false, GetTurnBreifing(player)) as RestUserMessage; //send turn breifing to all players
                    timerMessages.Add(mapMessage);
                    player.currentMessages.Add(mapMessage); //add it to the active messages (only these accept reactions)

                    string actionPrompt = "Choose an action: 👣=Walk | ✋=Use | 💼=Loot | 🔄=Equip | 🗑️=Drop";
                    var actionMessage = await player.discordUser.SendMessageAsync(actionPrompt) as RestUserMessage;
                    await actionMessage.AddReactionsAsync(Emotes.actionEmojis);
                    player.currentMessages.Add(actionMessage);
                }

                while (seconds >= 0)
                {
                    foreach(IUserMessage timer in timerMessages)
                        await timer.ModifyAsync(m => m.Content = $"Time Remaining: `:{seconds.ToString("00")}`");

                    await Task.Delay(1000);
                    seconds--;
                }
                
            }

            await Task.Delay(-1);
        }

        Embed GetTurnBreifing(Player player)
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.AddField("Map", map.GetMapAreaString(player.x, player.y, players), true);

            string key =
                "🟩 - Grass\n" +
                "🟦 - Water\n(Can't shoot in)\n" +
                "🟫 - Tree \n(Can loot)\n" +
                "🟨 - Chest\n(Can loot)\n" +
                "🟥 - Wall \n(Breakable)\n";

            builder.AddField("Key", key, true);

            int shieldBarAmount = player.shield / 10;
            string shieldBar = string.Concat(Enumerable.Repeat("🟦", shieldBarAmount)) + string.Concat(Enumerable.Repeat("⬛", 10 - shieldBarAmount)); //Fill bar with blue squares and gray squares depending on the player's shield
            builder.AddField("Shield", shieldBar);

            int healthBarAmount = player.health / 10;
            string healthBar = string.Concat(Enumerable.Repeat("🟩", healthBarAmount)) + string.Concat(Enumerable.Repeat("⬛", 10 - healthBarAmount));
            builder.AddField("Health", healthBar);

            return builder.Build();
        }

        async Task HandleIngameReaction(SocketReaction reaction)
        {
            Emoji emote = new Emoji(reaction.Emote.Name);
            Player player = GetPlayerById(reaction.UserId);

            //if the message is the last active message, then continue
            if (player.currentMessages.Last().Id != reaction.MessageId) return;

            if (reaction.Emote.Name == Emotes.backButton.Name) //if a back button was pressed
            {
                var channel = await player.discordUser.GetOrCreateDMChannelAsync();
                var message = await channel.GetMessageAsync(reaction.MessageId) as RestUserMessage;
                await message.DeleteAsync();
                player.currentMessages.Remove(player.currentMessages.Last());
            }

            for (int i = 0; i < Emotes.actionEmojis.Length; i++) //scan for action emotes
            {
                if (emote.Name == Emotes.actionEmojis[i].Name) //if the emote matches
                {
                    player.turnAction = (Action)i; //the emote array pos should match the enum

                    if (i == 0 || i == 1 && player.inventory[player.equipped].type == ItemType.Weapon) //if i is 0 (move) or 1 (use) and equipped with weapon 
                    {
                        var message = await player.discordUser.SendMessageAsync("Select Direction:") as RestUserMessage; //follow up asking for a direction
                        await message.AddReactionsAsync(Emotes.arrowEmojis);
                        await message.AddReactionAsync(Emotes.backButton);
                        player.currentMessages.Add(message);
                    }

                    return;
                }
            }

            for (int i = 0; i < Emotes.arrowEmojis.Length; i++)
            {
                if (emote == Emotes.arrowEmojis[i])
                {
                    player.turnDirection = (Direction)i;

                    return;
                }
            }

            for (int i = 0; i < Emotes.slotEmojis.Length; i++)
            {
                if (emote == Emotes.slotEmojis[i])
                {
                    player.turnIndex = i;

                    return;
                }
            }

        }

        #endregion

        Player GetPlayerById(ulong id)
        {
            return players.First(x => x.discordUser.Id == id);
        }
    }
}