using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using RankEnhancements.Models;

public static class Lib
{
  static public List<CCSPlayerController> GetPlayers()
  {
    List<CCSPlayerController> players = Utilities.GetPlayers();
    return players.FindAll(player => player.IsLegal() && player.IsConnected());      
  }
  static public int CTandTCount()
  {
    List<CCSPlayerController> players = Lib.GetPlayers();
    return players.FindAll(player => player.IsLegal() && player.IsCTorT()).Count;        
  }
}