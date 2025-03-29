using UnityEngine;
using UnityEngine.SceneManagement;

public class FallingBarrel : MonoBehaviour
{
  private static bool particlesLoaded;
  private static GameObject particleEffects;
  private static GameObject specialParticleEffects;
  
  private Rigidbody2D barrelRigidBody;
  [SerializeField] float barrelFallSpeed;

  private Rewindable rewindableScript;
  private float startY;

  private bool isQuitting;
  public bool IsSpecial { get; private set; }
  
  private void Awake()
  {
    IsSpecial = Random.Range(0, 50) == 0;
    
    if (!particlesLoaded)
    {
      particleEffects = Resources.Load<GameObject>("ParticleEffects/BarrelParticles");
      specialParticleEffects = Resources.Load<GameObject>("ParticleEffects/BarrelParticlesSpecial");
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
      Instantiate(IsSpecial ? specialParticleEffects : particleEffects, transform.position, Quaternion.identity);
    }
  }
  
  // Update is called once per frame
  void Update()
  {
    if (rewindableScript && rewindableScript.isRewinding)
    {
      if (Mathf.Abs(transform.position.y - startY) < 0.1f)
      {
        GameManager.IncreaseScore((int) (ScoreEvent.BarrelHammerDestroy * GetModifier()), transform);
        Destroy(gameObject);
      }
      return;
    }
    barrelRigidBody.linearVelocityY = -barrelFallSpeed * GetModifier();
  }
  
  private void HandleBarrelCollision(GameObject obj)
  {
    if (obj.CompareTag("Barrel"))
    {
      if (rewindableScript.isRewinding)
      {
        GameManager.IncreaseScore((int) (ScoreEvent.BarrelRewindDestroy * GetModifier()), transform);
      }
      Destroy(gameObject);
    }
  }

  private float GetModifier()
  {
    return IsSpecial ? 1.5f : 1;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    HandleBarrelCollision(collision.gameObject);
    if (collision.gameObject.name == "BarrelCleanUp")
    {
      Destroy(gameObject);
    }
  }
}
