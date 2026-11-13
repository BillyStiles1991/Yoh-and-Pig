using System;
using System.Collections;
using System.Linq;
using FreezerGameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

// Short-lived handoff used exclusively by New Game. Nothing is written by the oven.
public class IntroNewGame : MonoBehaviour
{
    public static string Error;
    public static int FirstFree()
    {
        for(int slot=1;slot<=SaveRepository.SlotCount;slot++) if(!SaveRepository.Exists(slot)) return slot;
        return 0;
    }
    public static void StartRun(string scene,int slot,string reward)
    {
        var go = new GameObject("Initial game save");
        DontDestroyOnLoad(go);
        go.AddComponent<IntroNewGame>().StartCoroutine(Enter(go,scene,slot,reward,SceneManager.GetActiveScene().path));
    }
    static IEnumerator Enter(GameObject go,string scene,int slot,string reward,string title)
    {
        var load=SceneManager.LoadSceneAsync(scene);
        yield return load;
        float speed=Time.timeScale;
        Time.timeScale=0;
        yield return new WaitForEndOfFrame();
        Texture2D preview=null;
        RenderTexture rt=null, previous=RenderTexture.active;
        try
        {
            var save=FindAnyObjectByType<SaveController>();
            if (!save || !save.progress || !save.playerBody || !save.confiner2D) throw new Exception("Check gameplay SaveController references.");
            var area=FindObjectsByType<SaveArea>(FindObjectsSortMode.None).Single(a=>a.Boundary==save.confiner2D.BoundingShape2D);
            var snapshot=save.progress.Snapshot();
            if (!string.IsNullOrEmpty(reward))
            {
                var inv=FindAnyObjectByType<InventoryController>();
                var definition=inv && inv.database ? inv.database.Item(reward) : null;
                if (!inv || inv.store!=save.progress || definition==null || !definition.isKeyItem)
                    throw new Exception("Configure the intro reward as a Key Item in the gameplay inventory database.");
                var entry=snapshot.inventory.Find(i=>i.itemId==reward && string.IsNullOrEmpty(i.instanceId));
                if(entry==null) snapshot.inventory.Add(new InventoryEntry{itemId=reward,quantity=1});
                else entry.quantity=checked(entry.quantity+1);
            }
            int w=480,h=Mathf.Max(1,Mathf.RoundToInt(w*(float)Screen.height/Screen.width));
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(!screen) throw new Exception("Unable to capture the starting location.");
            try
            {
                rt=RenderTexture.GetTemporary(w,h,0); Graphics.Blit(screen,rt); RenderTexture.active=rt;
                preview=new Texture2D(w,h,TextureFormat.RGB24,false);
                preview.ReadPixels(new Rect(0,0,w,h),0,0); preview.Apply();
            } finally { Destroy(screen); }
            var data=new SaveData{slot=slot,displayName="New game",savedUtc=DateTime.UtcNow.ToString("O"),
                sceneName=scene,areaId=area.areaId,locationName=area.displayName,playerPosition=save.playerBody.transform.position,
                savePointId="new_game",progress=snapshot,screenshotJpegBase64=Convert.ToBase64String(preview.EncodeToJPG(75))};
            SaveRepository.Write(data,true);
            save.progress.Restore(snapshot);
        } catch(Exception e) { Error=e.Message; }
        finally
        {
            RenderTexture.active=previous; if(rt) RenderTexture.ReleaseTemporary(rt);
            if(preview) Destroy(preview);
            Time.timeScale=speed;
        }
        if(Error!=null) yield return SceneManager.LoadSceneAsync(title);
        Destroy(go);
    }
}
