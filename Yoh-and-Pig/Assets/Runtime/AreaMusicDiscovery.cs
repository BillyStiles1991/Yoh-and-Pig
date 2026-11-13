using UnityEngine;
// Put this on an area-entry trigger, or call Unlock() when the area music begins.
public class AreaMusicDiscovery : MonoBehaviour
{
    public string trackId;
    public void Unlock()
    {
        if (string.IsNullOrWhiteSpace(trackId)) return;
        string key = "intro.music." + trackId;
        if (PlayerPrefs.HasKey(key)) return;
        PlayerPrefs.SetInt(key,1); PlayerPrefs.Save();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody && other.attachedRigidbody.CompareTag("Player")) Unlock();
    }
}
