using DG.Tweening;
using UnityEngine;

/// <summary>
/// Runs once at startup before any scene loads.
/// </summary>
public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Tweens: 500 covers 120 max confetti (3 tweens each) + board animations
        // Sequences: 50 is enough — confetti no longer uses sequences
        DOTween.SetTweensCapacity(500, 50);
    }
}
