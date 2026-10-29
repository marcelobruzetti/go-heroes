using UnityEngine;

public class GameUIController : MonoBehaviour
{
  private enum GameState
  {
    Ready,
    Waiting,
    Playing,
    GameOver
  }

  [SerializeField] private GameState currentState = GameState.Ready;
  [SerializeField] private GameObject readyPanel;
  [SerializeField] private GameObject waitingPanel;
  [SerializeField] private GameObject playingPanel;
  [SerializeField] private GameObject gameOverPanel;

  private void ApplyState()
  {
    readyPanel.SetActive(currentState == GameState.Ready);
    waitingPanel.SetActive(currentState == GameState.Waiting);
    playingPanel.SetActive(currentState == GameState.Playing);
    gameOverPanel.SetActive(currentState == GameState.GameOver);
  }

  private void Start()
  {
    ApplyState();
  }

  public void StartMatchmaking()
  {
    currentState = GameState.Waiting;
    ApplyState();
  }

  public void StartBattle()
  {
    Debug.Log("Start Battle clicked");

    currentState = GameState.Playing;
    ApplyState();
  }
}