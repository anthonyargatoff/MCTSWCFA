using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private VolumeProfile templateDefaultVolume;
    [SerializeField] private VolumeProfile templateRewindVolume;
    [SerializeField] private VolumeProfile templateDeathVolume;
    [SerializeField] private PlayerRewindController rewindController;
    [SerializeField] private PlayerController playerController;

    private VolumeProfile defaultVolume;
    private VolumeProfile rewindVolume;
    private VolumeProfile deathVolume;
    
    private readonly Dictionary<VolumeProfile, Volume> profileToVolumes = new();
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        defaultVolume = Instantiate(templateDefaultVolume);
        var defVol = CreateVolumesForProfile(defaultVolume);
        defVol.weight = 1.0f;
        
        rewindVolume = Instantiate(templateRewindVolume);
        CreateVolumesForProfile(rewindVolume);
        
        deathVolume = Instantiate(templateDeathVolume);
        CreateVolumesForProfile(deathVolume);   
        
        rewindController?.OnRewindToggle.AddListener(mode =>
        {
            if (mode)
            {
                StartCoroutine(ChromaticAbberationOscillation());
            }
            TransitionVolumes(mode ? rewindVolume : defaultVolume);
        });

        playerController.OnDeath += () =>
        {
            TransitionVolumes(deathVolume);
            StartCoroutine(FadeInDeathVolume());
        };
    }

    private static void TweenVolumeWeight(float end, Volume volume)
    {
        DOTween.To(() => volume.weight, v => volume.weight = v, end, 1.0f).SetEase(Ease.Linear).SetUpdate(true);
    }

    private Volume CreateVolumesForProfile(VolumeProfile profile)
    {
        var gObj = new GameObject();
        gObj.transform.SetParent(transform);
        gObj.name = profile.name;
        var vol = gObj.AddComponent<Volume>();
        vol.sharedProfile = profile;
        vol.weight = 0f;
        profileToVolumes[profile] = vol;

        return vol;
    }

    private IEnumerator ChromaticAbberationOscillation()
    {
        const float min = 0, max = 0.2f;
        rewindVolume.TryGet<ChromaticAberration>(out var chromaticAbberation);
        
        if (chromaticAbberation == null) yield break;

        chromaticAbberation.intensity.value = min;
        var toggle = false;
        while (rewindController.IsRewinding)
        {
            DOTween.To(() => chromaticAbberation.intensity.value, x => chromaticAbberation.intensity.value = x, toggle ? min : max, 1.0f).SetEase(Ease.Linear).SetUpdate(true);
            toggle = !toggle;
            yield return new WaitForSecondsRealtime(1.2f);
        }
        chromaticAbberation.intensity.value = min;
    }

    private void TransitionVolumes(VolumeProfile volume)
    {
        foreach (var kvp in profileToVolumes)
        {
            TweenVolumeWeight(kvp.Key != volume ? 0 : 1, kvp.Value); 
        }
    }

    private IEnumerator FadeInDeathVolume()
    {
        Vignette vignette = null;
        deathVolume?.TryGet(out vignette);
        if (!vignette) yield break;

        vignette.intensity.value = 0f;
        vignette.center.value = mainCamera.WorldToViewportPoint(playerController.transform.position);
        
        DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, 1, 1.0f).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1.5f);
    }
}
