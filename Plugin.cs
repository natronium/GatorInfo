using BepInEx;
using Cinemachine;
using Mono.Cecil;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scratch
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        Plugin p;
        private void Awake()
        {
            p = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogDebug("FLORGLE!");
        }

        private static Vector3 oAngle;
        private static Vector3 oPos;
        private static float oBaseMapDist;
        private static float oFarClip;
        private static float oLodBias;

        private static Terrain terrain;
        private static Transform tf;
        private static GameObject player;
        private static CinemachineBrain brain;
        private static new Camera camera;
        private static bool isSnapped = false;

        public static void Snapshot()
        {
            if (isSnapped)
            {
                return;
            }
            isSnapped = true;

            camera = Camera.main;
            oFarClip = camera.farClipPlane;
            brain = camera.GetComponent<Cinemachine.CinemachineBrain>();
            player = GameObject.Find("/Players/Player");
            tf = camera.transform;
            oPos = tf.position;
            oAngle = tf.eulerAngles;
            terrain = GameObject.Find("/Terrain").GetComponent<Terrain>();
            oBaseMapDist = terrain.basemapDistance;
            oLodBias = QualitySettings.lodBias;
        }
        public static void Restore()
        {
            
            terrain.basemapDistance = oBaseMapDist;
            tf.eulerAngles = oAngle;
            tf.position = oPos;
            player.SetActive(true);
            brain.enabled = true;
            //don't care about orthosize. game doesn't use ortho (i think)
            camera.orthographic = false;
            camera.farClipPlane = oFarClip;
            RenderSettings.fog = true;
            QualitySettings.lodBias = oLodBias;
        }

        public static void DoTheThing(bool restore = false)
        {
            Snapshot();
            UnityEngine.RenderSettings.fog = false;
            camera.farClipPlane = 1e6F;
            camera.orthographic = true;
            camera.orthographicSize = 250; //island is approximately 800x800 square units. this number is half the height of the camera's view
            brain.enabled = false; // Turn off camera control stuff
            player.SetActive(false); // Turn off character
            tf.position = new Vector3(64, 100, 83); //determined empirically NB: needs to be above highest point on island to prevent clipping
            tf.eulerAngles = new Vector3(90, 0, 0);
            terrain.basemapDistance = 500;
            QualitySettings.lodBias = 100_000f;


            //NB: no colons allowed in windows filenames
            SnapPic(camera, $"{System.DateTime.Now.ToString("s").Replace(':', '-')}-snap", RenderTextureFormat.ARGB32);
            //TODO: how to depth?? depth would be sweet? or maybe some other trick for doing a heightmap??
            //SnapPic(camera, $"{System.DateTime.Now.ToString("s").Replace(':', '-')}-depth", RenderTextureFormat.Depth);

            //restore
            if (restore) {
                Restore();
            }

        }

        public static void SnapPic(Camera cam, string name, RenderTextureFormat format)
        {
            cam.enabled = false;
            RenderTexture rt = new(10_000, 10_000, 32, format);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            cam.enabled = true;


            var narray = new NativeArray<byte>(rt.width * rt.height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref narray, rt, 0, (AsyncGPUReadbackRequest req) =>
            {
                if (!req.hasError)
                {
                    NativeArray<byte> encoded;
                    encoded = ImageConversion.EncodeNativeArrayToPNG(narray, rt.graphicsFormat, (uint)rt.width, (uint)rt.height);
                    System.IO.File.WriteAllBytes($"C:\\Users\\na\\Desktop\\LilGatorProject\\MapPics\\{name}.png", encoded.ToArray());
                    encoded.Dispose();
                }
                narray.Dispose();
            });
        }
        // UnityEngine.RenderSettings.fog = false;
        // clip planes
        // camera position?
    }
}
