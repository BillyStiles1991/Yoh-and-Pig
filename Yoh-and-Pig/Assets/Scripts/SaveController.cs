using System.IO;
using Unity.Cinemachine;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    [SerializeField] private CinemachineConfiner2D confiner2D;
    private string saveLocation;
    public void SaveGame()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (confiner2D == null)
        {
            confiner2D = FindAnyObjectByType<CinemachineConfiner2D>();
        }

        if (player == null ||
            confiner2D == null ||
            confiner2D.BoundingShape2D == null)
        {
            Debug.LogError(
                "Cannot save: player, confiner or map boundary is missing.",
                this
            );
            return;
        }

        SaveData saveData = new SaveData
        {
            playerPosition = player.transform.position,
            mapBoundary = confiner2D.BoundingShape2D.gameObject.name
        };

        string saveLocation = Path.Combine(
            Application.persistentDataPath,
            "saveData.json"
        );

        string json = JsonUtility.ToJson(saveData, true);

        try
        {
            File.WriteAllText(saveLocation, json);
            Debug.Log($"Game saved to: {saveLocation}", this);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"Could not save the game: {exception.Message}",
                this
            );
        }
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));
            FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D = GameObject.Find(saveData.mapBoundary).GetComponent<PolygonCollider2D>();
        }
        else
        {
            SaveGame();
        }
    }
}