public class ServerEvent
{
  public string type;
  public string message;
  public string match_id;
  public ServerGameState game_state;
  public string result;
}

public class ServerGameState
{
  public PlayerState[] players;
  public string current_turn_player_id;
  public string status;
}

public class PlayerState
{
  public string id;
  public int hp;
  public int max_hp;
}