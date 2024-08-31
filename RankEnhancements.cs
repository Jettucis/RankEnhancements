using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using Dapper;
using RankEnhancements.Models;

namespace RankEnhancements;

public class EnhancementConfig : BasePluginConfig
{
  [JsonPropertyName("db_host")] public string DbHost { get; set; } = "";
  [JsonPropertyName("db_user")] public string DbUser { get; set; } = "";
  [JsonPropertyName("db_pass")] public string DbPass { get; set; } = "";
  [JsonPropertyName("db_name")] public string DbName { get; set; } = "";
  [JsonPropertyName("db_port")] public string DbPort { get; set; } = "3306";
  [JsonPropertyName("rank_table_name")] public string DbTable { get; set; } = "lvl_base";
  [JsonPropertyName("chat_prefix")] public string ChatPrefix { get; set; } = "[RankEnhancements]";
  [JsonPropertyName("min_players")] public int MinPlayers { get; set; } = 4;
  [JsonPropertyName("give_fleshbang")] public bool GiveFlashbang { get; set; } = true;
  [JsonPropertyName("points_fleshbang")] public int FlashbangTarget { get; set; } = 10000;
  [JsonPropertyName("give_smoke")] public bool GiveSmoke { get; set; } = true;
  [JsonPropertyName("points_smoke")] public int SmokeTarget { get; set; } = 15000;
  [JsonPropertyName("give_grenade")] public bool GiveGrenade { get; set; } = true;
  [JsonPropertyName("points_grenade")] public int GrenadeTarget { get; set; } = 20000;
  [JsonPropertyName("give_fire")] public bool GiveFire { get; set; } = true;
  [JsonPropertyName("points_fire")] public int FireTarget { get; set; } = 25000;
  [JsonPropertyName("give_armour")] public bool GiveArmour { get; set; } = true;
  [JsonPropertyName("points_armour")] public int ArmourTarget { get; set; } = 30000;
  [JsonPropertyName("give_helmet")] public bool GiveHelmet { get; set; } = true;
  [JsonPropertyName("points_helmet")] public int HelmetTarget { get; set; } = 35000;
}

public class RankEnhancements : BasePlugin, IPluginConfig<EnhancementConfig>
{
  public override string ModuleName => "RankEnhancements";
  public override string ModuleAuthor => "Jetta";
  public override string ModuleDescription => "Enhancements for levels_ranks plugin";
  public override string ModuleVersion => "0.0.1";
  // Config //
  public EnhancementConfig Config { get; set; } = new EnhancementConfig();
  //
  private List<RankPlayer> _rankPlayers = new List<RankPlayer>();
  private bool _isWarmup = true;

  public void OnConfigParsed(EnhancementConfig config)
  {
    this.Config = config;
  }
  public override void Load(bool hotReload)
  {
    base.Load(hotReload);
    RegisterHooks();
    RegisterListeners();

    Console.WriteLine("[RankEnhancements] Plugin has been loaded!");
  }

  void RegisterHooks()
  {
    // Server Hooks
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    // Round Hooks
    RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
    RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
    // Player Hooks
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
  }

  void RegisterListeners()
  {
    RegisterListener<Listeners.OnMapStart>(OnMapStart);
    RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
  }
  // -------------------- Map Listeners -------------------- //
  public void OnMapStart(string map)
  {
    _isWarmup = true;
  }
  public void OnMapEnd()
  {
    _rankPlayers.Clear();
  }
  // -------------------- Connection Hooks -------------------- //
  public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
  {
    if (@event.Userid == null || @event.Userid.IsHLTV || (!@event.Userid.IsBot && @event.Userid.SteamID.ToString().Length != 17) || @event.Userid.IsBot || @event.Userid.AuthorizedSteamID == null || @event.Userid.AuthorizedSteamID!.SteamId2 == null) return HookResult.Continue;
    // Pievienojam spēlētāju "sarakstā"
    string steamId = @event.Userid.AuthorizedSteamID!.SteamId2.ToString();
    if (isPlayerInList(steamId)) return HookResult.Continue;

    AddPlayerToList(steamId);

    return HookResult.Continue;
  }
  public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (@event.Userid == null || @event.Userid.IsHLTV || (!@event.Userid.IsBot && @event.Userid.SteamID.ToString().Length != 17) || @event.Userid.IsBot || @event.Userid.AuthorizedSteamID == null || @event.Userid.AuthorizedSteamID!.SteamId2 == null) return HookResult.Continue;
    // Izņemam no "player saraksta"
    string steamId = @event.Userid.AuthorizedSteamID!.SteamId2;
    ChangeSteamID(ref steamId);

