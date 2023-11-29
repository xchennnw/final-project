using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
    SkyColorScriptableObject lastSky = null;
    SkyColorScriptableObject currSky = null;

    float transitTime = 3.0f;
    float transitTimeSplit = 0.1f;
    WaitForSeconds transitWait = new WaitForSeconds(0.1f);

    void UpdateSkyColor(SkyColorScriptableObject currSky) {
        if (this.currSky != null) {
            this.lastSky = this.currSky;
            this.currSky = currSky;

            // Start transition 
            StartCoroutine(TransitSky());
        }
        else {
            // First sky
            this.currSky = currSky;

            // Send sky color to shader
            SendSkyColorToShader(currSky.skyColor, currSky.horizonColor, currSky.groundColor);
        }
    }

    IEnumerator TransitSky() {
        for (float t = 0.0f; t < transitTime; t += transitTimeSplit) { 
            float lerp = t / transitTime;

            Color skyColor = Color.Lerp(lastSky.skyColor, currSky.skyColor, lerp);
            Color horizonColor = Color.Lerp(lastSky.horizonColor, currSky.horizonColor, lerp);
            Color groundColor = Color.Lerp(lastSky.groundColor, currSky.groundColor, lerp);

            SendSkyColorToShader(skyColor, horizonColor, groundColor);

            yield return transitWait;
        }
    }

    void SendSkyColorToShader(Color skyColor, Color horizonColor, Color GroundColor) { 
        Shader.SetGlobalColor("_SkyColor", currSky.skyColor);
        Shader.SetGlobalColor("_HorizonColor", currSky.horizonColor);
        Shader.SetGlobalColor("_GroundColor", currSky.groundColor);
    }
}
