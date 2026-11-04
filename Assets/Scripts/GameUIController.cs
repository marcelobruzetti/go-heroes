using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
  [SerializeField] private UnityEngine.UI.Text waitingText;
  [SerializeField] private UnityEngine.UI.Text connectionStatusText;
  [SerializeField] private UnityEngine.UI.Text battleStatusText;

  [Header("Battle feedback")]
  [SerializeField] private RectTransform enemyHero;
  [SerializeField] private RectTransform playerHero;
  [SerializeField] private float attackDuration = 0.20f;

  private Text enemyHealthText;
  private Text playerHealthText;
  private Vector2 enemyHeroStartPosition;
  private Vector2 playerHeroStartPosition;
  private Vector3 enemyHeroStartScale;
  private Vector3 playerHeroStartScale;
  private Image enemyHeroImage;
  private Image playerHeroImage;
  private Color enemyHeroStartColor;
  private Color playerHeroStartColor;
  private Coroutine battleAnimation;

  private float enemyHealthFillMaxX;
  private float playerHealthFillMaxX;

  private void Awake()
  {
    enemyHealthFillMaxX = enemyHealthFill.anchorMax.x;
    playerHealthFillMaxX = playerHealthFill.anchorMax.x;

    enemyHealthText = enemyHealthFill.parent.GetComponentInChildren<Text>();
    playerHealthText = playerHealthFill.parent.GetComponentInChildren<Text>();
    enemyHero = enemyHero != null ? enemyHero : GameObject.Find("Enemy Hero")?.GetComponent<RectTransform>();
    playerHero = playerHero != null ? playerHero : GameObject.Find("Player Hero")?.GetComponent<RectTransform>();

    if (enemyHero != null)
    {
      enemyHeroStartPosition = enemyHero.anchoredPosition;
      enemyHeroStartScale = enemyHero.localScale;
      enemyHeroImage = enemyHero.GetComponent<Image>();
      enemyHeroStartColor = enemyHeroImage.color;
    }

    if (playerHero != null)
    {
      playerHeroStartPosition = playerHero.anchoredPosition;
      playerHeroStartScale = playerHero.localScale;
      playerHeroImage = playerHero.GetComponent<Image>();
      playerHeroStartColor = playerHeroImage.color;
    }
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
    waitingText.text = "Waiting for opponent...";
    currentState = GameState.Waiting;
    ApplyState();
  }

  public void ShowPlaying()
  {
    currentState = GameState.Playing;
    ApplyState();
  }

  public void FinishBattle(bool won, string message)
  {
    string result = won ? "YOU WIN!" : "YOU LOSE!";

    if (!string.IsNullOrEmpty(message))
    {
      result += $"\n{message.ToUpperInvariant()}";
    }

    resultText.text = result;
    currentState = GameState.GameOver;
    ApplyState();
  }

  public void PlayAgain()
  {
    currentState = GameState.Waiting;
    ApplyState();
  }

  public void UpdateHealth(int enemyHealth, int enemyMaxHealth, int playerHealth, int playerMaxHealth)
  {
    SetHealthWidth(enemyHealthFill, enemyHealthFillMaxX, HealthRatio(enemyHealth, enemyMaxHealth));
    SetHealthWidth(playerHealthFill, playerHealthFillMaxX, HealthRatio(playerHealth, playerMaxHealth));
    enemyHealthText.text = $"{enemyHealth} / {enemyMaxHealth}";
    playerHealthText.text = $"{playerHealth} / {playerMaxHealth}";
  }

  private float HealthRatio(int health, int maxHealth)
  {
    return maxHealth <= 0 ? 0 : (float)health / maxHealth;
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

  public void ShowConnectionError(string message)
  {
    connectionStatusText.text = message;
  }

  public void ClearConnectionError()
  {
    connectionStatusText.text = "";
  }

  public void ShowBattleMessage(string message)
  {
    battleStatusText.text = message;
  }

  public void ClearBattleMessage()
  {
    battleStatusText.text = "";
  }

  public void PlayAttackAnimation(bool localPlayerAttacked, int damage, bool blocked)
  {
    StartBattleAnimation(AttackAnimation(localPlayerAttacked, damage, blocked));
  }

  public void PlayHealAnimation(bool localPlayerHealed, int amount)
  {
    StartBattleAnimation(HealAnimation(localPlayerHealed, amount));
  }

  private void StartBattleAnimation(IEnumerator animation)
  {
    if (battleAnimation != null)
    {
      StopCoroutine(battleAnimation);
      ResetHeroes();
    }

    battleAnimation = StartCoroutine(animation);
  }

  private IEnumerator AttackAnimation(bool localPlayerAttacked, int damage, bool blocked)
  {
    RectTransform attacker = localPlayerAttacked ? playerHero : enemyHero;
    RectTransform defender = localPlayerAttacked ? enemyHero : playerHero;
    Image defenderImage = localPlayerAttacked ? enemyHeroImage : playerHeroImage;
    Vector2 attackerStart = attacker.anchoredPosition;
    // These heroes are positioned by anchors, so both anchored positions start
    // at zero. Move by an explicit offset instead of interpolating those values.
    float direction = localPlayerAttacked ? -1f : 1f;
    Vector2 target = attackerStart + new Vector2(direction * 300f, 0f);

    for (float elapsed = 0; elapsed < attackDuration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / attackDuration;
      float arc = Mathf.Sin(progress * Mathf.PI) * 36f;
      attacker.anchoredPosition = Vector2.Lerp(attackerStart, target, progress) + Vector2.up * arc;
      yield return null;
    }

    attacker.anchoredPosition = target;
    ShowFloatingText(defender, blocked ? "BLOCKED" : $"-{damage}", blocked ? new Color(0.35f, 0.7f, 1f) : new Color(1f, 0.36f, 0.3f));
    if (!blocked)
    {
      SpawnSlash(defender, localPlayerAttacked);
      SpawnBloodSplash(defender);
    }
    yield return blocked ? BlockAnimation(defender, defenderImage) : HitAnimation(defender, defenderImage);

    for (float elapsed = 0; elapsed < attackDuration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / attackDuration;
      attacker.anchoredPosition = Vector2.Lerp(target, attackerStart, progress);
      yield return null;
    }

    ResetHeroes();
    battleAnimation = null;
  }

  private IEnumerator HitAnimation(RectTransform defender, Image defenderImage)
  {
    Vector2 startPosition = defender.anchoredPosition;
    Color startColor = defenderImage.color;
    Color hitColor = new Color(1f, 0.12f, 0.12f, startColor.a);
    for (float elapsed = 0; elapsed < 0.18f; elapsed += Time.deltaTime)
    {
      defender.anchoredPosition = startPosition + Vector2.right * Mathf.Sin(elapsed * 90f) * 10f;
      defenderImage.color = Color.Lerp(startColor, hitColor, 0.85f);
      yield return null;
    }

    defender.anchoredPosition = startPosition;
    defenderImage.color = startColor;
  }

  private IEnumerator HealAnimation(bool localPlayerHealed, int amount)
  {
    RectTransform hero = localPlayerHealed ? playerHero : enemyHero;
    Image heroImage = localPlayerHealed ? playerHeroImage : enemyHeroImage;
    ShowFloatingText(hero, $"+{amount}", new Color(0.25f, 1f, 0.55f));
    SpawnHealingSparkles(hero);

    Vector3 startScale = hero.localScale;
    Color startColor = heroImage.color;
    for (float elapsed = 0; elapsed < 0.3f; elapsed += Time.deltaTime)
    {
      float pulse = Mathf.Sin(elapsed / 0.3f * Mathf.PI);
      hero.localScale = Vector3.Lerp(startScale, startScale * 1.18f, pulse);
      heroImage.color = Color.Lerp(startColor, new Color(0.35f, 1f, 0.5f, startColor.a), pulse * 0.55f);
      yield return null;
    }

    heroImage.color = startColor;
    ResetHeroes();
    battleAnimation = null;
  }

  private void SpawnHealingSparkles(RectTransform hero)
  {
    Vector2 heroCenter = (hero.anchorMin + hero.anchorMax) * 0.5f;
    for (int index = 0; index < 8; index++)
    {
      GameObject sparkle = new GameObject("Healing Sparkle", typeof(RectTransform), typeof(Image));
      sparkle.transform.SetParent(hero.parent, false);

      RectTransform sparkleTransform = sparkle.GetComponent<RectTransform>();
      sparkleTransform.anchorMin = heroCenter;
      sparkleTransform.anchorMax = heroCenter;
      sparkleTransform.anchoredPosition = hero.anchoredPosition + Random.insideUnitCircle * 28f;
      float size = Random.Range(4f, 9f);
      sparkleTransform.sizeDelta = new Vector2(size, size);
      sparkleTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

      Image sparkleImage = sparkle.GetComponent<Image>();
      sparkleImage.color = Color.Lerp(new Color(0.15f, 0.8f, 0.35f), Color.white, Random.value * 0.55f);
      sparkleImage.raycastTarget = false;
      StartCoroutine(AnimateHealingSparkle(sparkleTransform, sparkleImage));
    }
  }

  private IEnumerator AnimateHealingSparkle(RectTransform sparkle, Image image)
  {
    Vector2 startPosition = sparkle.anchoredPosition;
    Color color = image.color;
    const float duration = 0.5f;

    for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / duration;
      sparkle.anchoredPosition = startPosition + Vector2.up * (30f * progress);
      sparkle.localScale = Vector3.one * (1f - progress * 0.5f);
      color.a = 1f - progress;
      image.color = color;
      yield return null;
    }

    Destroy(sparkle.gameObject);
  }

  private IEnumerator BlockAnimation(RectTransform defender, Image defenderImage)
  {
    Color startColor = defenderImage.color;
    SpawnShield(defender);

    for (float elapsed = 0; elapsed < 0.22f; elapsed += Time.deltaTime)
    {
      float pulse = Mathf.Sin(elapsed / 0.22f * Mathf.PI);
      defenderImage.color = Color.Lerp(startColor, new Color(0.3f, 0.8f, 1f, startColor.a), pulse * 0.45f);
      yield return null;
    }

    defenderImage.color = startColor;
  }

  private void SpawnShield(RectTransform defender)
  {
    GameObject shield = new GameObject("Block Shield", typeof(RectTransform), typeof(Image));
    shield.transform.SetParent(defender.parent, false);

    RectTransform shieldTransform = shield.GetComponent<RectTransform>();
    Vector2 defenderCenter = (defender.anchorMin + defender.anchorMax) * 0.5f;
    shieldTransform.anchorMin = defenderCenter;
    shieldTransform.anchorMax = defenderCenter;
    shieldTransform.anchoredPosition = defender.anchoredPosition;
    shieldTransform.sizeDelta = new Vector2(72f, 72f);
    shieldTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    shieldTransform.localScale = Vector3.one * 0.35f;

    Image image = shield.GetComponent<Image>();
    image.color = new Color(0.2f, 0.75f, 1f, 0.75f);
    image.raycastTarget = false;
    StartCoroutine(AnimateShield(shieldTransform, image));
  }

  private IEnumerator AnimateShield(RectTransform shield, Image image)
  {
    Color color = image.color;
    const float duration = 0.32f;

    for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / duration;
      shield.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.2f, progress);
      color.a = (1f - progress) * 0.75f;
      image.color = color;
      yield return null;
    }

    Destroy(shield.gameObject);
  }

  private void SpawnBloodSplash(RectTransform defender)
  {
    const int particleCount = 7;
    Vector2 defenderCenter = (defender.anchorMin + defender.anchorMax) * 0.5f;

    for (int index = 0; index < particleCount; index++)
    {
      GameObject particle = new GameObject("Blood Particle", typeof(RectTransform), typeof(Image));
      particle.transform.SetParent(defender.parent, false);

      RectTransform particleTransform = particle.GetComponent<RectTransform>();
      particleTransform.anchorMin = defenderCenter;
      particleTransform.anchorMax = defenderCenter;
      particleTransform.anchoredPosition = defender.anchoredPosition + Random.insideUnitCircle * 22f;
      float size = Random.Range(3f, 8f);
      particleTransform.sizeDelta = new Vector2(size, size * Random.Range(0.55f, 1.15f));
      particleTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

      Image image = particle.GetComponent<Image>();
      image.color = Color.Lerp(new Color(0.45f, 0.01f, 0.01f), new Color(1f, 0.13f, 0.08f), Random.value);
      image.raycastTarget = false;

      Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(0.15f, 1f)).normalized;
      StartCoroutine(AnimateBloodParticle(particleTransform, image, direction * Random.Range(45f, 105f)));
    }
  }

  private void SpawnSlash(RectTransform defender, bool localPlayerAttacked)
  {
    GameObject slash = new GameObject("Attack Slash", typeof(RectTransform), typeof(Image));
    slash.transform.SetParent(defender.parent, false);

    RectTransform slashTransform = slash.GetComponent<RectTransform>();
    Vector2 defenderCenter = (defender.anchorMin + defender.anchorMax) * 0.5f;
    slashTransform.anchorMin = defenderCenter;
    slashTransform.anchorMax = defenderCenter;
    slashTransform.anchoredPosition = defender.anchoredPosition;
    slashTransform.sizeDelta = new Vector2(150f, 9f);
    slashTransform.localRotation = Quaternion.Euler(0f, 0f, localPlayerAttacked ? -35f : 35f);
    slashTransform.localScale = new Vector3(0.2f, 1f, 1f);

    Image image = slash.GetComponent<Image>();
    image.color = new Color(1f, 0.94f, 0.84f, 1f);
    image.raycastTarget = false;
    StartCoroutine(AnimateSlash(slashTransform, image));
  }

  private IEnumerator AnimateSlash(RectTransform slash, Image image)
  {
    Color color = image.color;
    const float duration = 0.16f;

    for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / duration;
      slash.localScale = new Vector3(Mathf.Lerp(0.2f, 1.15f, progress), 1f, 1f);
      color.a = 1f - progress;
      image.color = color;
      yield return null;
    }

    Destroy(slash.gameObject);
  }

  private IEnumerator AnimateBloodParticle(RectTransform particle, Image image, Vector2 travel)
  {
    Vector2 startPosition = particle.anchoredPosition;
    Color color = image.color;
    const float duration = 0.38f;

    for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
    {
      float progress = elapsed / duration;
      particle.anchoredPosition = startPosition + travel * progress + Vector2.down * (progress * progress * 20f);
      color.a = 1f - progress;
      image.color = color;
      yield return null;
    }

    Destroy(particle.gameObject);
  }

  private void ShowFloatingText(RectTransform hero, string message, Color color)
  {
    GameObject feedback = new GameObject("Battle Feedback", typeof(RectTransform), typeof(Text));
    feedback.transform.SetParent(hero.parent, false);
    RectTransform feedbackTransform = feedback.GetComponent<RectTransform>();
    feedbackTransform.anchorMin = hero.anchorMin;
    feedbackTransform.anchorMax = hero.anchorMax;
    feedbackTransform.anchoredPosition = hero.anchoredPosition + Vector2.up * 75f;
    feedbackTransform.sizeDelta = new Vector2(180f, 50f);

    Text text = feedback.GetComponent<Text>();
    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    text.text = message;
    text.fontSize = 26;
    text.fontStyle = FontStyle.Bold;
    text.alignment = TextAnchor.MiddleCenter;
    text.color = color;
    StartCoroutine(FloatAndFade(feedbackTransform, text));
  }

  private IEnumerator FloatAndFade(RectTransform feedback, Text text)
  {
    Color color = text.color;
    for (float elapsed = 0; elapsed < 0.7f; elapsed += Time.deltaTime)
    {
      float progress = elapsed / 0.7f;
      feedback.anchoredPosition += Vector2.up * Time.deltaTime * 55f;
      color.a = 1f - progress;
      text.color = color;
      yield return null;
    }

    Destroy(feedback.gameObject);
  }

  private void ResetHeroes()
  {
    if (enemyHero != null)
    {
      enemyHero.anchoredPosition = enemyHeroStartPosition;
      enemyHero.localScale = enemyHeroStartScale;
      if (enemyHeroImage != null) enemyHeroImage.color = enemyHeroStartColor;
    }

    if (playerHero != null)
    {
      playerHero.anchoredPosition = playerHeroStartPosition;
      playerHero.localScale = playerHeroStartScale;
      if (playerHeroImage != null) playerHeroImage.color = playerHeroStartColor;
    }
  }
}
