using UnityEngine;

public class GameSessionController : MonoBehaviour
{
  [SerializeField] private GameUIController gameUI;
  [SerializeField] private GameClient gameClient;

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
    gameClient.QueueForMatchmaking();
  }

  private void HandleServerEvent(ServerEvent serverEvent)
  {
    switch (serverEvent.type)
    {
      case "waiting":
        gameUI.ShowWaiting();
        break;

      case "match_started":
        gameUI.ShowPlaying();
        break;
    }
  }
}