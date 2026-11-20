using CrtRoyale.TestKit;
using UnityEngine;

/// <summary>Runs the URP test player with HDR and MSAA on, as in a typical game. The renderer asset holds the effect.</summary>
public static class CrtUrpPlayerBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CrtTestPlayer.ConfigureCamera = camera =>
        {
            camera.allowHDR = true;
            camera.allowMSAA = true;
        };
    }
}
