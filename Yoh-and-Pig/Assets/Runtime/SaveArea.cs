using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class SaveArea : MonoBehaviour
{
    [Tooltip("Unique permanent ID. Do not change after creating saves.")]
    public string areaId;
    public string displayName;
    public PolygonCollider2D Boundary => GetComponent<PolygonCollider2D>();
}
