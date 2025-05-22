using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class PassThroughPlatform3D : MonoBehaviour
{
    private BoxCollider _collider;
    private bool _playerOnPlatform;

    private void Start()
    {
        _collider = GetComponent<BoxCollider>();
    }

    private void OnCollisionStay(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController>();

        if (player != null && player.dropRequested)
        {
            Debug.Log("Pass-through triggered");

            // Disable collider temporarily to allow fall-through
            Physics.IgnoreLayerCollision(_collider.gameObject.layer, player.GetComponent<Collider>().gameObject.layer, true);

            // Reset request so it doesn't keep triggering
            player.dropRequested = false;

            Debug.Log($"Ignoring collision between {_collider.name} and {player.GetComponent<Collider>().name}");
        }
    }

    private System.Collections.IEnumerator RestoreCollision(PlayerController player)
    {
        yield return new WaitForSeconds(0.75f);

        if (player != null)
        {
            Physics.IgnoreLayerCollision(_collider.gameObject.layer, player.GetComponent<Collider>().gameObject.layer, false);
        }
    }
}
