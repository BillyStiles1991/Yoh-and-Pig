using System.Collections;
using UnityEngine;
public class IntroStationPulse : MonoBehaviour
{
    public bool breathing;
    float phase;
    Vector3 initial;
    void Awake(){initial=transform.localScale;}
    public void Pulse(){phase=1;}
    void Update(){
        phase=Mathf.Max(0,phase-Time.deltaTime*2);
        float offset=breathing?.035f*Mathf.Sin(Time.time*2):0f;
        transform.localScale=Vector3.Scale(initial,new Vector3(1,1+offset+.15f*Mathf.Sin(phase*Mathf.PI),1));
    }
    static AudioClip chime;
    public static AudioClip Chime(){
        if(chime)return chime;
        const int rate=24000; float[] data=new float[rate/2];
        for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=.18f*Mathf.Exp(-7*t)*Mathf.Sin(2*Mathf.PI*(t<.18f?660:880)*t);}
        chime=AudioClip.Create("Oven bell",data.Length,1,rate,false);chime.SetData(data,0);return chime;
    }
}
