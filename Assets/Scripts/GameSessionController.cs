using UnityEngine;

public class GameSessionController : MonoBehaviour
{
  [SerializeField] private GameUIController gameUI;
  [SerializeField] private GameClient gameClient;
  private string localPlayerId;
  private PlayerState previousLocalPlayer;
  private PlayerState previousEnemyPlayer;

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
        LogGameState(serverEvent);
        break;

      case "connected":
        localPlayerId = serverEvent.player_id;
        Debug.Log($"Local player ID: {localPlayerId}");
        break;

      case "waiting":
        gameUI.ShowWaiting();
        break;

      case "match_started":
        previousLocalPlayer = null;
        previousEnemyPlayer = null;
        gameUI.ShowPlaying();
        break;

      case "game_over":
        gameUI.FinishBattle(serverEvent.result == "win", serverEvent.message);
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

  private void LogGameState(ServerEvent serverEvent)
  {
    ServerGameState gameState = serverEvent.game_state;
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

    PlayActionFeedback(serverEvent, localPlayer, enemyPlayer);
    gameUI.UpdateHealth(enemyPlayer.hp, enemyPlayer.max_hp, localPlayer.hp, localPlayer.max_hp);

    bool isMyTurn = gameState.current_turn_player_id == localPlayerId;
    gameUI.SetTurn(isMyTurn);
    gameUI.ClearBattleMessage();

    Debug.Log(
        $"Match status: {gameState.status}; " +
        $"my turn: {isMyTurn}"
    );

    previousLocalPlayer = localPlayer;
    previousEnemyPlayer = enemyPlayer;
  }

  private void PlayActionFeedback(ServerEvent serverEvent, PlayerState localPlayer, PlayerState enemyPlayer)
  {
    if (previousLocalPlayer == null || previousEnemyPlayer == null) return;

    bool localPlayerActed = serverEvent.player_id == localPlayerId;
    if (serverEvent.action == "attack")
    {
      int damage = localPlayerActed
        ? previousEnemyPlayer.hp - enemyPlayer.hp
        : previousLocalPlayer.hp - localPlayer.hp;
      gameUI.PlayAttackAnimation(localPlayerActed, damage, damage == 0);
      return;
    }

    if (serverEvent.action == "heal")
    {
      int healed = localPlayerActed
        ? localPlayer.hp - previousLocalPlayer.hp
        : enemyPlayer.hp - previousEnemyPlayer.hp;
      gameUI.PlayHealAnimation(localPlayerActed, healed);
      return;
    }
  }

  public void ChooseAction(string action)
  {
    gameClient.SendAction(action);
  }
}
