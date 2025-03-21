using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RewindEffect : MonoBehaviour
{
    
    [SerializeField] private Material material;
    [SerializeField] private PlayerRewindController rewindController;
    
    private RawImage rawImage;
    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        var color = rawImage.color;
        color.a = 0;
        rawImage.color = color;
        rewindController?.OnRewindToggle.AddListener(mode => TweenAlpha(mode ? 0 : 1, mode ? 1 : 0));
    }

    private void TweenAlpha(float start, float end)
    {
        var color = rawImage.color;
        color.a = start;
        rawImage.color = color;
        DOTween.ToAlpha(() => rawImage.color, x => rawImage.color = x, end, 1f).SetEase(Ease.InCubic).SetUpdate(true);
    }
    
    private void Update()
    {
        material.SetFloat("_UnscaledTime", Time.unscaledTime);
    }
}
