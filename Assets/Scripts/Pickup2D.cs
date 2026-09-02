using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Pickup2D : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private UnityEvent<GameObject> onPickedUp;

    public static event Action<Pickup2D, GameObject> PickedUp;
    public bool IsCollected { get; private set; }

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCollected) return;
        GameObject player = other.GetComponentInParent<PlayerMovement>() != null ? other.transform.root.gameObject : null;
        if (player == null && other.CompareTag(playerTag)) player = other.gameObject;
        if (player == null) return;
        IsCollected = true;
        onPickedUp?.Invoke(player);
        PickedUp?.Invoke(this, player);
        if (destroyOnPickup) Destroy(gameObject);
    }
}