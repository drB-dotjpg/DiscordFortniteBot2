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

            //The rest of the program methods will be called here

            Login().GetAwaiter().GetResult(); //start login sequence
        }

        public DiscordSocketClient _client;
        private IServiceProvider _services;
        public SocketGuild _server;

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

            while (_client.ConnectionState != ConnectionState.Connected) await Task.Delay(30); //wait for login to finish

            await Task.Delay(-1);
            
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            _server = _client.Guilds.First(g => g.Name == inputServerName); //get the server by name TODO: Make it not break
            Console.WriteLine(_server.Name);
            return Task.CompletedTask;
        }

        private Task MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            return Task.CompletedTask;
        }
    }
}
