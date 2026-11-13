using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// The kitchen uses its own transient inventory. Order of stations: 3 sources, 3 machines, oven.
public class IntroKitchenRoom : MonoBehaviour
{
    public IntroContent content;
    public Rigidbody2D hero;
    public Animator heroAnimator;
    public Transform[] stations;
    public Animator[] stationAnimators;
    public AudioSource effects;
    public float speed=3,reach=1.65f;
    public bool CanWalk { get; set; }
    public bool Baked {get;private set;}
    bool[] produced=new bool[3];
    int carried=-1;
    public string Notice {get;private set;}
    public string Hands => carried<0 ? IntroPrefs.L("Empty","Άδεια") : content.pairs[carried].raw;
    public string Products => string.Join(", ",System.Array.FindAll(new int[]{0,1,2},i=>produced[i]).MapNames(content));
    Vector2 move,last=Vector2.down;
    public int Nearest()
    {
        float best=reach*reach; int found=-1;
        for(int i=0;i<stations.Length;i++)
        {
            float d=((Vector2)stations[i].position-hero.position).sqrMagnitude;
            if(d<=best){best=d;found=i;}
        }
        return found;
    }
    public string StationName(int i) => i<0 ? "" : i<3 ? content.pairs[i].raw : i<6 ? content.pairs[i-3].machine : IntroPrefs.L("Oven","Φούρνος");
    void Update()
    {
        move=Vector2.zero;
        if(CanWalk)
        {
            var pad=Gamepad.current;
            if(pad!=null) move=pad.leftStick.ReadValue()+pad.dpad.ReadValue();
            move+=new Vector2((IntroPrefs.Held(3)?1:0)-(IntroPrefs.Held(2)?1:0),(IntroPrefs.Held(0)?1:0)-(IntroPrefs.Held(1)?1:0));
            move=Vector2.ClampMagnitude(move,1);
            if(IntroPrefs.Pressed(4)||(pad?.buttonWest.wasPressedThisFrame??false)) Use(Nearest());
        }
        if(move.sqrMagnitude>.01f) last=move.normalized;
        if(heroAnimator && heroAnimator.runtimeAnimatorController)
        {
            heroAnimator.SetBool("isWalking",move.sqrMagnitude>.01f);
            heroAnimator.SetFloat("InputX",move.x); heroAnimator.SetFloat("InputY",move.y);
            heroAnimator.SetFloat("LastInputX",last.x); heroAnimator.SetFloat("LastInputY",last.y);
        }
    }
    void FixedUpdate(){hero.linearVelocity=CanWalk?move*speed:Vector2.zero;}
    void Use(int i)
    {
        if(i<0)return;
        if(stationAnimators[i] && stationAnimators[i].runtimeAnimatorController) stationAnimators[i].SetTrigger("Interact");
        stations[i].GetComponent<IntroStationPulse>()?.Pulse();
        if(i<3){carried=i; Notice=StationName(i);return;}
        if(i<6)
        {
            int pair=i-3;
            if(carried!=pair){Notice=IntroPrefs.L("Needs ","Χρειάζεται ")+content.pairs[pair].raw;return;}
            produced[pair]=true;carried=-1;Notice=IntroPrefs.L("Made ","Έφτιαξες ")+content.pairs[pair].prepared;return;
        }
        if(Baked){Notice=IntroPrefs.L("Already baked","Έτοιμο");return;}
        if(!System.Array.TrueForAll(produced,b=>b)){Notice=IntroPrefs.L("Prepare all three ingredients first.","Ετοίμασε πρώτα τα τρία υλικά.");return;}
        Baked=true;
        if(content.ovenComplete)effects.PlayOneShot(content.ovenComplete);
        else effects.PlayOneShot(IntroStationPulse.Chime());
        if(heroAnimator && heroAnimator.runtimeAnimatorController)heroAnimator.SetTrigger("Celebrate");
        hero.GetComponentInChildren<IntroStationPulse>()?.Pulse();
        Notice=IntroPrefs.L("Oven complete! Choose New Game to claim your keepsake.","Έτοιμο! Επίλεξε Νέο παιχνίδι για το ενθύμιο.");
    }
}
internal static class IntroNames
{
    internal static string[] MapNames(this int[] indices, IntroContent c)=>System.Array.ConvertAll(indices,i=>c.pairs[i].prepared);
}
