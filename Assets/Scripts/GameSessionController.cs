using UnityEngine;

public class GameSessionController : MonoBehaviour
{
  [SerializeField] private GameUIController gameUI;
  [SerializeField] private GameClient gameClient;
  private string localPlayerId;

  private void OnEnable()
  {
    gameClient.ServerEventReceived += HandleServerEvent;
  }

  private void OnDisable()
  {
    gameClient.ServerEventReceived -= HandleServerEvent;
  }

  public void Play()
  {
    gameUI.ClearConnectionError();
    gameClient.QueueForMatchmaking();
  }

  private void HandleServerEvent(ServerEvent serverEvent)
  {
    switch (serverEvent.type)
    {
      case "game_state":
        LogGameState(serverEvent.game_state);
        break;

      case "connected":
        localPlayerId = serverEvent.player_id;
        Debug.Log($"Local player ID: {localPlayerId}");
        break;

      case "waiting":
        gameUI.ShowWaiting();
        break;

      case "match_started":
        gameUI.ShowPlaying();
        break;

      case "game_over":
        gameUI.FinishBattle(serverEvent.result == "win");
        break;

      case "disconnected":
        localPlayerId = null;
        gameUI.ShowReady();
        gameUI.ShowConnectionError("Connection to the server was lost. Please try again.");
        break;

      case "connection_failed":
        gameUI.ShowConnectionError(serverEvent.message);
        break;

      case "action_rejected":
        gameUI.ShowBattleMessage(serverEvent.message);
        break;
    }
  }

  private void LogGameState(ServerGameState gameState)
  {
    if (gameState == null)
    {
      Debug.LogWarning("Received game_state without state data");
      return;
    }

    PlayerState localPlayer = null;
    PlayerState enemyPlayer = null;

    foreach (PlayerState player in gameState.players)
    {
      if (player.id == localPlayerId)
      {
        localPlayer = player;
      }
      else
      {
        enemyPlayer = player;
      }
    }

    if (localPlayer == null || enemyPlayer == null)
    {
      Debug.LogWarning("Could not identify both players from game state");
      return;
    }

    float localHealthRatio = (float)localPlayer.hp / localPlayer.max_hp;
    float enemyHealthRatio = (float)enemyPlayer.hp / enemyPlayer.max_hp;

    gameUI.UpdateHealth(enemyHealthRatio, localHealthRatio);

    bool isMyTurn = gameState.current_turn_player_id == localPlayerId;
    gameUI.SetTurn(isMyTurn);
    gameUI.ClearBattleMessage();

    Debug.Log(
        $"Match status: {gameState.status}; " +
        $"my turn: {isMyTurn}"
    );
  }

  public void ChooseAction(string action)
  {
    gameClient.SendAction(action);
  }
}