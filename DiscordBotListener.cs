using Impostor.Api.Events;
using Impostor.Api.Events.Client;
using Impostor.Api.Events.Managers;
using Impostor.Api.Events.Meeting;
using Impostor.Api.Events.Player;
using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Api.Net.Custom;
using Impostor.Api.Net.Inner;
using Impostor.Api.Net.Inner.Objects;
using Impostor.Api.Net.Inner.Objects.ShipStatus;
using Impostor.Api.Net.Messages.Rpcs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot
{
    public class DiscordBotListener : IEventListener
    {
        
        private readonly ILogger<DiscordBot> _logger;
        private readonly string hostName = "localhost";
        private IEventManager _eventManager;
        private Dictionary<GameCode, GameData> gameDataMap = new();

        public DiscordBotListener(ILogger<DiscordBot> logger, IEventManager eventManager)
        {   
            _logger = logger;
            _eventManager = eventManager;
            gameDataMap = new Dictionary<GameCode, GameData>();
        }

        [EventListener]
        public void onGameCreated(IGameCreatedEvent e)
        {
            var gameData = new GameData(e.Game.Code);
            gameDataMap.Add(e.Game.Code, gameData);
        }

        [EventListener]
        public void OnGameStarted(IGameStartedEvent e)
        {
            if (!gameDataMap.ContainsKey(e.Game.Code)) return;
            _logger.LogInformation($"Game is starting.");

            var game = gameDataMap[e.Game.Code];
            foreach (var player in e.Game.Players)
            {
                if (player.Character == null) return;
                game.AddPlayer(player);
            }
            

            string pattern = "*_match.json";
            string workingDirectory = Environment.CurrentDirectory;
            string directoryPath = Path.Combine(workingDirectory, "plugins", "MatchLog");
            string[] matchFiles = Directory.GetFiles(directoryPath, pattern);
            var eventData = new
            {
                EventName = "GameStart",
                MatchID = matchFiles.Length - 1,
                GameCode = game.gameCode,
                Players = game.Players.Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                PlayerColors = game.Players.Select(p => p.Character.PlayerInfo.CurrentOutfit.Color).ToList(),
                Impostors = game.Impostors.Where(p => p.Character.PlayerInfo.IsImpostor).Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                Crewmates = game.Crewmates.Where(p => !p.Character.PlayerInfo.IsImpostor).Select(p => p.Character.PlayerInfo.PlayerName).ToList()
                
            };
            string jsonData = JsonSerializer.Serialize(eventData);
            _logger.LogInformation(jsonData);
            SendMessage(jsonData);

            
        }


        [EventListener]
        public void onMeetingStart(IMeetingStartedEvent e)
        {
            if (!gameDataMap.ContainsKey(e.Game.Code)) return;
            var game = gameDataMap[e.Game.Code];
            _logger.LogInformation($"Meeting Started.");
            var eventData = new
            {
                EventName = "MeetingStart",
                GameCode = game.gameCode,
                Players = game.Players.Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                DeadPlayers = game.Players.Where(p => p.Character.PlayerInfo.IsDead).Select(p => p.Character.PlayerInfo.PlayerName).ToList()
            };
            string jsonData = JsonSerializer.Serialize(eventData);
            _logger.LogInformation(jsonData);
            SendMessage(jsonData);
        }

        [EventListener]
        public void onMeetingEnd(IMeetingEndedEvent e)
        {
            if (!gameDataMap.ContainsKey(e.Game.Code)) return;
            var game = gameDataMap[e.Game.Code];
            _logger.LogInformation($"Meeting Ended.");
            var eventData = new
            {
                EventName = "MeetingEnd",
                GameCode = game.gameCode,
                Players = game.Players.Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                DeadPlayers = game.Players.Where(p => p.Character.PlayerInfo.IsDead).Select(p => p.Character.PlayerInfo.PlayerName).ToList()
            };
            string jsonData = JsonSerializer.Serialize(eventData);
            _logger.LogInformation(jsonData);
            SendMessage(jsonData);
        }

        [EventListener(EventPriority.Lowest)]
        public void OnGameEnded(IGameEndedEvent e)
        {
            if (!gameDataMap.ContainsKey(e.Game.Code)) return;
            var game = gameDataMap[e.Game.Code];
            _logger.LogInformation($"Game has ended.");
            string pattern = "*_match.json";
            string workingDirectory = Environment.CurrentDirectory;
            string directoryPath = Path.Combine(workingDirectory, "plugins", "MatchLog");
            string[] matchFiles = Directory.GetFiles(directoryPath, pattern);

            var eventData = new
            {
                EventName = "GameEnd",
                MatchID = matchFiles.Length - 2,
                GameCode = game.gameCode,
                Players = game.Players.Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                PlayerColors = game.Players.Select(p => p.Character.PlayerInfo.CurrentOutfit.Color).ToList(),
                DeadPlayers = game.Players.Where(p => p.Character.PlayerInfo.IsDead).Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                Impostors = game.Impostors.Where(p => p.Character.PlayerInfo.IsImpostor).Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                Crewmates = game.Crewmates.Where(p => !p.Character.PlayerInfo.IsImpostor).Select(p => p.Character.PlayerInfo.PlayerName).ToList(),
                Result = e.GameOverReason
            };
            string jsonData = JsonSerializer.Serialize(eventData);
            _logger.LogInformation(jsonData);
            SendMessage(jsonData);
            game.ResetGame();
        }

        [EventListener]
        public void onGameDestroyed(IGameDestroyedEvent e)
        {
            if (!gameDataMap.ContainsKey(e.Game.Code)) return;
            gameDataMap.Remove(e.Game.Code);

        }

        private void SendMessage(string message)
        {
            _logger.LogDebug(message);
                
            try
            {
                // Connect to the server
                using (var client = new TcpClient(hostName, 5000))
                {
                    // Get the network stream
                    using (var stream = client.GetStream())
                    {
                        // Convert the message to bytes
                        byte[] data = Encoding.UTF8.GetBytes(message);

                        // Send the message
                        stream.Write(data, 0, data.Length);

                        _logger.LogInformation($"Message sent: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while sending message: {ex.Message}");
            }
        }
    }
}


//}
