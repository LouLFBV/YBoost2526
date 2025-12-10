using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private GameObject ui;

    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}
