using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

public static class Player
{
  public const int TEAM_SPEC = 1;
  public const int TEAM_T = 2;
  public const int TEAM_CT = 3;

  static public bool IsLegal([NotNullWhen(true)] this CCSPlayerController? player)
  {
    return player != null && player.IsValid && player.PlayerPawn.IsValid && player.PlayerPawn.Value?.IsValid == true; 
  }
  static public bool IsConnected([NotNullWhen(true)] this CCSPlayerController? player)
  {
    return player.IsLegal() && player.Connected == PlayerConnectedState.PlayerConnected;
  }
  static public bool IsCT([NotNullWhen(true)] this CCSPlayerController? player)
  {
    return IsConnected(player) && player.TeamNum == TEAM_CT;
  }
  static public bool IsT([NotNullWhen(true)] this CCSPlayerController? player)
  {
    return IsConnected(player) && player.TeamNum == TEAM_T;
  }
  static public bool IsCTorT([NotNullWhen(true)] this CCSPlayerController? player)
  {
    return IsConnected(player) && (player.TeamNum == TEAM_CT || player.TeamNum == TEAM_T);
  }
}