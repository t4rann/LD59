using UnityEngine;

public class TransformPoint : MonoBehaviour
{
    public bool isOccupied = false;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}