using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoHeroes.Splash
{
  /// <summary>
  /// Carries the splash across the scene change, so neither picture nor sound breaks:
  /// <list type="bullet">
  /// <item>Plays every splash sound on its own pool of voices, which outlive the splash scene, so a
  /// piano note is never cut off. Each sound gets its own voice, so notes overlap freely, the same
  /// note twice included, and nothing is ever stopped mid-sound.</item>
  /// <item>Groups the voices in three buses (mechanical effects, melody, resolution), each faded
  /// on its own: everything quickly on a skip, the piano gently once the game is showing.</item>
  /// <item>Draws a black curtain over every camera: it fades the splash out, stays black while the
  /// game scene activates (so no half-built frame ever shows), then fades the game in.</item>
  /// </list>
  /// It destroys itself once the game is visible and every voice has finished.
  /// </summary>
  public sealed class SplashHandoff : MonoBehaviour
  {
    public enum Bus { Effects, Melody, Resolution }
    const int BusCount = 3;

    [Tooltip("One voice per sound in flight. The ident plays at most about twenty.")]
    [SerializeField] AudioSource[] voices = new AudioSource[0];
    [Tooltip("Black held after the game scene activates, while its first frames settle.")]
    [SerializeField] float revealHold = .05f;
    [Tooltip("Fade from black into the game.")]
    [SerializeField] float revealTime = .35f;
    [Tooltip("Once the game shows, melody and resolution still ringing fade over this time. 0 lets them ring.")]
    [SerializeField] float pianoCleanup = 1.2f;
    [SerializeField] float maxStep = 1f / 20f;

    enum Stage { Showing, FadingOut, Black, Revealing }

    struct Voice
    {
      public Bus Bus;
      public float Volume, Age, Hold, Release;
    }

    Stage stage;
    Scene origin;
    Voice[] state;
    readonly float[] busGain = { 1f, 1f, 1f };
    readonly float[] busRate = new float[BusCount];
    float curtain, rate, revealClock;
    bool arrived, cleanupStarted;
    int nextVoice, plays;
    readonly AudioClip[] history = new AudioClip[32];

    /// <summary>True from the moment the screen is fully black until the game starts to show.</summary>
    public bool IsBlack => stage == Stage.Black || (stage == Stage.Revealing && curtain >= 1f);
    public float Curtain => curtain;
    /// <summary>Sounds started so far. Lets tests check nothing plays twice.</summary>
    public int Plays => plays;
    /// <summary>The clip of the <paramref name="index"/>th sound played, for tests.</summary>
    public AudioClip Played(int index) => index < history.Length ? history[index] : null;
    public float BusGain(Bus bus) => busGain[(int)bus];

    /// <summary>How many voices are sounding <paramref name="clip"/> right now.</summary>
    public int CountPlaying(AudioClip clip)
    {
      int count = 0;
      foreach (var voice in voices)
        if (voice != null && voice.isPlaying && voice.clip == clip) count++;
      return count;
    }

    void Awake()
    {
      origin = gameObject.scene;
      useGUILayout = false;
      state = new Voice[voices.Length];
      DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.activeSceneChanged += OnActiveSceneChanged;
    void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

    void OnActiveSceneChanged(Scene from, Scene to)
    {
      if (to != origin) arrived = true;
    }

    /// <summary>
    /// Starts <paramref name="clip"/> on a free voice of its own. With <paramref name="hold"/> above 0,
    /// the voice fades over <paramref name="release"/> seconds once it is that old.
    /// </summary>
    public void Play(Bus bus, AudioClip clip, float volume, float pitch, float hold = 0f, float release = 0f)
    {
      if (clip == null || voices.Length == 0) return;
      int index = FreeVoice();
      var voice = voices[index];
      if (voice == null) return;
      state[index] = new Voice { Bus = bus, Volume = volume, Hold = hold, Release = Mathf.Max(.01f, release) };
      voice.clip = clip;
      voice.pitch = pitch;
      voice.volume = volume * busGain[(int)bus];
      voice.Play();
      if (plays < history.Length) history[plays] = clip;
      plays++;
    }

    /// <summary>The next voice that is silent. With none free, the oldest one, which by then has faded.</summary>
    int FreeVoice()
    {
      for (int i = 0; i < voices.Length; i++)
      {
        int index = (nextVoice + i) % voices.Length;
        if (voices[index] != null && !voices[index].isPlaying) { nextVoice = (index + 1) % voices.Length; return index; }
      }
      int oldest = nextVoice;
      nextVoice = (nextVoice + 1) % voices.Length;
      return oldest;
    }

    /// <summary>Fades one bus to silence over <paramref name="seconds"/>. A second call can only hurry it.</summary>
    public void FadeBus(Bus bus, float seconds)
    {
      int b = (int)bus;
      busRate[b] = Mathf.Max(busRate[b], 1f / Mathf.Max(.01f, seconds));
    }

    /// <summary>
    /// Fades to black over <paramref name="seconds"/>. Called again while fading, it can only hurry
    /// the fade. With <paramref name="fadeSounds"/> (a skip), every splash sound fades out with it.
    /// </summary>
    public void FadeOut(float seconds, bool fadeSounds)
    {
      if (fadeSounds)
        for (int b = 0; b < BusCount; b++) FadeBus((Bus)b, seconds);
      float wanted = 1f / Mathf.Max(.01f, seconds);
      if (stage == Stage.FadingOut) { rate = Mathf.Max(rate, wanted); return; }
      if (stage != Stage.Showing) return;
      stage = Stage.FadingOut;
      rate = wanted;
    }

    void Update()
    {
      float dt = Mathf.Min(Time.unscaledDeltaTime, maxStep);
      switch (stage)
      {
        case Stage.FadingOut:
          curtain = Mathf.Min(1f, curtain + dt * rate);
          if (curtain >= 1f) stage = Stage.Black;
          break;
        case Stage.Black:
          if (arrived) { stage = Stage.Revealing; revealClock = 0f; }
          break;
        case Stage.Revealing:
          revealClock += dt;
          curtain = 1f - Mathf.Clamp01((revealClock - revealHold) / Mathf.Max(.01f, revealTime));
          if (!cleanupStarted && curtain <= 0f)
          {
            cleanupStarted = true;
            if (pianoCleanup > 0f) { FadeBus(Bus.Melody, pianoCleanup); FadeBus(Bus.Resolution, pianoCleanup); }
          }
          if (curtain <= 0f && !AnyVoicePlaying()) Destroy(gameObject);
          break;
      }
      Mix(dt);
    }

    /// <summary>Applies each voice's own envelope and its bus's fade.</summary>
    void Mix(float dt)
    {
      for (int b = 0; b < BusCount; b++)
        if (busRate[b] > 0f) busGain[b] = Mathf.Max(0f, busGain[b] - dt * busRate[b]);
      for (int i = 0; i < voices.Length; i++)
      {
        var voice = voices[i];
        if (voice == null || !voice.isPlaying) continue;
        ref var s = ref state[i];
        s.Age += dt;
        float envelope = s.Hold > 0f ? Mathf.Clamp01(1f - (s.Age - s.Hold) / s.Release) : 1f;
        float gain = busGain[(int)s.Bus];
        voice.volume = s.Volume * envelope * gain * gain; // Squared: a fade that sounds even, not abrupt at the end.
        // Silent for good: stop it, so the handoff can finish. Nothing is audible to click.
        if (envelope <= 0f || gain <= 0f) voice.Stop();
      }
    }

    bool AnyVoicePlaying()
    {
      foreach (var voice in voices)
        if (voice != null && voice.isPlaying) return true;
      return false;
    }

    void OnGUI()
    {
      if (curtain <= 0f || Event.current.type != EventType.Repaint) return;
      // OnGUI draws after every camera, so the curtain covers the splash and the game alike.
      GUI.depth = -1000;
      var previous = GUI.color;
      GUI.color = new Color(0f, 0f, 0f, curtain);
      GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
      GUI.color = previous;
    }
  }
}
