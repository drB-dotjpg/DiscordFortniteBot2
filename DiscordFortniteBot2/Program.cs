using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            _client.ReactionRemoved += ReactionRemoved;
            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, inputToken);  //login using token & start
            await _client.StartAsync();

            while (!ready) await Task.Delay(30); //wait for login to finish

            _server = _client.Guilds.First(g => g.Name == inputServerName); //get the server by name
        }

        async Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (phase == Phase.Ingame)
                await HandleIngameReactionRemoval(arg3);
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
                    await HandlePregameReaction(arg3);
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

            int seconds = !debug ? 180 : 6;

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

                await usersJoinedMessage.ModifyAsync(m => m.Content = $"**Players Joined**: {players.Count}/8 slots filled\n{ joinedPlayers }" +
                    $"\n**Time Left**: { timeLeft }");
            }
        }

        async Task HandlePregameReaction(SocketReaction reaction)
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

                    var joinMessage = await reactionUser.SendMessageAsync($"You have been added to the game! You are playing as {playerIcon}.\n" +
                        $"Press {Emotes.leaveGame} to leave.") as RestUserMessage;

                    await joinMessage.AddReactionAsync(Emotes.leaveGame);

                    Console.WriteLine($"Added {reactionUser.Username} to the game.");

                    if (players.Count >= 8) playerLimitHit = true;
                }
            }
            else if (reaction.Emote.Name == Emotes.leaveGame.Name && HasPlayerJoined(_server.GetUser(reaction.UserId)))
            {
                Player player = GetPlayerById(reaction.UserId);
                await player.discordUser.SendMessageAsync("You have left the game.");
                players.Remove(player);

                Console.WriteLine($"{player.discordUser.Username} left the game.");
            }
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
        int turn = 1;
        const int TURN_SECONDS = 40;
        const int INACTIVIY_LIMIT = 2;
        const int SUPPLY_DROP_DELAY = 20; //Amount of turns before supply drops start appearing
        int supplyDropCooldown = 5; //Turns between each supply drop, once it reaches 5 a supply drop will drop somewhere

        RestUserMessage spectatorMesasge;
        List<Player> deadPlayers = new List<Player>();

        async Task InGame()
        {
            Console.WriteLine("Generating map...");
            map = new Map(debug); //generate map

            await channel.DeleteMessagesAsync(await channel.GetMessagesAsync().FlattenAsync());
            spectatorMesasge = await channel.SendMessageAsync("Starting game...");

            while (players.Count == 1 || debug)
            {
                await spectatorMesasge.ModifyAsync(x => x.Content = GetSpectatorMessage());

                int seconds = TURN_SECONDS; //set the turn timer

                foreach (Player player in players) //
                {
                    player.currentBriefing = player.briefing;
                    player.briefing = "";

                    player.turnMessage = await player.discordUser.SendMessageAsync(null, false, GetTurnBriefing(player)) as RestUserMessage; //send turn briefing

                    player.currentMessages.Add(player.turnMessage); //add it to the active messages (only these accept reactions)

                    //send the reactions to the players
                    string actionPrompt = "Choose an action: (Remove reaction to pick a different action)\n👣 Walk | ✋ Use Item | 🔨 Build Wall | 💼 Loot Chest/Tree | 🔄 Equip Item | 🗑️ Drop Item | ℹ World map/Map key/Player list.";
                    var actionMessage = await player.discordUser.SendMessageAsync(actionPrompt) as RestUserMessage;
                    await actionMessage.AddReactionsAsync(Emotes.actionEmojis);
                    await actionMessage.AddReactionAsync(Emotes.infoButton);
                    player.currentMessages.Add(actionMessage); //add it to the players current messages so the reaction handler accepts this message
                }


                while (seconds >= 0 && !ArePlayersReady()) //wait for timer to finish or all players to be ready
                {
                    if (seconds == 10)
                    {
                        foreach (Player player in players)
                            await player.turnMessage.ModifyAsync(e => e.Embed = GetTurnBriefing(player, true));
                    }

                    await Task.Delay(1000);
                    seconds--;
                }

                await ProcessEndOfTurn(); //process the end of turn (this comment helped)

                map.UpdateStorm(turn);

                if(turn > SUPPLY_DROP_DELAY) //Check if the delay has passed
                {
                    supplyDropCooldown++;
                    if(supplyDropCooldown >= 5) //Drop one every 5 turns
                    {
                        supplyDropCooldown = 0;
                        map.DropSupplyDrop();
                    }
                }

                turn++;
            }

            await Task.Delay(-1);
        }

        async Task ProcessEndOfTurn() //tldr: player list loops
        {
            foreach (Player player in players) //check for inactivity
            {
                if (!player.ready) //if the player is not ready (turn timer ran out)
                {
                    player.inactiveTurns++;

                    if (player.inactiveTurns >= INACTIVIY_LIMIT) //if they're at the inactivity limit
                    {
                        player.health = 0; //bye bye
                    }
                }
            }

            foreach (Player player in players) //start processing turn data, items take priority so they're first
            {
                if (player.turnAction == Action.Use && player.health != 0)
                {
                    ItemType itemType = player.inventory[player.equipped].type;

                    player.stats.totalItemsUsed++;

                    switch (itemType)
                    {
                        case ItemType.Empty:
                            break; //haha jeff put return here what a dummy //shut up jpg //my b

                        case ItemType.Weapon:
                            HandleShootAction(player, player.equipped);
                            player.inventory[player.equipped].ammo--;
                            if (player.inventory[player.equipped].ammo <= 0)
                            {
                                player.inventory[player.equipped] = new Item();
                            }
                            break;

                        case ItemType.Trap:
                            player.PlaceTrap(map, player.equipped);
                            break;

                        case ItemType.Health:
                        case ItemType.Shield:
                        case ItemType.HealAll:
                            player.UseHealingItem(player.equipped);
                            break;
                    }
                }

                if (map.mapGrid[player.y, player.x].Type == TileType.Storm) //also standing in the storm is cringe
                {
                    player.TakeDamage(15);
                    player.briefing += "\n" + "Took 15 damage in the storm.";
                }
            }

            foreach (Player player in players.ToList()) //make sure players are not dead, so they cannot continue doing stuff
            {
                if (player.health <= 0)
                {
                    foreach (Player p in players)
                        await p.discordUser.SendMessageAsync(embed:
                        new EmbedBuilder() { Title = player.discordUser.Username + " has died!" }.WithColor(Color.Gold).Build());

                    map.mapGrid[player.y, player.x] = new Map.Tile(player.inventory);

                    await player.discordUser.SendMessageAsync("**Final stats:**\n" + player.stats.GetAllStats()); //send them stats

                    deadPlayers.Add(player); //join the club
                    players.Remove(player); //they're out bye bye
                }
            }

            foreach (Player player in players) //then process building (so players cannot stand on walls that get built on them)
            {
                if (player.turnAction == Action.Build)
                {
                    map = player.Build(map);
                }
            }

            foreach (Player player in players) //process the rest of the turn data
            {
                player.ready = false; //reset player ready state

                switch (player.turnAction)
                {
                    case Action.Move:
                        player.Move(map);
                        break;

                    case Action.Loot:
                        if (map.mapGrid[player.y, player.x].Type == TileType.Chest) //if the player is on a chest
                        {
                            if (player.Loot(map.mapGrid[player.y, player.x].Items[player.turnIndex])) //true if player could loot (had empty inventory slots)
                            {
                                map.mapGrid[player.y, player.x].Items[player.turnIndex] = new Item(); //make the chest slot empty (since the item was taken you know?)

                                if (map.mapGrid[player.y, player.x].IsEmpty()) map.mapGrid[player.x, player.y] = new Map.Tile(TileType.Grass); //remove the chest if its empty
                            }
                        }
                        else if (map.mapGrid[player.y, player.x].Type == TileType.Tree) //if the player is on a tree (they climbed it)
                        {
                            player.materials += 10; //give them materials
                            player.briefing += "\n" + "You chopped down a tree and got +10 materials.";
                            player.stats.totalTreesCut++;
                            map.mapGrid[player.y, player.x] = new Map.Tile(TileType.Grass); //the tree turns into grass
                        }
                        break;

                }

                if (turn == map.GetStormDelay()) //warn players about the storm
                {
                    player.briefing += "\n" + $"The storm's eye has started shrinking. It will fully close in {map.GetStormSpeed()} turns. Check your world map ({Emotes.infoButton} button) to see storm progress.";
                }
                else if (map.GetStormDelay() - 5 <= turn && turn < map.GetStormDelay())
                {
                    int turnsTillTheThingHappens = map.GetStormDelay() - turn;
                    string plural = turnsTillTheThingHappens > 1 ? "s" : "";
                    player.briefing += "\n" + $"The storm will appear in {turnsTillTheThingHappens} turn{plural}.";
                }

                player.stats.totalTurnsAlive++;
            }
        }

        Embed GetTurnBriefing(Player player, bool timeWarning = false)
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.AddField("Map", map.GetMapAreaString(player.x, player.y, players)); //add map

            //add health and shield bars
            int shieldBarAmount = player.shield / 10;
            string shieldBar = string.Concat(Enumerable.Repeat("🟦", shieldBarAmount)) + string.Concat(Enumerable.Repeat("⬛", 10 - shieldBarAmount)); //Fill bar with blue squares and gray squares depending on the player's shield
            builder.AddField($"Shield: {player.shield}", shieldBar, true);

            int healthBarAmount = player.health / 10;
            string healthBar = string.Concat(Enumerable.Repeat("🟩", healthBarAmount)) + string.Concat(Enumerable.Repeat("⬛", 10 - healthBarAmount));
            builder.AddField($"Health: {player.health}", healthBar, true);

            builder.AddField("Materials", player.materials); //Materials are easy

            builder.AddField("Inventory", GetInventoryString(player)); //add Inventory

            string link;
            TileType type = map.mapGrid[player.y, player.x].Type;
            switch (type)
            {
                case TileType.Chest:
                case TileType.Tree:
                    link = "near a"; break;
                case TileType.Water:
                    link = "in"; break;
                case TileType.Storm:
                    link = "in the"; break;
                default:
                    link = "on"; break;
            }

            string briefing = "You are standing " + link + " " + map.mapGrid[player.y, player.x].Type.ToString().ToLower() + ".";
            briefing += player.currentBriefing;
            builder.AddField("Briefing", briefing);

            if (timeWarning)
            {
                builder.WithFooter("10 second warning! Please finish your turn.");
            }
            else if (player.inactiveTurns > 0)
            {
                string plural = player.inactiveTurns > 1 ? "s" : "";
                builder.WithFooter($"WARNING: You've been inactive for {player.inactiveTurns} turn{plural}. To keep the pace of the game for the other players, you will be kicked if you reach {INACTIVIY_LIMIT} inactive turns.");
            }

            return builder.Build();
        }

        Embed GetHelpMessage()
        {
            EmbedBuilder builder = new EmbedBuilder();

            string key =
                "🟩 - Grass\n" +
                "🟦 - Water\n(Can't use items while standing in)\n" +
                "🟫 - Tree \n(Can loot for materials)\n" +
                "🟨 - Chest\n(Can loot for items)\n" +
                "🟥 - Wall \n(Breakable with weapons)\n";

            builder.AddField("Map Key", key); //Show the map key

            string playersInGame = GetPlayersJoined(showReady: true);

            builder.AddField("Players", playersInGame); //Show the player list

            return builder.Build();
        }

        Embed GetLootMessage(Player player)
        {
            EmbedBuilder builder = new EmbedBuilder();

            List<Item> items = map.mapGrid[player.y, player.x].Items.ToList(); //get items in the chest the player is standing on
            int index = 1;
            string s = "";

            foreach (Item item in items) //add each item discription into a string
            {
                string pluralUses = item.ammo > 1 ? "uses" : "use"; //don't have it say '1 uses' that is cringe

                switch (item.type)
                {
                    case ItemType.Weapon:
                        s += $"{item.name} `Weapon. Deals {item.effectVal} damage. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Health:
                        s += $"{item.name} `Healing. Heals {item.effectVal} health. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Shield:
                        s += $"{item.name} `Healing. Heals {item.effectVal} shield. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.HealAll:
                        s += $"{item.name} `Healing. Heals {item.effectVal} health & shield. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Trap:
                        s += $"{item.name} `Trap. Deals {item.effectVal} damage on contact. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Empty:
                        s += "Empty slot"; break;
                }

                s += "\n";

                index++;
            }

            builder.AddField("Chest", s);
            return builder.Build();
        }

        string GetSpectatorMessage()
        {
            string builder = "";

            builder += "```" + map.GetWorldMapString() + "```\n";
            builder += "Players Alive:\n" + GetPlayersJoined(true);
            builder += "\n\nPlayers Dead:\n" + GetDeadPlayers();

            return builder;
        }

        string GetInventoryString(Player player)
        {
            string builder = "";

            for (int i = 0; i < player.inventory.Length; i++) //for each slot in the player's inventory
            {
                builder += (i + 1) + ": "; //add the slot number

                if (player.equipped == i) builder += "**[Equipped]** "; //if its equipped then let them know that

                Item item = player.inventory[i]; //get the specific item type

                string pluralUses = item.ammo > 1 ? "uses" : "use"; //improper grammer will reset your vbucks

                switch (item.type) //add the item data to the string
                {
                    case ItemType.Weapon:
                        builder += $"{item.name} `Weapon. Deals {item.effectVal} damage. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Health:
                        builder += $"{item.name} `Healing. Heals {item.effectVal} health. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Shield:
                        builder += $"{item.name} `Healing. Heals {item.effectVal} shield. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.HealAll:
                        builder += $"{item.name} `Healing. Heals {item.effectVal} health & shield. {item.ammo} {pluralUses} left.`"; break;
                    case ItemType.Trap:
                        builder += $"{item.name} `Trap. Deals {item.effectVal} damage on contact. {item.ammo} {pluralUses} left.`"; break;
                    default:
                        builder += "Empty slot"; break;
                }

                builder += "\n";
            }

            return builder;
        }

        async Task HandleIngameReaction(SocketReaction reaction)
        {
            Emoji emote = new Emoji(reaction.Emote.Name);
            Player player = GetPlayerById(reaction.UserId);

            //if the message is the last active message, then continue
            if (player.currentMessages.Last().Id != reaction.MessageId) return;

            if (reaction.Emote.Name == Emotes.infoButton.Name) //if the info button was pressed
            {
                var infoMessage = await player.discordUser.SendMessageAsync($"```{map.GetWorldMapString(player)}```", false, GetHelpMessage()); //send the info menu
                player.currentMessages.Add(infoMessage as RestUserMessage);
                return;
            }

            if (reaction.Emote.Name == Emotes.sprintFastButton.Name) //if fast sprinting button is pressed
            {
                player.movementSpeed = 3; //the player is sprinting (wow)
                return;
            }
            else if (reaction.Emote.Name == Emotes.sprintButton.Name) //if sprinting button is pressed
            {
                player.movementSpeed = 2;
                return;
            }

            for (int i = 0; i < Emotes.actionEmojis.Length; i++) //scan for action emotes
            {
                if (emote.Name == Emotes.actionEmojis[i].Name) //if the emote matches
                {
                    player.turnAction = (Action)i; //the emote array pos should match the enum

                    switch ((Action)i)
                    {
                        case Action.Move:
                            var moveMessage = await player.discordUser.SendMessageAsync($"Select Direction (Add {Emotes.sprintButton} to move 2 tiles, {Emotes.sprintFastButton} to move 3.):") as RestUserMessage; //follow up asking for a direction
                            await moveMessage.AddReactionAsync(Emotes.sprintButton);
                            await moveMessage.AddReactionAsync(Emotes.sprintFastButton);
                            await moveMessage.AddReactionsAsync(Emotes.arrowEmojis);
                            player.currentMessages.Add(moveMessage);
                            break;

                        case Action.Use:
                            ItemType itemType = player.inventory[player.equipped].type;

                            if (map.mapGrid[player.y, player.x].Type == TileType.Water)
                            {
                                var warnMessage = await player.discordUser.SendMessageAsync($"You cannot use items while in water!") as RestUserMessage;
                                player.currentMessages.Add(warnMessage);
                            }
                            else if (itemType == ItemType.Weapon || itemType == ItemType.Trap)
                            {
                                var useMessage = await player.discordUser.SendMessageAsync($"({itemType.ToString()} equipped) Select Direction:") as RestUserMessage; //follow up asking for a direction
                                await useMessage.AddReactionsAsync(Emotes.arrowEmojis);
                                player.currentMessages.Add(useMessage);
                            }
                            else
                            {
                                var useMessage = await player.discordUser.SendMessageAsync("Selected action will be executed at the start of the next turn.") as RestUserMessage;
                                player.currentMessages.Add(useMessage);
                                player.ready = true;
                            }
                            break;

                        case Action.Build:
                            if (player.materials < 10)
                            {
                                var warnMessage = await player.discordUser.SendMessageAsync($"Building requires 10 materials!") as RestUserMessage;
                                player.currentMessages.Add(warnMessage);
                                break;
                            }

                            var buildMessage = await player.discordUser.SendMessageAsync("Select Direction:") as RestUserMessage; //follow up asking for a direction
                            await buildMessage.AddReactionsAsync(Emotes.arrowEmojis);
                            player.currentMessages.Add(buildMessage);
                            break;

                        case Action.Loot:
                            if (map.mapGrid[player.y, player.x].Type == TileType.Chest)
                            {
                                var lootMessage = await player.discordUser.SendMessageAsync(null, false, GetLootMessage(player)) as RestUserMessage; //follow up asking for the slot number
                                await lootMessage.AddReactionsAsync(Emotes.slotEmojis);
                                player.currentMessages.Add(lootMessage);
                            }
                            else if (map.mapGrid[player.y, player.x].Type == TileType.Tree)
                            {
                                var lootConfirmMessage = await player.discordUser.SendMessageAsync("Selected action will be executed at the start of the next turn.") as RestUserMessage;
                                player.currentMessages.Add(lootConfirmMessage);
                                player.ready = true;
                            }
                            else
                            {
                                var warnMessage = await player.discordUser.SendMessageAsync($"You can only loot trees for materials or chests for items!") as RestUserMessage;
                                player.currentMessages.Add(warnMessage);
                            }
                            break;

                        case Action.Equip:
                            var equipMessage = await player.discordUser.SendMessageAsync($"Select Slot: ") as RestUserMessage; //follow up asking for the slot number
                            await equipMessage.AddReactionsAsync(Emotes.slotEmojis);
                            player.currentMessages.Add(equipMessage);
                            break;

                        case Action.Drop:
                            var dropMessage = await player.discordUser.SendMessageAsync("Select Slot: (Items will be dropped into a chest)") as RestUserMessage; //follow up asking for the slot number
                            await dropMessage.AddReactionsAsync(Emotes.slotEmojis);
                            player.currentMessages.Add(dropMessage);
                            break;
                    }

                    return;
                }
            }

            for (int i = 0; i < Emotes.arrowEmojis.Length; i++) //if the emotes are any of the arrow emotes
            {
                if (emote.Name == Emotes.arrowEmojis[i].Name)
                {
                    player.turnDirection = (Direction)i; //change the turn direction value to the arrow emote value
                    player.ready = true;

                    var arrowConfirmMessage = await player.discordUser.SendMessageAsync("Selected action will be executed at the start of the next turn.") as RestUserMessage;
                    player.currentMessages.Add(arrowConfirmMessage);

                    return;
                }
            }

            for (int i = 0; i < Emotes.slotEmojis.Length; i++) //if the emotes are any of the number emotes
            {
                if (emote.Name == Emotes.slotEmojis[i].Name)
                {
                    player.turnIndex = i;

                    switch (player.turnAction)
                    {
                        case Action.Equip:
                            player.equipped = player.turnIndex; //do the equip
                            await player.turnMessage.ModifyAsync(e => e.Embed = GetTurnBriefing(player)); //edit the turn message
                            break;

                        case Action.Loot:
                            player.ready = true; //commit loot next turn

                            var lootConfirmMessage = await player.discordUser.SendMessageAsync("Selected action will be executed at the start of the next turn.") as RestUserMessage;
                            player.currentMessages.Add(lootConfirmMessage);

                            break;

                        case Action.Drop:
                            Item item = player.inventory[player.turnIndex]; //get the item the player is dropping

                            bool added = map.mapGrid[player.y, player.x].AddChestItem(item); //add an item to a chest if possible

                            if (added) //if its possible
                            {
                                player.inventory[player.turnIndex] = new Item(); //remove the item from the players inventory
                                await player.turnMessage.ModifyAsync(e => e.Embed = GetTurnBriefing(player)); //change the turn message because the inventory changed 5head
                                player.stats.totalItemsDropped++;
                            }
                            break;
                    }

                    return;
                }
            }

        }

        async Task HandleIngameReactionRemoval(SocketReaction reaction)
        {
            bool isActionEmote = false;

            foreach (Emoji emote in Emotes.actionEmojis)
            {
                if (emote.Name == reaction.Emote.Name) isActionEmote = true;
            }

            if (!isActionEmote && reaction.Emote.Name != Emotes.infoButton.Name) return; //if the emote is not in the action emote array (and its not the info button) then don't do anything

            Player player = GetPlayerById(reaction.UserId);

            if (player.currentMessages.Last().Id == reaction.MessageId) return; //if the message is the latest message, then don't do anything
            if (player.currentMessages.Count < 2) return; //if there is 1 message in the thingy then don't do the thingy

            player.turnAction = Action.None; //reset the actions so the turn does not end with unintended moves
            player.turnDirection = Direction.None;

            var message = player.currentMessages.Last() as RestUserMessage; //delete the message of the removed reaction
            await message.DeleteAsync();
            player.currentMessages.Remove(player.currentMessages.Last());

            player.ready = false;
        }

        void HandleShootAction(Player player, int slot) //Returns true if a player is hit
        {
            Item weapon = player.inventory[slot];

            int xDir = 0;
            int yDir = 0;

            switch (player.turnDirection)
            {
                case Direction.Left:
                    xDir = -1;
                    break;
                case Direction.Right:
                    xDir = 1;
                    break;
                case Direction.Up:
                    yDir = -1;
                    break;
                case Direction.Down:
                    yDir = 1;
                    break;
            }

            for (int i = 1; i < (int)weapon.range + 1; i++)
            {
                int newX = i * xDir;
                int newY = i * yDir;
                if (player.x + newX >= 0 && player.x + newX < Map.MAPWIDTH
                && player.y + newY >= 0 && player.y + newY < Map.MAPHEIGHT) //Check if the tile to be checked is within the bounds of the array
                {
                    if (CheckForWallHitAtTile(player.x + newX, player.y + newY))
                    {
                        player.briefing += "\n" + "You shot and destroyed a wall.";
                        player.stats.totalWallsDestroyed++;
                        return;
                    }

                    Player hitPlayer = CheckForPlayerHitAtTile(player.x + newX, player.y + newY, weapon);
                    if (hitPlayer != null)
                    {
                        player.briefing += "\n" + $"You shot {hitPlayer.discordUser.Username} and did {weapon.effectVal} damage.";
                        hitPlayer.briefing += "\n" + $"You got shot by {player.discordUser.Username} and took {weapon.effectVal} damage";
                        return;
                    }
                }
                else return;
            }

        }

        bool CheckForWallHitAtTile(int x, int y) //If hits a wall, destroys it and then returns true
        {
            if (map.mapGrid[y, x].Type == TileType.Wall)
            {
                map.mapGrid[y, x].Type = TileType.Grass;
                return true;
            }
            return false;
        }

        Player CheckForPlayerHitAtTile(int x, int y, Item weapon) //Returns the player hit if there was one
        {
            foreach (Player otherPlayer in players)
            {
                if (otherPlayer.x == x && otherPlayer.y == y)
                {
                    otherPlayer.TakeDamage(weapon.effectVal);
                    return otherPlayer;
                }
            }
            return null;
        }

        bool ArePlayersReady()
        {
            foreach (Player player in players)
            {
                if (!player.ready) return false;
            }
            return true;
        }

        string GetDeadPlayers()
        {
            string builder = "";

            foreach(Player player in deadPlayers)
            {
                builder += player.icon + " - `" + player.discordUser.Username + "`\n";
            }

            return builder;
        }

        #endregion

        Player GetPlayerById(ulong id)
        {
            return players.First(x => x.discordUser.Id == id);
        }

        string GetPlayersJoined(bool showReady = false)
        {
            string builder = "";

            if (players.Count > 0) //if there are more than 0 players
            {
                foreach (Player player in players) //for each player in the game, add them to the list.
                {
                    builder += player.icon + " - `" + player.discordUser.Username + "`";
                    if (showReady && player.ready) builder += " Ready";
                    builder += "\n";
                }
            }
            else //if there are no players
            {
                builder = "\n";
            }

            return builder;
        }
    }
}