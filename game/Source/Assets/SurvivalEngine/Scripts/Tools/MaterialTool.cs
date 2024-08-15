using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }

    /// <summary>
    /// This works for STANDARD shaders only, change the Material Render Mode
    /// If you are using another shader, you should maybe add another function that does the same for your shader
    /// </summary>

    public class MaterialTool
    {
        public static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
        {
            if (IsStandard(standardShaderMaterial))
            {
                switch (blendMode)
                {
                    case BlendMode.Opaque:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        standardShaderMaterial.SetInt("_ZWrite", 1);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = -1;
                        break;
                    case BlendMode.Cutout:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        standardShaderMaterial.SetInt("_ZWrite", 1);
                        standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 2450;
                        break;
                    case BlendMode.Fade:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        standardShaderMaterial.SetInt("_ZWrite", 0);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 3000;
                        break;
                    case BlendMode.Transparent:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        standardShaderMaterial.SetInt("_ZWrite", 0);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 3000;
                        break;
                }
            }
        }

        public static bool IsStandard(Material material)
        {
            return material != null && material.HasProperty("_Color")
                && material.HasProperty("_MainTex")
                && material.HasProperty("_SrcBlend")
                && material.HasProperty("_DstBlend")
                && material.HasProperty("_ZWrite");
        }

        public static bool HasColor(Material material)
        {
            return material != null && material.HasProperty("_Color");
        }
    }
}