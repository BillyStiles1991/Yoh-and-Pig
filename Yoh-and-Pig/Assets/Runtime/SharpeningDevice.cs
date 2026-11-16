using UnityEngine;
namespace FreezerGameplay
{
    public class SharpeningDevice:MonoBehaviour
    {
        public bool rotatingStone;public float reach=2,restorationPerSecond=4,xpPerSecond=2;
        public Sprite headAndBack,stoneArt;public AudioClip workingSound;
        public void Interact(){var ui=FindAnyObjectByType<ActivityWindow>();if(ui)ui.OpenSharpening(this);}
    }
    public static class SharpeningRules
    {
        public static float Accuracy(float error,int skill)=>Mathf.Clamp01(1-Mathf.Abs(error)/Mathf.Min(.24f,.07f+Mathf.Max(0,skill-1)*.005f));
        public static float Restoration(float accuracy,float seconds,float rate,int skill)=>Mathf.Max(0,seconds)*Mathf.Max(0,rate)*Mathf.Clamp01(accuracy)*(1+Mathf.Max(0,skill-1)*.03f);
    }
}
