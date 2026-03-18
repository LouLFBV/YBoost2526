using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private GameObject ui;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (ui == null || mainCam == null) return;

        // Simple et robuste
        ui.transform.forward = (ui.transform.position - mainCam.transform.position).normalized;
    }
}
