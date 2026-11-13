using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class ProductionIntro : MonoBehaviour
{
    public Sprite productionLogo;
    public AudioClip logoMusic;
    public VideoClip cutscene;
    public string titleScene = "KitchenTitle";
    [Min(0)] public float blackLead = .35f, logoHold = 1.5f;
    [Min(.01f)] public float logoFade = .65f, movieFade = .5f;
    [Range(.1f,20)] public float movieLimit = 20f;
    [Min(.1f)] public float holdSeconds = 1.2f, retractSeconds = .35f;
    [Range(2,5)] public float ringPixels = 3f;
    [Min(1)] public float preparationTimeout = 6f;
    public TMP_FontAsset promptFont;

    VideoPlayer player;
    AudioSource music, filmSound;
    CanvasGroup logo, film, prompt;
    RawImage screen;
    AspectRatioFitter fit;
    TMP_Text label;
    BootSkipRing ring;
    bool leaving, ended, failed, hintVisible, retracting;
    float fill;

    void Start()
    {
        if (string.IsNullOrWhiteSpace(titleScene) || titleScene == gameObject.scene.name ||
            !Application.CanStreamedLevelBeLoaded(titleScene))
        {
            Debug.LogError("ProductionIntro: assign the enabled KitchenTitle scene in the active build profile.", this);
            enabled = false;
            return;
        }
        BuildUI();
        music = gameObject.AddComponent<AudioSource>();
        filmSound = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = filmSound.playOnAwake = false;
        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false; player.isLooping = false;
        player.renderMode = VideoRenderMode.APIOnly;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, filmSound);
        player.waitForFirstFrame = true;
        player.loopPointReached += Ended;
        player.errorReceived += Error;
        if (cutscene)
        {
            player.clip = cutscene;
            player.Prepare();
        }
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0, blackLead));
        hintVisible = true;
        if (logoMusic) { music.clip = logoMusic; music.volume = 0; music.Play(); }
        yield return FadeLogo(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0, logoHold));
        yield return FadeLogo(false);
        music.Stop();
        if (cutscene && !failed)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + preparationTimeout;
            while (!player.isPrepared && !failed && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (player.isPrepared && !failed)
            {
                fit.aspectRatio = (float)Math.Max(1d, player.width) / (float)Math.Max(1d, player.height);
                float elapsed = 0;
                float limit = Mathf.Clamp(movieLimit,.1f,20f);
                filmSound.volume = 0;
                player.Play();
                while (!ended && !failed && elapsed < limit)
                {
                    screen.texture = player.texture;
                    float alpha = Mathf.Clamp01(elapsed / Mathf.Max(.01f, movieFade));
                    film.alpha = screen.texture ? alpha : 0f; filmSound.volume = alpha;
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                }
            }
            else if (!failed) Debug.LogWarning("Opening film took too long to prepare. Continuing to title.", this);
        }
        EnterTitle();
    }

    IEnumerator FadeLogo(bool fadeIn)
    {
        float duration = Mathf.Max(.01f,logoFade);
        for (float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
        {
            float alpha = Mathf.Clamp01(elapsed/duration);
            logo.alpha = music.volume = fadeIn ? alpha : 1-alpha;
            yield return null;
        }
        logo.alpha = music.volume = fadeIn ? 1 : 0;
    }

    void Update()
    {
        if (leaving || !ring) return;
        var k = Keyboard.current; var g = Gamepad.current; var m = Mouse.current;
        if (!hintVisible)
        {
            hintVisible = (k?.anyKey.wasPressedThisFrame ?? false) || (m?.leftButton.wasPressedThisFrame ?? false);
            if (g != null && !hintVisible)
                foreach (var control in g.allControls)
                    if (control is ButtonControl button && button.wasPressedThisFrame) { hintVisible = true; break; }
        }
        bool held = Application.isFocused && ((k?.eKey.isPressed ?? false) ||
            (m?.leftButton.isPressed ?? false) || (g?.buttonNorth.isPressed ?? false));
        if (held) hintVisible = true;
        prompt.alpha = hintVisible ? 1 : 0;
        if (!held && fill > 0) retracting = true;
        if (retracting)
        {
            fill = Mathf.MoveTowards(fill,0,Time.unscaledDeltaTime/Mathf.Max(.1f,retractSeconds));
            if (fill == 0) retracting = false;
        }
        else if (held) fill = Mathf.MoveTowards(fill,1,Time.unscaledDeltaTime/Mathf.Max(.1f,holdSeconds));
        ring.SetProgress(fill);
        if (fill >= 1) EnterTitle();
    }

    void EnterTitle()
    {
        if (leaving) return;
        leaving = true;
        StopAllCoroutines();
        music.Stop(); filmSound.Stop(); player.Stop();
        SceneManager.LoadSceneAsync(titleScene, LoadSceneMode.Single);
    }
    void Ended(VideoPlayer source) { ended = true; }
    void Error(VideoPlayer source,string reason) { failed=true; Debug.LogWarning("Opening film: "+reason,this); }
    void OnDestroy()
    {
        if (!player) return;
        player.loopPointReached -= Ended;
        player.errorReceived -= Error;
    }

    void BuildUI()
    {
        var canvasObject = new GameObject("Opening canvas",typeof(RectTransform),typeof(Canvas));
        canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var black = Stretch("Black",canvas.transform);
        black.gameObject.AddComponent<Image>().color=Color.black;
        var logoRect=Stretch("Production logo",black);
        logoRect.anchorMin=new Vector2(.15f,.2f);logoRect.anchorMax=new Vector2(.85f,.8f);
        var picture=logoRect.gameObject.AddComponent<Image>();
        picture.sprite=productionLogo;picture.preserveAspect=true;picture.enabled=productionLogo!=null;
        logo=logoRect.gameObject.AddComponent<CanvasGroup>();logo.alpha=0;
        var filmRoot=Stretch("Opening film",black);
        film=filmRoot.gameObject.AddComponent<CanvasGroup>();film.alpha=0;
        var videoRect=Stretch("Picture",filmRoot);screen=videoRect.gameObject.AddComponent<RawImage>();
        fit=videoRect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        var footer=Stretch("Skip prompt",black);
        footer.anchorMin=footer.anchorMax=new Vector2(1,0);
        footer.pivot=new Vector2(1,0);footer.sizeDelta=new Vector2(285,48);footer.anchoredPosition=new Vector2(-20,18);
        footer.gameObject.AddComponent<Image>().color=new Color(0,0,0,.76f);
        prompt=footer.gameObject.AddComponent<CanvasGroup>();prompt.alpha=0;
        var textRect=Stretch("Hold label",footer);textRect.offsetMax=new Vector2(-54,0);
        label=textRect.gameObject.AddComponent<TextMeshProUGUI>();
        if(promptFont) label.font=promptFont;
        label.text=IntroPrefs.L("Hold E / Y / △ / LMB to skip","Κράτησε E / Y / △ / LMB");
        label.fontSize=18;label.alignment=TextAlignmentOptions.Center;
        var circle=Stretch("Hold progress",footer);circle.anchorMin=circle.anchorMax=new Vector2(1,.5f);
        circle.sizeDelta=new Vector2(36,36);circle.anchoredPosition=new Vector2(-27,0);
        ring=circle.gameObject.AddComponent<BootSkipRing>();ring.stroke=Mathf.Clamp(ringPixels,2,5);ring.raycastTarget=false;
    }

    static RectTransform Stretch(string name,Transform parent)
    {
        var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
        rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
}
