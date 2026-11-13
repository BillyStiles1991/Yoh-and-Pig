using UnityEngine;
using UnityEngine.InputSystem;
public static class IntroPrefs
{
    public static readonly Key[] Defaults={Key.W,Key.S,Key.A,Key.D,Key.E,Key.Tab};
    static string K(string s)=> "intro.settings."+s;
    public static int Int(string key,int fallback=0)=>PlayerPrefs.GetInt(K(key),fallback);
    public static void Set(string key,int value){PlayerPrefs.SetInt(K(key),value);PlayerPrefs.Save();}
    public static string L(string en,string el)=>Int("greek")==1?el:en;
    public static Key Binding(int i)=>(Key)Int("key"+i,(int)Defaults[i]);
    public static bool Held(int i)=>Keyboard.current!=null && Keyboard.current[Binding(i)].isPressed;
    public static bool Pressed(int i)=>Keyboard.current!=null && Keyboard.current[Binding(i)].wasPressedThisFrame;
    public static void ApplyAudio()=>AudioListener.volume=Int("volume",8)/10f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()=>ApplyAudio();
}
