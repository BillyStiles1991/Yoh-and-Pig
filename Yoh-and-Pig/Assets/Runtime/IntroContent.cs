using System;
using TMPro;
using UnityEngine;
[Serializable] public class KitchenPair
{
    public string raw, machine, prepared;
    public Sprite rawSprite, machineSprite;
}
[Serializable] public class IntroRecipe
{
    public string titleEn, titleEl;
    [TextArea(3,12)] public string instructionsEn, instructionsEl;
}
[Serializable] public class IntroTrack
{
    public string id, title;
    public AudioClip clip;
    public bool availableAtStart;
}
[Serializable] public class CreditLink { public string label, httpsUrl; }
[CreateAssetMenu(menuName="Freezer Game/Intro kitchen content")]
public class IntroContent : ScriptableObject
{
    public string gameplayScene = "SampleScene", rewardItemId = "item.kitchen_keepsake";
    public KitchenPair[] pairs = {
        new KitchenPair {raw="Wheat",machine="Mill",prepared="Flour"},
        new KitchenPair {raw="Milk",machine="Churn",prepared="Butter"},
        new KitchenPair {raw="Berries",machine="Press",prepared="Berry juice"}
    };
    public Sprite floor, hero, sleepingPig, oven, creditsBackground;
    public RuntimeAnimatorController heroAnimations, pigAnimations, stationAnimations;
    public AudioClip ovenComplete;
    public IntroTrack[] tracks = {};
    public IntroRecipe[] recipes = new IntroRecipe[30];
    public TMP_FontAsset regularFont, alternateFont, dyslexiaFont;
    [TextArea] public string thanks = "Thank you for playing!", thanksEl = "Ευχαριστούμε που παίξατε!";
    [TextArea(4,18)] public string credits;
    public CreditLink[] links = {};
}
