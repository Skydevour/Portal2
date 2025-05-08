using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public Portal[] portals;

    private void Awake()
    {
        portals = FindObjectsOfType<Portal>();
    }

    private void Update()
    {
        for (var i = 0; i < portals.Length; i++) portals[i].PrePortalRender();
        for (var i = 0; i < portals.Length; i++) portals[i].Render();
        for (var i = 0; i < portals.Length; i++) portals[i].PostPortalRender();
    }
}