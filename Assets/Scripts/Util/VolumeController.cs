using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private VolumeProfile defaultVolume;
    [SerializeField] private VolumeProfile rewindVolume;
    [SerializeField] private PlayerRewindController rewindController;

    private readonly Dictionary<VolumeProfile, Volume> profileToVolumes = new();

    private void Awake()
    {
        var defVol = CreateVolumesForProfile(defaultVolume);
        defVol.weight = 1.0f;
        CreateVolumesForProfile(rewindVolume);
        
        rewindController?.OnRewindToggle.AddListener(mode =>
        {
            if (mode)
            {
                StartCoroutine(ChromaticAbberationOscillation());
            }
            foreach (var kvp in profileToVolumes)
            {
                if (mode)
                {
                    TweenVolumeWeight(kvp.Key != rewindVolume ? 0 : 1, kvp.Value);   
                }
                else
                {
                    TweenVolumeWeight(kvp.Key != rewindVolume ? 1 : 0, kvp.Value);
                }
            }
        });
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
}
