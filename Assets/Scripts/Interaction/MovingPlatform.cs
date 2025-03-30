using System;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private Rigidbody2D rb;

    public Vector2 DeltaP { get; private set; }
    private Vector2 prevPosition;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        prevPosition = rb.position;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var playerController = collision.gameObject.GetComponent<PlayerController>();
        if (!collision.gameObject.CompareTag("Player") || !playerController) return;
        playerController.OnEnterMovingPlatform.Invoke(this);
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        var playerController = collision.gameObject.GetComponent<PlayerController>();
        if (!collision.gameObject.CompareTag("Player") || !playerController) return;
        playerController.OnExitMovingPlatform.Invoke(this);
    }

    private void FixedUpdate()
    {
        DeltaP = rb.position - prevPosition;
        prevPosition = rb.position;
    }
}
