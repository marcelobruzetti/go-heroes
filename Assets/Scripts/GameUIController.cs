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
  [SerializeField] private UnityEngine.UI.Text resultText;

  [SerializeField] private RectTransform enemyHealthFill;
  [SerializeField] private RectTransform playerHealthFill;

  [SerializeField] private UnityEngine.UI.Button attackButton;
  [SerializeField] private UnityEngine.UI.Button defendButton;
  [SerializeField] private UnityEngine.UI.Button healButton;
  [SerializeField] private UnityEngine.UI.Text turnText;

  private float enemyHealthFillMaxX;
  private float playerHealthFillMaxX;

  private void Awake()
  {
    enemyHealthFillMaxX = enemyHealthFill.anchorMax.x;
    playerHealthFillMaxX = playerHealthFill.anchorMax.x;
  }

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

  public void ShowReady()
  {
    currentState = GameState.Ready;
    ApplyState();
  }

  public void ShowWaiting()
  {
    currentState = GameState.Waiting;
    ApplyState();
  }

  public void ShowPlaying()
  {
    currentState = GameState.Playing;
    ApplyState();
  }

  public void FinishBattle(bool won)
  {
    resultText.text = won ? "YOU WIN!" : "YOU LOSE!";
    currentState = GameState.GameOver;
    ApplyState();
  }

  public void PlayAgain()
  {
    currentState = GameState.Waiting;
    ApplyState();
  }

  public void UpdateHealth(float enemyHealthRatio, float playerHealthRatio)
  {
    SetHealthWidth(enemyHealthFill, enemyHealthFillMaxX, enemyHealthRatio);
    SetHealthWidth(playerHealthFill, playerHealthFillMaxX, playerHealthRatio);
  }

  private void SetHealthWidth(RectTransform fill, float fullHealthMaxX, float healthRatio)
  {
    healthRatio = Mathf.Clamp01(healthRatio);

    Vector2 anchorMax = fill.anchorMax;
    anchorMax.x = Mathf.Lerp(fill.anchorMin.x, fullHealthMaxX, healthRatio);
    fill.anchorMax = anchorMax;
  }

  public void SetActionsInteractable(bool interactable)
  {
    attackButton.interactable = interactable;
    defendButton.interactable = interactable;
    healButton.interactable = interactable;
  }

  public void SetTurn(bool isMyTurn)
  {
    turnText.text = isMyTurn ? "YOUR TURN" : "ENEMY'S TURN";
    SetActionsInteractable(isMyTurn);
  }
}