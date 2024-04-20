using Impostor.Api.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class GameData
    {

        public List<IClientPlayer> Players { get; set; }
        public List<IClientPlayer> Crewmates { get; set; }
        public List<IClientPlayer> Impostors { get; set; }
        public List<IClientPlayer> deadPlayers { get; set; }

        public string gameCode { get; set; }

        public GameData(string code)
        {
            gameCode = code;
            Players = new List<IClientPlayer>();
            Crewmates = new List<IClientPlayer>();
            Impostors = new List<IClientPlayer>();
            deadPlayers = new List<IClientPlayer>();
        }

        public void AddPlayer(IClientPlayer player)
        {
            if(player.Character.PlayerInfo.IsImpostor) 
            {
                Impostors.Add(player);
            } else
            {
                Crewmates.Add(player);
            }
            Players.Add(player);
        }
        public void ResetGame()
        {
            Players = new List<IClientPlayer>();
            Crewmates = new List<IClientPlayer>();
            Impostors = new List<IClientPlayer>();
            deadPlayers = new List<IClientPlayer>();
            //eventLogging = new List<String>();
        }

    }
}
