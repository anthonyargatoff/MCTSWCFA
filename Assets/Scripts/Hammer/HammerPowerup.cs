using UnityEngine;

public class HammerPowerup : MonoBehaviour
{
    private const string pfx_path = "";
    [SerializeField] private string custom_pfx_path = "";
    
    private static GameObject _defaultPfxPrefab;
    private GameObject pfxPrefab;
    
    private PlayerController pc;

    private void Awake()
    {
        if (_defaultPfxPrefab == null && pfx_path.Trim().Length > 0)
        {
            _defaultPfxPrefab = Resources.Load(pfx_path) as GameObject;
        }

        if (custom_pfx_path.Trim().Length > 0)
        {
            pfxPrefab = Resources.Load(custom_pfx_path) as GameObject;
        }
        
        pc = transform.parent.GetComponent<PlayerController>();
    }

    public bool PowerupActive()
    {
        return pc?.UsingHammer ?? false;
    }
    
    public void MakeParticleEffects(Transform sourceObject, float? duration = 0.5f)
    {
        var pfx = pfxPrefab ?? _defaultPfxPrefab;
        if (pfx == null) return;
        
        var particles = Instantiate(pfx, sourceObject.position, Quaternion.identity);
        particles.transform.localScale = sourceObject.lossyScale;
        var particlesScr = particles.AddComponent<HammerParticles>();
        StartCoroutine(particlesScr.Debris(duration ?? 0.5f));
    }
}
