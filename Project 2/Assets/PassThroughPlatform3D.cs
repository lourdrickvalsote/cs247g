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
            Physics.IgnoreCollision(_collider, player.GetComponent<Collider>(), true);

            // Reset request so it doesn't keep triggering
            player.dropRequested = false;

            // Optional: Re-enable collision after a delay
            StartCoroutine(RestoreCollision(player));
        }
    }

    private System.Collections.IEnumerator RestoreCollision(PlayerController player)
    {
        yield return new WaitForSeconds(0.5f);

        if (player != null)
        {
            Physics.IgnoreCollision(_collider, player.GetComponent<Collider>(), false);
        }
    }

    // private void Update()
    // {
    //     var player = other.gameObject.GetComponent<PlayerController>();
    //     if (_playerOnPlatform && player.dropRequested)
    //     {

    //     }
    // }

    // private void SetPlayerOnPlatform(BoxCollider other, bool value)
    // {
    //     var player = other.gameObject.GetComponent<PlayerController>();
    //     if (player != null)
    //     {
    //         _playerOnPlatform = value;
    //     }
    // }

    // private void OnCollisionEnter3D(BoxCollider other)
    // {
    //     SetPlayerOnPlatform(other, true);
    // }

    // private void OnCollisionExit3D(BoxCollider other)
    // {
    //     SetPlayerOnPlatform(other, true);
    // }
}
