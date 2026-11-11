using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotView : MonoBehaviour
{
    public Button button;
    public TMP_Text slotLabel, saveName, savedDate, location, playTime;
    public RawImage preview;
    private Texture2D texture;
    public void Show(int slot, SaveData data, string error)
    {
        if (texture != null) Destroy(texture);
        texture = null;
        Set(slotLabel, "SLOT " + slot);
        Set(saveName, error != null ? "Unreadable save" : data == null ? "Save " + slot + " — Empty" : data.displayName);
        Set(savedDate, ""); Set(location, ""); Set(playTime, "");
        if (preview != null) { preview.texture = null; preview.enabled = false; }
        if (data == null) return;
        if (DateTimeOffset.TryParse(data.savedUtc, out var date))
            Set(savedDate, date.ToLocalTime().ToString("dd MMM yyyy • HH:mm"));
        Set(location, data.locationName);
        TimeSpan time = TimeSpan.FromSeconds(data.progress.secondsPlayed);
        Set(playTime, $"Played {(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
        if (preview != null && !string.IsNullOrEmpty(data.screenshotJpegBase64))
        {
            try
            {
                texture = new Texture2D(2, 2);
                if (!texture.LoadImage(Convert.FromBase64String(data.screenshotJpegBase64)))
                    throw new FormatException("Invalid preview image.");
                preview.texture = texture; preview.enabled = true;
                var aspect = preview.GetComponent<AspectRatioFitter>();
                if (aspect != null) aspect.aspectRatio = (float)texture.width / texture.height;
            }
            catch { if (texture != null) Destroy(texture); texture = null; }
        }
    }
    private void Set(TMP_Text label, string text)
    {
        if (label != null) { label.richText = false; label.text = text; }
    }
    private void OnDestroy() { if (texture != null) Destroy(texture); }
}
