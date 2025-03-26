using UnityEngine;
using UnityEngine.SceneManagement;

public class FallingBarrel : MonoBehaviour
{
  private static bool particlesLoaded = false;
  private static GameObject particleEffects;
  
  private Rigidbody2D barrelRigidBody;
  [SerializeField] float barrelFallSpeed;

  private Rewindable rewindableScript;
  private float startY;

  private bool isQuitting;
  
  private void Awake()
  {
    if (!particlesLoaded)
    {
      particleEffects = Resources.Load<GameObject>("ParticleEffects/BarrelParticles");
      particlesLoaded = true;
    }
    Application.quitting += () => isQuitting = true;
    rewindableScript = GetComponent<Rewindable>();
    startY = transform.position.y;
    barrelRigidBody = GetComponent<Rigidbody2D>();
  }

  private void OnDestroy()
  {
    if (!isQuitting && SceneManager.GetActiveScene().isLoaded)
    {
      Instantiate(particleEffects, transform.position, Quaternion.identity);
    }
  }
  
  // Update is called once per frame
  void Update()
  {
    if (rewindableScript && rewindableScript.isRewinding)
    {
      if (Mathf.Abs(transform.position.y - startY) < 0.1f)
      {
        GameManager.IncreaseScore(ScoreEvent.BarrelHammerDestroy, transform);
        Destroy(gameObject);
      }
      return;
    }
    barrelRigidBody.linearVelocityY = -barrelFallSpeed;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.name == "BarrelCleanUp")
    {
      Destroy(gameObject);
    }
  }
}
