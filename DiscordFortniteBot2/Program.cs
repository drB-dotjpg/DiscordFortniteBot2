using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DiscordFortniteBot2
{
    class Program
    {
        static void Main(string[] args) => new Program().Login().GetAwaiter().GetResult();

        public DiscordSocketClient _client;
        private IServiceProvider _services;

        async Task Login()
        {
            _client = new DiscordSocketClient();
            _services = new ServiceCollection().AddSingleton(_client).BuildServiceProvider();

            _client.Log += Log;
            _client.Ready += Ready;
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;

            await _client.LoginAsync(TokenType.Bot, "");  //TODO: Token handling
            await _client.StartAsync();
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private Task Ready()
        {
            throw new NotImplementedException();
        }

        private Task MessageReceived(SocketMessage arg)
        {
            throw new NotImplementedException();
        }

        Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            throw new NotImplementedException();
        }
    }
}
