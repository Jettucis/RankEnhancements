namespace RankEnhancements.Models;

public class RankPlayer
{
  public string SteamID { get; set; }
  public int Points { get; set; } = 0;

  public RankPlayer(string steamId)
  {
    SteamID = steamId;
  }
}