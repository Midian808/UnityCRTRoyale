using CrtRoyale;
using CrtRoyale.TestKit;
using UnityEngine;

/// <summary>Adds the effect to the test player when run with "-crtEffect"; HDR and MSAA stay on as in a typical game.</summary>
public static class CrtBuiltInPlayerBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CrtTestPlayer.ConfigureCamera = camera =>
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-crtEffect") < 0)
                return;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.gameObject.AddComponent<CrtRoyaleEffect>();
        };
    }
}
