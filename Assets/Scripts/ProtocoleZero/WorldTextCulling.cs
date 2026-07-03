using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ProtocoleZero
{
    /// <summary>
    /// Les TextMesh du decor utilisent "ProtocoleZero/World Text" (lisible de face
    /// uniquement). Certains textes du projet utilisent l'astuce anti-miroir du
    /// scale X negatif, ce qui inverse le winding des triangles : pour eux la face
    /// lisible est techniquement la face "arriere". Ce correctif choisit donc,
    /// texte par texte, la face a garder (Cull Back pour un scale positif,
    /// Cull Front pour un determinant negatif). Sans lui, ces textes seraient
    /// invisibles de face (ecran tutoriel, etiquette du PC...).
    /// </summary>
    public static class WorldTextCulling
    {
        private const string ShaderName = "ProtocoleZero/World Text";
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static Material cullBack;
        private static Material cullFront;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Apply();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private static void Apply()
        {
            TextMesh[] texts = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                MeshRenderer renderer = texts[i].GetComponent<MeshRenderer>();
                if (renderer == null
                    || renderer.sharedMaterial == null
                    || renderer.sharedMaterial.shader == null
                    || renderer.sharedMaterial.shader.name != ShaderName)
                {
                    continue;
                }

                EnsureMaterials(renderer.sharedMaterial);

                Vector3 scale = texts[i].transform.lossyScale;
                bool mirrored = scale.x * scale.y * scale.z < 0f;
                renderer.sharedMaterial = mirrored ? cullFront : cullBack;
            }
        }

        private static void EnsureMaterials(Material source)
        {
            if (cullBack == null)
            {
                cullBack = new Material(source) { name = source.name + "_CullBack" };
                cullBack.SetFloat(CullId, (float)CullMode.Back);
            }

            if (cullFront == null)
            {
                cullFront = new Material(source) { name = source.name + "_CullFront" };
                cullFront.SetFloat(CullId, (float)CullMode.Front);
            }
        }
    }
}
