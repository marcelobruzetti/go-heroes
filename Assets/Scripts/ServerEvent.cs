[System.Serializable]
public class ServerEvent
{
  public string type;
  public string message;
  public string match_id;
  public string player_id;
  public ServerGameState game_state;
  public string result;
}

[System.Serializable]
public class ServerGameState
{
  public PlayerState[] players;
  public string current_turn_player_id;
  public string status;
}

[System.Serializable]
public class PlayerState
{
  public string id;
  public int hp;
  public int max_hp;
}