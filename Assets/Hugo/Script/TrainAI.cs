using UnityEngine;
using UnityEngine.AI;

public class TrainAI : MonoBehaviour 
{
    private NavMeshAgent _agent;
    [SerializeField]
    private GameObject _target;
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if ( _agent == null )
        {
            Debug.LogError("Nav Mesh Agent is NULL.");
        }
    }
    void Update()
    {
        _agent.SetDestination(_target.transform.position);
    }
}
