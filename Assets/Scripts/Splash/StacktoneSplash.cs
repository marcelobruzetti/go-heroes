using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GoHeroes.Splash
{
  /// <summary>
  /// The studio ident. The icon's layers settle into a stack, bottom first, each with a short
  /// mechanical contact. The letters follow from just above their places, one per beat of the
  /// Stacktone melody, and each one plays its piano note the moment it lands. An optional piano
  /// resolution follows. After a short hold, <see cref="SplashHandoff"/> fades to the game.
  ///
  /// Every sound is triggered from the same clock that moves the pieces, at the moment its piece
  /// lands, so picture and sound stay together whatever the timing settings. That clock runs on
  /// unscaled, clamped time: the same at any frame rate, and paused rather than skipped through a
  /// lost focus or a hidden tab. The scene is authored with the logo assembled, so it reads in edit
  /// mode. The game scene loads in the background and only activates once the screen is black.
  /// </summary>
  public sealed class StacktoneSplash : MonoBehaviour
  {
    public enum ResolutionMode
    {
      [Tooltip("The melody's last D rings out on its own.")] SingleNote,
      [Tooltip("A soft D minor under the last D.")] DMinor,
      [Tooltip("A soft Dm6/9 under the last D.")] DMinorSixNine,
      [Tooltip("The earlier synthesised chime.")] SynthChime,
    }

    [Serializable]
    public sealed class Timing
    {
      [Tooltip("Black before the first layer moves. Absorbs the first frame's hitch.")]
      public float leadIn = .12f;
      [Tooltip("Delay between consecutive icon layers.")]
      public float layerStagger = .05f;
      [Tooltip("How long one layer takes from its start to rest.")]
      public float layerTravel = .3f;
      [Tooltip("Point of a layer's travel, 0 to 1, where its contact is heard.")]
      [Range(.3f, 1f)] public float layerImpact = .62f;
      [Tooltip("When the first letter starts moving, from the beginning. Just before the stack completes.")]
      public float letterStart = .46f;
      [Tooltip("The melody's beat: the time between two letters landing, and between two notes. " +
               "136 ms is eighth notes at 220 BPM, the phrase's original tempo.")]
      public float letterStagger = .136f;
      [Tooltip("How long one letter takes from its start to rest.")]
      public float letterTravel = .32f;
      [Tooltip("Point of a letter's travel, 0 to 1, where it counts as landed and its note plays. " +
               "With the ease-out, 0.7 is 98% of the way: the eye reads it as arrived.")]
      [Range(.3f, 1f)] public float letterLanding = .7f;
      [Tooltip("Length of the stack's reaction to each landing layer.")]
      public float settle = .14f;
      [Tooltip("Gap between the stack completing and the highlight starting.")]
      public float highlightDelay = .02f;
      [Tooltip("How long the highlight takes to travel up the stack.")]
      public float highlightTime = .42f;
      [Tooltip("The completed logo, fully still, before the fade starts.")]
      public float hold = .7f;
      [Tooltip("Fade to black at the end of the ident.")]
      public float fadeOut = .42f;
      [Tooltip("Fade to black, and of every splash sound, after a skip.")]
      public float skipFadeOut = .25f;
      [Tooltip("Input this early is ignored, so the click that launched the game does not skip it.")]
      public float skipGuard = .2f;
      [Tooltip("Longest frame the clock accepts. A longer one (a hitch, a hidden tab) pauses the ident.")]
      public float maxStep = 1f / 20f;
    }

    [Serializable]
    public sealed class Motion
    {
      [Tooltip("How far above its place each layer starts, as a fraction of the logo's height.")]
      public float layerRise = .3f;
      [Tooltip("How far above its place each letter starts, as a fraction of the logo's height.")]
      public float letterRise = .12f;
      [Tooltip("Ease-out exponent. 3 is cubic: fast at first, a long smooth deceleration.")]
      [Range(1f, 6f)] public float easePower = 3.2f;
      [Tooltip("Share of the travel over which a piece fades in, so nothing pops into view.")]
      [Range(.05f, 1f)] public float fadeIn = .35f;
      [Tooltip("How far the stack gives under each landing layer, as a fraction of the logo's height.")]
      public float settleDepth = .006f;
      [Tooltip("Brightest point of the highlight. 0 turns it off.")]
      [Range(0f, .6f)] public float highlightStrength = .22f;
      [Tooltip("Width of the highlight band, as a fraction of the stack's height.")]
      [Range(.1f, 1f)] public float highlightWidth = .45f;
      public Color highlightColour = Color.white;
    }

    [Serializable]
    public sealed class Composition
    {
      [Tooltip("Width of the logo as a fraction of the screen's width.")]
      [Range(.2f, 1f)] public float widthFill = .58f;
      [Tooltip("Upper limit on the logo's height as a fraction of the screen's. Keeps ultrawide calm.")]
      [Range(.1f, 1f)] public float heightFill = .24f;
      [Tooltip("Shift right, as a fraction of the logo's width. The icon carries most of the ink, so " +
               "the box centre reads left of the visual centre.")]
      [Range(-.1f, .1f)] public float opticalShift = .02f;
    }

    [Serializable]
    public sealed class Effects
    {
      [Tooltip("The icon layers' mechanical contact.")]
      public AudioClip layer;
      [Range(0f, 1f)] public float layerVolume = .22f;
      [Tooltip("Pitch of the lowest layer. Each layer above sounds a little higher.")]
      public float layerPitch = .94f;
      public float layerPitchStep = .022f;
      [Tooltip("Fixed per-layer variation in pitch and level, so no two contacts sound identical.")]
      [Range(0f, .2f)] public float variation = .05f;
      [Tooltip("The earlier synthesised letter click, under each piano note. 0 turns it off.")]
      public AudioClip letterClick;
      [Range(0f, 1f)] public float letterClickVolume = 0f;
      [Tooltip("The earlier synthesised chime, used by the SynthChime resolution.")]
      public AudioClip chime;
      [Range(0f, 1f)] public float chimeVolume = .33f;
    }

    [Serializable]
    public sealed class Piano
    {
      public bool enabled = true;
      [Tooltip("Level of the whole melody. Each note is this times its velocity.")]
      [Range(0f, 1f)] public float masterVolume = .9f;
      [Tooltip("The recorded notes, all in octave 2.")]
      public AudioClip d, e, f, g, a, b;
      [Tooltip("One note per letter of STACKTONE, left to right.")]
      public string melody = "DFAGBAFED";
      [Tooltip("Relative level of each note, like a pianist's touch. One per letter.")]
      public float[] velocities = { .85f, .78f, .90f, .75f, .95f, .82f, .75f, .78f, .90f };
      [Tooltip("0 lets every note ring out naturally. Above 0, a note starts to fade this long after it starts.")]
      [Min(0f)] public float noteHold = 0f;
      [Tooltip("Length of that fade, when Note Hold is above 0.")]
      [Min(.01f)] public float noteRelease = .5f;
      [Tooltip("Once the game is showing, piano still ringing fades out over this time. 0 lets it ring.")]
      [Min(0f)] public float cleanupFade = 1.2f;
    }

    [Serializable]
    public sealed class Resolution
    {
      public ResolutionMode mode = ResolutionMode.SingleNote;
      [Range(0f, 1f)] public float volume = .4f;
      [Tooltip("After the last letter lands, so the chord never covers the melody's last note.")]
      public float delay = .3f;
      [Tooltip("Delay between the chord's notes, lowest first. A soft roll rather than a block.")]
      public float strum = .03f;
      [Tooltip("The chord fades out over this time once the fade to the game starts. 0 lets it ring.")]
      [Min(0f)] public float fade = 1f;
      [Tooltip("Notes with their octave. The samples are octave 2; octave 3 plays one an octave up. " +
               "Open voicings keep the low register from getting muddy.")]
      public string dMinorVoicing = "D2 A2 F3";
      public string dMinorSixNineVoicing = "D2 A2 E3 F3 B3";
    }

    // A fixed pattern, not random: every run sounds the same, and no two neighbours match.
    static readonly float[] Pattern = { 0f, .7f, -.5f, .3f, -.8f, .5f, -.2f, .9f, -.6f };

    [SerializeField] Camera view;
    [SerializeField] Transform logo;
    [Tooltip("Size of the assembled logo in world units, from the art's layout file.")]
    [SerializeField] Vector2 logoSize = new Vector2(2914f, 572f);
    [Tooltip("Shown in place of the layers once they have settled and the highlight has passed.")]
    [SerializeField] SpriteRenderer iconWhole;
    [Tooltip("Bottom layer first.")]
    [SerializeField] SpriteRenderer[] iconLayers = new SpriteRenderer[0];
    [Tooltip("Left to right.")]
    [SerializeField] SpriteRenderer[] letters = new SpriteRenderer[0];
    [SerializeField] SplashHandoff handoff;
    [SerializeField] string nextScene = "SampleScene";
    [SerializeField] Timing timing = new Timing();
    [SerializeField] Motion motion = new Motion();
    [SerializeField] Composition composition = new Composition();
    [SerializeField] Effects effects = new Effects();
    [SerializeField] Piano piano = new Piano();
    [SerializeField] Resolution resolution = new Resolution();

    struct Piece
    {
      public SpriteRenderer Renderer;
      public Vector3 Rest;
      public Color Colour;
      public float Start;
      public bool Heard;
    }

    struct Voice
    {
      public AudioClip Clip;
      public float Pitch;
    }

    Piece[] layers, glyphs;
    AudioClip[] notes;
    Voice[] dMinor, sixNine;
    int resolved;
    AsyncOperation loading;
    bool exiting, skipped, activated, assembled;
    float clock;

    /// <summary>Seconds of ident played so far.</summary>
    public float Clock => clock;
    public bool Exiting => exiting;
    public bool Skipped => skipped;
    public bool Activated => activated;
    public ResolutionMode Mode { get => resolution.mode; set => resolution.mode = value; }
    public int LayerCount => iconLayers.Length;
    public int LetterCount => letters.Length;
    /// <summary>The clip each letter plays, left to right. Null where the melody has no note.</summary>
    public AudioClip NoteFor(int letter) => notes != null && letter < notes.Length ? notes[letter] : null;

    public float StackComplete => timing.leadIn + Mathf.Max(0, iconLayers.Length - 1) * timing.layerStagger + timing.layerTravel;
    public float LettersComplete => timing.letterStart + Mathf.Max(0, letters.Length - 1) * timing.letterStagger + timing.letterTravel;
    /// <summary>When every piece is at rest.</summary>
    public float Complete => Mathf.Max(StackComplete + timing.settle, LettersComplete);
    /// <summary>When the fade to the game starts, unless skipped.</summary>
    public float ExitTime => Mathf.Max(Complete, HighlightEnd) + timing.hold;
    /// <summary>Full length when nobody skips, to the moment the screen is black.</summary>
    public float Duration => ExitTime + timing.fadeOut;
    /// <summary>When layer <paramref name="i"/> (0 is the bottom) is heard landing.</summary>
    public float LayerImpact(int i) => timing.leadIn + i * timing.layerStagger + timing.layerImpact * timing.layerTravel;
    /// <summary>When letter <paramref name="i"/> lands and its note plays.</summary>
    public float LetterImpact(int i) => timing.letterStart + i * timing.letterStagger + timing.letterLanding * timing.letterTravel;
    /// <summary>When the resolution's first note plays.</summary>
    public float ResolutionStart => LetterImpact(Mathf.Max(0, letters.Length - 1)) + resolution.delay;
    float HighlightStart => StackComplete + timing.highlightDelay;
    float HighlightEnd => HighlightStart + timing.highlightTime;

    void Awake()
    {
      if (view == null) view = Camera.main;
      layers = Prepare(iconLayers, timing.leadIn, timing.layerStagger);
      glyphs = Prepare(letters, timing.letterStart, timing.letterStagger);
      if (iconWhole != null) iconWhole.enabled = false;

      notes = new AudioClip[letters.Length];
      for (int i = 0; i < notes.Length; i++)
        notes[i] = i < piano.melody.Length ? Clip(piano.melody[i]) : null;
      dMinor = ParseVoicing(resolution.dMinorVoicing);
      sixNine = ParseVoicing(resolution.dMinorSixNineVoicing);
    }

    AudioClip Clip(char note)
    {
      switch (char.ToUpperInvariant(note))
      {
        case 'D': return piano.d;
        case 'E': return piano.e;
        case 'F': return piano.f;
        case 'G': return piano.g;
        case 'A': return piano.a;
        case 'B': return piano.b;
        default: return null;
      }
    }

    /// <summary>"D2 A2 F3" to clips and pitches. Octave 2 is the recording; each octave up doubles the pitch.</summary>
    Voice[] ParseVoicing(string voicing)
    {
      var parts = (voicing ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      var voices = new Voice[parts.Length];
      for (int i = 0; i < parts.Length; i++)
      {
        string part = parts[i];
        var clip = Clip(part[0]);
        int octave = part.Length > 1 && char.IsDigit(part[1]) ? part[1] - '0' : 2;
        if (clip == null) Debug.LogWarning($"Splash: '{part}' in voicing '{voicing}' has no sample.", this);
        voices[i] = new Voice { Clip = clip, Pitch = Mathf.Pow(2f, octave - 2) };
      }
      return voices;
    }

    static Piece[] Prepare(SpriteRenderer[] renderers, float start, float stagger)
    {
      var pieces = new Piece[renderers.Length];
      for (int i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null) continue;
        pieces[i] = new Piece
        {
          Renderer = renderer,
          Rest = renderer.transform.localPosition,
          Colour = renderer.color,
          Start = start + i * stagger,
        };
        renderer.enabled = false;
      }
      return pieces;
    }

    void Start()
    {
      if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
      {
        loading = SceneManager.LoadSceneAsync(nextScene);
        loading.allowSceneActivation = false;
      }
      else
      {
        Debug.LogError($"Splash: scene '{nextScene}' is not in the build settings. The splash stays up.");
      }
      Fit();
    }

    void Update()
    {
      clock += Mathf.Min(Time.unscaledDeltaTime, timing.maxStep);
      Fit();

      if (clock >= timing.skipGuard && SkipPressed()) Exit(true);
      else if (clock >= ExitTime) Exit(false);

      AnimateLayers();
      AnimateLetters();
      Resolve();

      // The screen is black and the game is loaded or loading: hand over. Never earlier, so a
      // scene that is not ready is never shown, and only once.
      if (exiting && !activated && loading != null && handoff != null && handoff.IsBlack)
      {
        activated = true;
        loading.allowSceneActivation = true;
      }
    }

    void Exit(bool skip)
    {
      if (loading == null) return; // Nowhere to go. Stay on the logo rather than a black screen.
      if (handoff == null)
      {
        if (!activated) { activated = true; loading.allowSceneActivation = true; }
        return;
      }
      if (exiting && !skip) return;
      bool first = !exiting;
      exiting = true;
      skipped |= skip;
      // A skip during the fade only hurries it. It never starts a second one.
      handoff.FadeOut(skip ? timing.skipFadeOut : timing.fadeOut, skip);
      if (first && !skip && resolution.fade > 0f) handoff.FadeBus(SplashHandoff.Bus.Resolution, resolution.fade);
    }

    void AnimateLayers()
    {
      float height = logoSize.y;
      bool settled = clock >= HighlightEnd && clock >= StackComplete + timing.settle;
      if (settled && !assembled)
      {
        // Swap the pieces for the whole icon, which has no cuts at all. Same pixels, same place.
        assembled = true;
        if (iconWhole != null) iconWhole.enabled = true;
        foreach (var piece in layers)
          if (piece.Renderer != null) piece.Renderer.enabled = false;
      }
      if (assembled) return;

      // The stack gives slightly under each layer as it lands. Every layer below moves together
      // with the one landing, so the stack never opens or overlaps.
      float highlight = Mathf.Clamp01((clock - HighlightStart) / Mathf.Max(.01f, timing.highlightTime));
      float band = motion.highlightWidth;
      for (int i = 0; i < layers.Length; i++)
      {
        ref var piece = ref layers[i];
        var renderer = piece.Renderer;
        if (renderer == null) continue;
        float u = (clock - piece.Start) / timing.layerTravel;
        if (u < 0f) continue;
        renderer.enabled = true;

        if (!piece.Heard && u >= timing.layerImpact)
        {
          piece.Heard = true;
          if (!skipped)
          {
            float pitch = effects.layerPitch + i * effects.layerPitchStep + Vary(i) * .5f;
            Play(SplashHandoff.Bus.Effects, effects.layer, effects.layerVolume * (1f + Vary(i + 3)), pitch);
          }
        }

        float dip = 0f;
        for (int above = i; above < layers.Length; above++)
          dip += Settle(clock - (layers[above].Start + timing.layerTravel));

        float rise = motion.layerRise * height * (1f - EaseOut(u));
        renderer.transform.localPosition = piece.Rest + Vector3.up * (rise - dip * motion.settleDepth * height);

        // A band of light moving up the finished stack: brightest at its centre, gone at its edges.
        float place = layers.Length > 1 ? (float)i / (layers.Length - 1) : 0f;
        float local = (highlight * (1f + band) - place) / band;
        float glow = highlight > 0f && local > 0f && local < 1f ? Mathf.Sin(Mathf.PI * local) : 0f;
        var colour = Color.Lerp(piece.Colour, motion.highlightColour, glow * motion.highlightStrength);
        colour.a = piece.Colour.a * Appear(u);
        renderer.color = colour;
      }
    }

    void AnimateLetters()
    {
      float height = logoSize.y;
      for (int i = 0; i < glyphs.Length; i++)
      {
        ref var piece = ref glyphs[i];
        var renderer = piece.Renderer;
        if (renderer == null) continue;
        float u = (clock - piece.Start) / timing.letterTravel;
        if (u < 0f) continue;
        if (u >= 1f && renderer.enabled && renderer.transform.localPosition == piece.Rest) continue;
        renderer.enabled = true;

        // The letter has arrived: its note, exactly now. After a skip, no new notes start.
        if (!piece.Heard && u >= timing.letterLanding)
        {
          piece.Heard = true;
          if (!skipped) Land(i);
        }

        renderer.transform.localPosition = piece.Rest + Vector3.up * (motion.letterRise * height * (1f - EaseOut(u)));
        var colour = piece.Colour;
        colour.a *= Appear(u);
        renderer.color = colour;
      }
    }

    void Land(int letter)
    {
      if (piano.enabled && notes[letter] != null)
      {
        float velocity = letter < piano.velocities.Length ? piano.velocities[letter] : 1f;
        // Pitch is always 1: the melody is the recording, never bent or randomised.
        Play(SplashHandoff.Bus.Melody, notes[letter], piano.masterVolume * velocity, 1f, piano.noteHold, piano.noteRelease);
      }
      if (effects.letterClickVolume > 0f)
        Play(SplashHandoff.Bus.Effects, effects.letterClick, effects.letterClickVolume, 1f + letter * .011f);
    }

    /// <summary>Plays the resolution's notes as their times come. Never after a skip or once the fade has begun.</summary>
    void Resolve()
    {
      if (exiting || handoff == null) return;
      float since = clock - ResolutionStart;
      if (since < 0f) return;
      switch (resolution.mode)
      {
        case ResolutionMode.DMinor: Roll(dMinor, since); break;
        case ResolutionMode.DMinorSixNine: Roll(sixNine, since); break;
        case ResolutionMode.SynthChime:
          if (resolved == 0) { resolved = 1; Play(SplashHandoff.Bus.Resolution, effects.chime, effects.chimeVolume, 1f); }
          break;
      }
    }

    void Roll(Voice[] chord, float since)
    {
      while (resolved < chord.Length && since >= resolved * resolution.strum)
      {
        var voice = chord[resolved++];
        Play(SplashHandoff.Bus.Resolution, voice.Clip, resolution.volume * piano.masterVolume, voice.Pitch);
      }
    }

    float EaseOut(float u) => 1f - Mathf.Pow(1f - Mathf.Clamp01(u), motion.easePower);
    float Appear(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / motion.fadeIn));
    float Vary(int index) => Pattern[index % Pattern.Length] * effects.variation;

    /// <summary>The stack's give, 0 to 1 to 0, starting the moment a layer comes to rest.</summary>
    float Settle(float since) => since > 0f && since < timing.settle ? Mathf.Sin(Mathf.PI * since / timing.settle) : 0f;

    void Fit()
    {
      if (view == null || !view.orthographic) return;
      var target = view.targetTexture;
      float aspect = Mathf.Max(.1f, target != null ? (float)target.width / target.height : view.aspect);
      view.orthographicSize = Mathf.Max(
        logoSize.x * .5f / composition.widthFill / aspect,
        logoSize.y * .5f / composition.heightFill);
      if (logo != null) logo.localPosition = new Vector3(logoSize.x * composition.opticalShift, 0f, 0f);
    }

    void Play(SplashHandoff.Bus bus, AudioClip clip, float volume, float pitch, float hold = 0f, float release = 0f)
    {
      if (handoff != null && clip != null && volume > 0f) handoff.Play(bus, clip, volume, pitch, hold, release);
    }

    static bool SkipPressed()
    {
      if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
      if (Mouse.current != null &&
          (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        return true;
      if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
      return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }
  }
}
