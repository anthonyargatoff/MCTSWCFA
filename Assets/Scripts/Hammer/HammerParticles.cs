using System.Collections;
using UnityEngine;

public class HammerParticles: MonoBehaviour
{
    public IEnumerator Debris(float duration = 0.5f)
    {
        var particles = GetComponent<ParticleSystem>();
        var mainPfx = particles.main;
        mainPfx.duration = duration;
        mainPfx.startLifetime = duration;
        particles.Play();
        
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}