using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Cinemachine;
using Mono.Cecil;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SearchService;

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
            StartCoroutine(WaitThenRun(1, DoTheThing));
        }

        private void OnDestroy()
        {
            Restore();
        }

        static IEnumerator WaitThenRun(float duration, System.Action action)
        {
            yield return new WaitForSeconds(duration);
            action();
            yield break;
        }

        private static Vector3 oAngle;
        private static Vector3 oPos;
        private static float oBaseMapDist;
        private static float oDetailDist;
        private static float oFarClip;
        private static float oLodBias;

        private static Terrain terrain;
        private static Transform tf;
        private static GameObject player;
        private static CinemachineBrain brain;
        private static new Camera camera;
        private static bool isSnapped = false;
        private static List<GameObject> markers = [];

        private static bool killedCuller = false;

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
            oDetailDist = terrain.detailObjectDistance;
            oLodBias = QualitySettings.lodBias;
        }
        public static void Restore()
        {

            terrain.basemapDistance = oBaseMapDist;
            terrain.detailObjectDistance = oDetailDist;
            tf.eulerAngles = oAngle;
            tf.position = oPos;
            player.SetActive(true);
            brain.enabled = true;
            //don't care about orthosize. game doesn't use ortho (i think)
            camera.orthographic = false;
            camera.farClipPlane = oFarClip;
            RenderSettings.fog = true;
            QualitySettings.lodBias = oLodBias;

            foreach (var marker in markers)
            {
                Object.Destroy(marker);
            }
        }



        public void DoTheThing(/* bool restore = false */)
        {
            var restore = false;
            var topLeft = new Vector3(-116, 100, 274);
            Snapshot();
            UnityEngine.RenderSettings.fog = false;
            camera.farClipPlane = 1e6F;
            camera.orthographic = true;
            //camera.orthographicSize = 250; //island is approximately 800x800 square units. this number is half the height of the camera's view
            camera.orthographicSize = 25; //one tenth
            brain.enabled = false; // Turn off camera control stuff
            player.transform.position = new Vector3(0, 16, 0);
            player.SetActive(false); // Turn off character
            // top left corner:
            tf.position = topLeft; //determined empirically NB: needs to be above highest point on island to prevent clipping
            tf.eulerAngles = new Vector3(90, 0, 0);
            terrain.basemapDistance = 500;
            terrain.detailObjectDistance = 500;
            QualitySettings.lodBias = 100_000f;

            try
            {
                if (!killedCuller)
                {
                    killedCuller = true;
                    var culler = GameObject.FindObjectOfType<ManualDistanceCulling>();
                    culler.gameObject.SetActive(false);
                    foreach (var chunk in culler.chunks)
                    {
                        foreach (var obj in chunk.expensiveChunkObjects)
                        {
                            obj.SetActive(true);
                        }
                        foreach (var ro in chunk.resistantObjects)
                        {
                            ro.gameObject.SetActive(true);
                        }
                        foreach (var obj in chunk.chunkObjects)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                Logger.LogDebug("culler probably already got killed, lol");
            }

            // var cartoonStandard = Shader.Find("Cartoon/Standard");
            var cartoonStandard = GameObject.Find("/Terrain/Map Signs/Map Sign/Sign").GetComponent<MeshRenderer>().sharedMaterial.shader;
            //var waterBlue = new Color(0.7217f, 0.8067f, 1f, 0.8039f); //original lakewater color
            var waterBlue = new Color(0.522f, 0.698f, 0.859f, 0.8039f);
            
            var ogWaterPlane = GameObject.Find("/Terrain/WaterPlane");
            var lakeWaterMat = ogWaterPlane.GetComponent<MeshRenderer>().sharedMaterial;
            ogWaterPlane.transform.localScale = new Vector3(700, 1, 700);
            lakeWaterMat.shader = cartoonStandard;
            lakeWaterMat.color = waterBlue;

            foreach (var renderer in GameObject.Find("/Terrain/Water").GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial.shader = cartoonStandard;
                renderer.sharedMaterial.color = waterBlue;
            }

            foreach (var renderer in GameObject.Find("/West (Forest)/West Water").GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial.shader = cartoonStandard;
                renderer.sharedMaterial.color = waterBlue;
            }

            //NB: no colons allowed in windows filenames
            //SnapPic(camera, $"{System.DateTime.Now.ToString("s").Replace(':', '-')}-snap", RenderTextureFormat.ARGB32);

            // var potPrefabs = Resources.FindObjectsOfTypeAll<ParticlePickup>()
            //     .Where(e => e.name.Equals("Pot Confetti"))
            //     .Select(e => e.gameObject.transform.parent.gameObject);


            // var breakables = Object.FindObjectsOfType<BreakableObject>();
            // var potPositions = breakables.Where(breakable => potPrefabs.Contains(breakable.breakingPrefab)).Select(breakable => breakable.transform.position);
            // var chestPositions = Object.FindObjectsOfType<BreakableObjectMulti>().Select(bom => bom.transform.position);
            // var paths = Object.FindObjectsOfType<ActorPathFollower>();
            // var racePositions = Object.FindObjectsOfType<Racetrack>().Select(racetrack => racetrack.transform.position);
            // //var NPCs = //uhhhh i really dunno for this one

            // foreach (Vector3 potPos in potPositions)
            // {
            //     var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            //     marker.transform.position = new(potPos.x, 90, potPos.z);
            //     marker.transform.localScale *= 1;
            //     marker.GetComponent<Renderer>().material.color = Color.cyan;
            //     markers.Add(marker);
            // }
            // foreach (Vector3 chestPos in chestPositions)
            // {
            //     var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //     marker.transform.position = new(chestPos.x, 90, chestPos.z);
            //     marker.transform.localScale *= 1;
            //     marker.GetComponent<Renderer>().material.color = Color.magenta;
            //     markers.Add(marker);
            // }
            // foreach (Vector3 racePos in racePositions)
            // {
            //     var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //     marker.transform.position = new(racePos.x, 90, racePos.z);
            //     marker.transform.localScale *= 1;
            //     marker.transform.Rotate(0, 45, 0);
            //     marker.GetComponent<Renderer>().material.color = Color.red;
            //     markers.Add(marker);
            // }


            // IEnumerator makeTiles()
            // {
            topLeft = new Vector3(75, 100, 75);
            camera.transform.position = topLeft;
            var orthoSize = 240f; // "radius" (technically only vertical, but i'm doing squares)
            for (int zoomLevel = 0; zoomLevel < 8; zoomLevel++)
            {
                long tileCount = 1L << zoomLevel;
                camera.orthographicSize = orthoSize;
                camera.transform.position = topLeft;
                var stepSize = orthoSize * 2;
                System.IO.Directory.CreateDirectory($"C:\\Users\\na\\Desktop\\LilGatorProject\\MapPics\\Tiles\\{zoomLevel}");
                for (int x = 0; x < tileCount; x++)
                {
                    for (int z = 0; z < tileCount; z++)
                    {
                        camera.transform.position = topLeft + new Vector3(x * stepSize, 0, z * -stepSize);
                        Logger.LogDebug($"{zoomLevel}\\{x}_{z}");
                        SnapPic(camera, $"{zoomLevel}\\{x}_{z}", RenderTextureFormat.ARGB32);
                    }
                }

                orthoSize /= 2;
                topLeft = new Vector3(topLeft.x - orthoSize, 100, topLeft.z + orthoSize);
            }
            // }


            //restore
            if (restore)
            {
                Restore();
            }

        }

        public static void SnapPic(Camera cam, string name, RenderTextureFormat format)
        {
            cam.enabled = false;
            RenderTexture rt = new(256, 256, 32, format);
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
                    System.IO.File.WriteAllBytes($"C:\\Users\\na\\Desktop\\LilGatorProject\\MapPics\\Tiles\\{name}.png", encoded.ToArray());
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