    if (!isPlayerInList(steamId)) return HookResult.Continue;

    RankPlayer? rankPlayer = GetRankPlayerBySteamId(steamId);
    if (rankPlayer == null) return HookResult.Continue;

    _rankPlayers.Remove(rankPlayer);

    return HookResult.Continue;
  }
  // -------------------- Round Hooks -------------------- //
  public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
  {
    _isWarmup = false;
    return HookResult.Continue;
  }
  public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
  {
    if (_isWarmup || Config == null || Lib.CTandTCount() < Config.MinPlayers) return HookResult.Continue;

    foreach(CCSPlayerController player in Lib.GetPlayers())
    {
      if (player == null || player.IsHLTV || player.IsBot || player.AuthorizedSteamID == null || player.AuthorizedSteamID!.SteamId2 == null || !player.IsCTorT()) continue;
      
      string steamId = player.AuthorizedSteamID!.SteamId2;
      ChangeSteamID(ref steamId);

      if (!isPlayerInList(steamId)) continue;

      _ = GetPlayersRank(steamId);
    }
    return HookResult.Continue;
  }
  // -------------------- Player Hooks -------------------- //
  public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    if (_isWarmup || @event.Userid == null || @event.Userid.IsHLTV || @event.Userid.IsBot || @event.Userid.AuthorizedSteamID == null || @event.Userid.AuthorizedSteamID!.SteamId2 == null || Config == null || Lib.CTandTCount() < Config.MinPlayers || !@event.Userid.IsCTorT()) return HookResult.Continue;
    
    //https://github.com/partiusfabaa/cs2-LiteVIP/blob/v1.0.7.7/LiteVip/LiteVip.cs
    var gamerules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
    var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
    var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

    if (gamerules != null)
      if (gamerules.TotalRoundsPlayed == 0 || (halftime && maxrounds / 2 == gamerules.TotalRoundsPlayed))
          return HookResult.Continue;

    string steamId = @event.Userid.AuthorizedSteamID!.SteamId2;
    ChangeSteamID(ref steamId);

    if (!isPlayerInList(steamId)) return HookResult.Continue;
    
    RankPlayer? rankPlayer = GetRankPlayerBySteamId(steamId);
    if (rankPlayer == null) return HookResult.Continue;

    GivePlayerItems(@event.Userid, rankPlayer.Points);
    
    return HookResult.Continue;
  }

  private void GivePlayerItems(CCSPlayerController player, int points)
  {
    try
    {
      if (Config == null) throw new InvalidOperationException("Database configuration (Config) is null.");

      if (Config.GiveFlashbang)
      {
        if (points < Config.FlashbangTarget && !Config.GiveSmoke && !Config.GiveGrenade && !Config.GiveFire && !Config.GiveHelmet && !Config.GiveArmour) return;
        if (points >= Config.FlashbangTarget)
        {
          if (!PlayerHasWeapon(player, "flashbang"))
          {
            player.GiveNamedItem("weapon_flashbang");
          }
        }
      }

      if (Config.GiveSmoke)
      {
        if (points < Config.SmokeTarget && !Config.GiveGrenade && !Config.GiveFire && !Config.GiveHelmet && !Config.GiveArmour) return;
        if (points >= Config.SmokeTarget)
        {
          if (!PlayerHasWeapon(player, "smokegrenade"))
          {
            player.GiveNamedItem("weapon_smokegrenade");
          }
        }
      }
      
      if (Config.GiveGrenade)
      {
        if (points < Config.GrenadeTarget && !Config.GiveFire && !Config.GiveHelmet && !Config.GiveArmour) return;
        if (points >= Config.GrenadeTarget)
        {
          if (!PlayerHasWeapon(player, "hegrenade"))
          {
            player.GiveNamedItem("weapon_hegrenade");
          }
        }
      }

      if (Config.GiveFire)
      {
        if (points < Config.FireTarget && !Config.GiveHelmet && !Config.GiveArmour) return;
        if (points >= Config.FireTarget)
        {
          if (!PlayerHasWeapon(player, "incgrenade") && !PlayerHasWeapon(player, "molotov"))
          {
            if (player.IsCT())
            {
              player.GiveNamedItem("weapon_incgrenade");
            }
            else
            {
              player.GiveNamedItem("weapon_molotov");
            }
          }
        }
      }
      // Since there is some kind of "bug" if player was dead last round will spawn with Armour, but without Helmet, so first check if has enough points to apply helmet and then armour
      if (Config.GiveHelmet)
      {
        if (points >= Config.HelmetTarget)
        {
          player.GiveNamedItem("item_kevlar");
          AddTimer(0.1f, () => {
            player.GiveNamedItem("item_assaultsuit");
          });
          return;
        }
        else if (Config.GiveArmour)
        {
          if (points >= Config.ArmourTarget)
          {
            player.GiveNamedItem("item_kevlar");
            return;
          }
          else
          {
            return;
          }
        }
      }

      if (Config.GiveArmour)
      {
        if (points >= Config.ArmourTarget)
        {
          player.GiveNamedItem("item_kevlar");
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($@"[RankEnhancements] Error in GivePlayerItems({player.PlayerName}, {points}): " + ex.Message);
    }
  }
  private void AddPlayerToList(string steamID)
  {
    ChangeSteamID(ref steamID);
    if (isPlayerInList(steamID)) return; // for special cases
    _rankPlayers.Add(new RankPlayer(steamID));
    _ = GetPlayersRank(steamID);
  }

  private bool PlayerHasWeapon(CCSPlayerController player, string wep)
  {
    if (player.IsValid 
      && player.Connected == PlayerConnectedState.PlayerConnected
      && player.PlayerPawn != null
      && player.PawnIsAlive
      && player.PlayerPawn.Value != null
      && player.PlayerPawn.Value.IsValid
      && player.PlayerPawn.Value.WeaponServices != null
      && player.PlayerPawn.Value.WeaponServices.MyWeapons != null)
    {
      foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
      {
        if (weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
        {
          CCSWeaponBase? ccsWeaponBase = weapon.Value.As<CCSWeaponBase>();

          if (ccsWeaponBase != null && ccsWeaponBase.IsValid)
          {
            CCSWeaponBaseVData? weaponData = ccsWeaponBase.VData;

            if (weaponData == null)
            {
              continue;
            }
            if (weaponData.Name.Contains(wep))
            {
              return true;
            }
            //Server.PrintToChatAll($"Debug: Weapon for {player.PlayerName} slot: {weaponData.GearSlot} {weaponData.Name}");
          }
        }
      }
    }
    
    return false;
  }

  // -------------------- Helper Functions -------------------- //
  private void ChangeSteamID(ref string SteamID)
  {
    if (SteamID.StartsWith("STEAM_0:"))
    {
      SteamID = SteamID.Replace("STEAM_0:", "STEAM_1:");
    }
  }
  private bool isPlayerInList(string pid) => _rankPlayers.Any(p => p.SteamID == pid);
  private RankPlayer? GetRankPlayerBySteamId(string steamId) => _rankPlayers.FirstOrDefault(p => p.SteamID == steamId);
  public void TextToChat(CCSPlayerController player, string text)
  {
    if (player.IsConnected()) player.PrintToChat($" {ChatColors.Magenta}{Config.ChatPrefix} {ChatColors.Default}{text}");
  }
  // -------------------- Database Functions -------------------- //
  private async Task GetPlayersRank(string steamId)
  {
    try
    {
      if (Config == null) throw new InvalidOperationException("[RankEnhancements] Database configuration (Config) is null.");

      RankPlayer? rankPlayer = GetRankPlayerBySteamId(steamId);
      if (rankPlayer == null)
      {
        Console.WriteLine($@"[RankEnhancements] GetPlayersRank({steamId}) is not in the rank players list.");
        return;
      }

      var rankPoints = await FetchRankValue(steamId);
      if (rankPoints == null)
      {
        Console.WriteLine($@"[RankEnhancements] GetPlayersRank({steamId}) doesn't have record in database.");
        rankPlayer.Points = 0;
      }
      else
      {
        rankPlayer.Points = (int)rankPoints;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($@"[RankEnhancements] Error in GetPlayersRank({steamId}): " + ex.Message);
    }
  }
  private async Task<int?> FetchRankValue(string steam)
  {
    using (var connection = new MySqlConnection(ConnectionString))
    {
      await connection.OpenAsync();
      var fetchQuery = $@"SELECT value FROM `{Config.DbTable}` WHERE steam = @SteamID";
      return await connection.QueryFirstOrDefaultAsync<int?>(fetchQuery, new { SteamID = steam });
    }
  }
  private string ConnectionString
  {
    get
    {
      if (Config.DbHost == null || Config.DbUser == null || Config.DbPass == null || Config.DbName == null || Config.DbPort == null)
        throw new InvalidOperationException("[RankEnhancements] Database configuration is not properly set.");

      return $"Server={Config.DbHost};Port={Config.DbPort};User ID={Config.DbUser};Password={Config.DbPass};Database={Config.DbName};";
    }
  }

  // -------------------- Command Functions -------------------- //
  [ConsoleCommand("css_rankaid", "Check what rank benefits you have")]
  [ConsoleCommand("css_benefits", "Check what rank benefits you have")]
  [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
  public void OnRankAidCommand(CCSPlayerController? player, CommandInfo commandInfo)
  {
    if (player == null) return;
    if (player.SteamID.ToString().Length != 17 || player.AuthorizedSteamID == null || player.AuthorizedSteamID!.SteamId2 == null)
    {
      TextToChat(player, $"{ChatColors.LightRed}Invalid SteamID. Please re-connect to Steam.");
      return;
    }

    string steamId = player.AuthorizedSteamID!.SteamId2;
    ChangeSteamID(ref steamId);
    RankPlayer? rankPlayer = GetRankPlayerBySteamId(steamId);

    if (rankPlayer == null)
    {
      Console.WriteLine($@"[RankEnhancements] OnRankAidCommand({steamId}) is not in the rank players list.");
      TextToChat(player, $"{ChatColors.LightRed}Can't find you in my list. Try again later.");
      AddPlayerToList(steamId);
      return;
    }

    TextToChat(player, $"{Localizer["benefit.your_rank"]}: {ChatColors.Gold}{rankPlayer.Points}");
    if (Config.GiveFlashbang) TextToChat(player, $"{ChatColors.Gold}({Config.FlashbangTarget}) {ChatColors.Default}{Localizer["benefit.flash"]} - {(rankPlayer.Points >= Config.FlashbangTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
    if (Config.GiveSmoke) TextToChat(player, $"{ChatColors.Gold}({Config.SmokeTarget}) {ChatColors.Default}{Localizer["benefit.smoke"]} - {(rankPlayer.Points >= Config.SmokeTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
    if (Config.GiveGrenade) TextToChat(player, $"{ChatColors.Gold}({Config.GrenadeTarget}) {ChatColors.Default}{Localizer["benefit.grenade"]} - {(rankPlayer.Points >= Config.GrenadeTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
    if (Config.GiveFire) TextToChat(player, $"{ChatColors.Gold}({Config.FireTarget}) {ChatColors.Default}{Localizer["benefit.fire"]} - {(rankPlayer.Points >= Config.FireTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
    if (Config.GiveArmour) TextToChat(player, $"{ChatColors.Gold}({Config.ArmourTarget}) {ChatColors.Default}{Localizer["benefit.armour"]} - {(rankPlayer.Points >= Config.ArmourTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
    if (Config.GiveHelmet) TextToChat(player, $"{ChatColors.Gold}({Config.HelmetTarget}) {ChatColors.Default}{Localizer["benefit.helmet"]} - {(rankPlayer.Points >= Config.HelmetTarget ? $"{ChatColors.Lime}{Localizer["benefit.true"]}" : $"{ChatColors.LightRed}{Localizer["benefit.false"]}")}");
  }
}