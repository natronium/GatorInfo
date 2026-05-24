using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Cinemachine;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GatorInfo
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin p;
        public static ManualLogSource l;
        private void Awake()
        {
            p = this;
            l = this.Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogDebug("Press F11 to log stuff positions");
            Logger.LogDebug("Press F12 to generate map tiles (WARNING RESOURCE INTENSIVE!)");
            Snapshot();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Game.State = GameState.Menu; // Render tree canopies (technically sets the proximityfade shader var)
                StartCoroutine(WaitThenRun(5, DoTheThing));
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                LogStuffPositions();
            }
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
            Game.State = GameState.Play;

        }

        public void LogStuffPositions()
        {

            DisableCuller();

            var potPrefabs = Resources.FindObjectsOfTypeAll<ParticlePickup>()
                .Where(e => e.name.Equals("Pot Confetti"))
                .Select(e => e.gameObject.transform.parent.gameObject);


            var breakables = Object.FindObjectsOfType<BreakableObject>();
            var pots = breakables.Where(breakable => potPrefabs.Contains(breakable.breakingPrefab));
            var chests = Object.FindObjectsOfType<BreakableObjectMulti>();
            var races = Object.FindObjectsOfType<Racetrack>();

            Logger.LogDebug("export const pot_info = [");
            foreach (var pot in pots)
            {
                var pos = pot.transform.position;
                var id = pot.id switch
                {
                    1606 => 1695,
                    1638 => 1709,
                    1663 => 1712,

                    _ => pot.id
                };
                var comment = id != pot.id ? $" // {pot.id} => {id} because pot mimic quest" : "";
                Logger.LogDebug($"{{pos:[{pos.z},{pos.x}], id: {id}}},{comment}");
            }
            Logger.LogDebug("];");
            Logger.LogDebug("export const chest_info = [");
            foreach (var chest in chests)
            {
                var pos = chest.transform.position;
                Logger.LogDebug($"{{pos:[{pos.z},{pos.x}], id: {chest.id}}},");
            }
            Logger.LogDebug("];");
            Logger.LogDebug("export const race_info = [");
            foreach (var race in races)
            {
                var pos = race.transform.position;
                Logger.LogDebug($"{{pos:[{pos.z},{pos.x}], id: {race.id}}},");
            }
            Logger.LogDebug("];");

            IEnumerable<DialogueActor> additionalActors = ((List<string>)[
                "/NorthWest (Tutorial Island)/Act 1/Quests/Jill Quest/Studying/Jill",
                "/NorthWest (Tutorial Island)/Act 1/Quests/Avery Quest/Avery",
                "/NorthWest (Tutorial Island)/Act 1/Quests/Martin Quest/Horse",
                "/East (Creeklands)/Cool Kids Quest/Cool CoolKids/Martin",
                "/East (Creeklands)/Cool Kids Quest/Subquests/Wolf Quest/Coolkid Wolf",
                "/East (Creeklands)/Cool Kids Quest/Subquests/Boar Quest/Coolkid Boar", // Grass, Bucket, Water, Leaf
                "/East (Creeklands)/Cool Kids Quest/Subquests/Goose Quest/Coolkid Goose", // detective cowl
                "/West (Forest)/Prep Quest/Subquests/Engineer/Character/Engineer",
                "/West (Forest)/Prep Quest/Subquests/Economist/Character/Gene (Economist)",
                "/West (Forest)/Prep Quest/Subquests/Entomologist/Character/Entomologist",
                "/West (Forest)/Prep Quest/End Sequence/End Actors/Jill",
                "/North (Mountain)/Theatre Quest/Subquests/Space!!!/HawkSpace", //Nerf + quest item
                "/North (Mountain)/Theatre Quest/Subquests/Cowfolk/Cowboy", //cowboy hat?
                "/North (Mountain)/Theatre Quest/Subquests/Vampire/Vampire Bat", //fangs
                "/North (Mountain)/Theatre Quest/Subquests/Vampire/IceCream/PartTimer", //parttimer location + npc
                "/North (Mountain)/Theatre Quest/Introduction/Avery"
            ]).Select(path => Util.GetByPath(path).GetComponent<DialogueActor>());


            Logger.LogDebug("export const npc_info = [");
            foreach (var npc in (List<DialogueActor>)[.. CompletionStats.c.completionActors, .. additionalActors])
            {
                var pos = npc.transform.position;
                Logger.LogDebug($"{{pos:[{pos.z},{pos.x}], name:\"{npc.profile.name}\", internal_name:\"{npc.name}\"}},");
            }
            Logger.LogDebug("];");
            Logger.LogDebug("export const npc_path_info = [");
            var paths = Resources.FindObjectsOfTypeAll<ActorPath>();
            foreach (var path in paths)
            {
                var parentName = path.transform.parent.name;
                // We don't care about the playground paths, or about awkward mouse's unused island path
                if (parentName == "Dynamic Character Paths" || path.name == "Island Path ")
                {
                    continue;
                }
                Logger.LogDebug($"{{name:\"{parentName}/{path.name}\", path_points:[ ");
                for (int i = 0; i < path.positions.Length; i++)
                {
                    var pos = path.GetPosition(i);
                    Logger.LogDebug($"[{pos.z},{pos.x}],");
                }
                if (path.connectEnds)
                {
                    var pos = path.GetPosition(0);
                    Logger.LogDebug($"[{pos.z},{pos.x}],");
                }
                Logger.LogDebug($"]}},");
            }
            Logger.LogDebug("];");

            var singleQuestItems = ((List<(string, string)>)[
                ("/NorthWest (Tutorial Island)/Act 1/Quests/Jill Quest/Sword Grove (1)/Powerup (Stick)", "Stick Pickup"),
                ("/NorthWest (Tutorial Island)/Act 1/Quests/Martin Quest/Pickup", "Pot? Pickup"),
                ("/West (Forest)/Prep Quest/Subquests/Economist/Monsters/ShapeMonster_Square (4)", "Cheese Sandwich Monsters"),
                ("NorthEast (Canyoney)/SideQuests/FetchVulture/Pickup/ScooterBoard Broken", "Broken Scooter"),
                ("/East (Creeklands)/Side Quests/Fetch Quest Shark/Retainer Pickup", "Shark Retainer")
            ]).Select((boop) =>
            {
                var (path, name) = boop;
                return (Util.GetByPath(path).transform, name);
            });
            var specialRocks = Util.GetByPath("/West (Forest)/Prep Quest/Subquests/Engineer/Special Rocks").transform.Cast<Transform>().Select((t, i) => (t, $"Special Rock #{i}"));
            IEnumerable<(Transform, string)> questItems = [.. singleQuestItems, .. specialRocks];
            Logger.LogDebug("export const quest_item_info =[");
            foreach (var (questItem, name) in questItems)
            {
                var pos = questItem.position;
                Logger.LogDebug($"{{pos:[{pos.z},{pos.x}], name:\"{name}\"}},");
            }
            Logger.LogDebug("];");
        }

        public void DoTheThing(/* bool restore = false */)
        {
            var restore = false;
            var topLeft = new Vector3(-116, 100, 274);
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
            terrain.heightmapPixelError = 0;
            terrain.detailObjectDistance = 500;
            QualitySettings.lodBias = 100_000f;

            // shadows actually look kinda bad in the map context
            // GameObject.FindObjectOfType<Light>().layerShadowCullDistances = Enumerable.Repeat(0f, 32).ToArray(); //always shadows!
            // QualitySettings.shadowDistance = 960;
            camera.layerCullDistances = Enumerable.Repeat(0f, 32).ToArray(); //cull nothing!!
            camera.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>().enabled = false; //changes the screen color based on camera coords. not what we want for map

            GameObject.Find("/Camera Local Effects")?.SetActive(false); //leaves and wind lines
            Logger.LogDebug("Initial setup complete");
            DisableCuller();

            DisableTreeLODs();

            FlattenWater();

            TakeTilePics();

            //restore
            if (restore)
            {
                Restore();
            }

        }

        public void FlattenWater()
        {
            Logger.LogDebug("Flattening Water");
            // var cartoonStandard = Shader.Find("Cartoon/Standard");
            var cartoonStandard = GameObject.Find("/Terrain/Map Signs/Map Sign/Sign").GetComponent<MeshRenderer>().sharedMaterial.shader;
            //var waterBlue = new Color(0.7217f, 0.8067f, 1f, 0.8039f); //original lakewater color
            var waterBlue = new Color(0.522f, 0.698f, 0.859f, 0.8039f);

            var waterPlane = GameObject.Find("/Terrain/WaterPlane");
            var lakeWaterMat = waterPlane.GetComponent<MeshRenderer>().sharedMaterial;
            waterPlane.transform.localScale = new Vector3(700, 1, 700);
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
        }

        public void DisableTreeLODs()
        {
            Logger.LogDebug("Disabling tree lods");
            foreach (var lodtree in LODTree.instances)
            {
                lodtree.gameObject.GetComponent<LODGroup>().enabled = false;
                foreach (var cube in lodtree.cubes)
                {
                    cube.gameObject.SetActive(false);
                }
                foreach (var billboard in lodtree.billboards)
                {
                    billboard.gameObject.SetActive(false);
                }
                foreach (var low in lodtree.lows)
                {
                    low.gameObject.SetActive(false);
                }
            }
        }

        public void TakeTilePics()
        {
            var topLeft = new Vector3(75, 100, 75);
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
        }

        public void DisableCuller()
        {
            Logger.LogDebug("disabling culler");
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
