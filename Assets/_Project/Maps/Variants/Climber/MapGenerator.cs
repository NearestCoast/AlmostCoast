#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.ActionStates.MeleeAttacks;
using _Project.Inventories.Items;
using _Project.Maps.Climber.Objects;
using _Project.Maps.Climber.Objects.Collectables;
using _Project.Maps.Climber.Objects.Variants;
using _Project.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;
using Random = UnityEngine.Random;

namespace _Project.Maps.Climber
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private Level.Type levelType;
        [SerializeField] private bool useDeco;
        [SerializeField] private bool optimizeTexture;
        [SerializeField] private float textureScale;
        [SerializeField] private GameObject cloneObj;
        [SerializeField] private List<Level> levels = new List<Level>();
        // [SerializeField] private GameObject ringdongPrefab;
        [SerializeField] private Transform mapInstanceContainer;
        [SerializeField] private Transform enemiesContainer;

        [SerializeField, TitleGroup("Prefabs")] private Transform objectPrefabContainer;
        [SerializeField, TitleGroup("Prefabs")] private Strawberry strawberryPrefab;
        [FormerlySerializedAs("abilityInstance")] [SerializeField, TitleGroup("Prefabs")] private Ability abilityPrefab;
        [SerializeField, TitleGroup("Prefabs")] private WallJumpCountUp wallJumpCountUpPrefab;
        [SerializeField, TitleGroup("Prefabs")] private SlideJumpUp slideJumpUpPrefab;
        [SerializeField, TitleGroup("Prefabs")] private MovingPlatform movingPlatformPrefab;
        [SerializeField, TitleGroup("Prefabs")] private SpotLight spotLightPrefab;
        [FormerlySerializedAs("silverKeyPrefab")] [SerializeField, TitleGroup("Prefabs")] private KeyCollectable silverKeyCollectablePrefab;
        [FormerlySerializedAs("goldKeyPrefab")] [SerializeField, TitleGroup("Prefabs")] private KeyCollectable goldKeyCollectablePrefab;

        [SerializeField] private NavMeshSurface navMeshSurface;

        private void OnValidate()
        { 
            if (Application.isPlaying) return;   

            if (!mapInstanceContainer) mapInstanceContainer = Extensions.FindInactiveObjectByName("MapInstances").transform;
            if (!enemiesContainer) enemiesContainer = Extensions.FindInactiveObjectByName("EnemySpotsContainer").transform;
            if (!objectPrefabContainer) objectPrefabContainer = Extensions.FindInactiveObjectByName("ObjectPrefabs").transform;

            AssignPrefab(ref strawberryPrefab, "Strawberry");
            AssignPrefab(ref abilityPrefab, "AbilityInstance");
            AssignPrefab(ref wallJumpCountUpPrefab, "WallJumpCountUp");
            AssignPrefab(ref slideJumpUpPrefab, "SlideJumpUp");
            AssignPrefab(ref movingPlatformPrefab, "MovingPlatformInstance");
            AssignPrefab(ref spotLightPrefab, "SpotLightInstance");
            AssignPrefab(ref silverKeyCollectablePrefab, "SilverKey");
            AssignPrefab(ref goldKeyCollectablePrefab, "GoldKey");

            navMeshSurface ??= Extensions.FindInactiveObjectByName("NavMesh Surface").GetComponent<NavMeshSurface>();
        }

        private void AssignPrefab<T>(ref T prefabField, string objectName) where T : Component
        {
            if (prefabField != null) return;

            var instance = Extensions.FindInactiveObjectByName(objectName);
            if (instance == null)
            {
                Debug.LogWarning($"Object '{objectName}' not found in the scene.");
                return;
            }

            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab source for '{objectName}' not found.");
                return;
            }

            prefabField = prefab.GetComponent<T>();
        }

        [Button]
        private void PutColliders()
        {
            levels = new List<Level>();
            var portals = new List<Portal>();
            
            if (cloneObj) DestroyImmediate(cloneObj);
            cloneObj = Instantiate(gameObject, mapInstanceContainer);
            DestroyImmediate(cloneObj.GetComponent<MapGenerator>());
            var cloneChapter = cloneObj.AddComponent<Chapter>();
            cloneChapter.Levels = levels;
            cloneChapter.Portals = portals;

            gameObject.SetActive(false);
            cloneObj.SetActive(true);

            RemoveColliders();

            var collectableCount = 0;

            var movingPlatform_Start_Dict = new Dictionary<string, MovingPlatform>();
            var movingPlatform_End_Dict = new Dictionary<string, Transform>();

            var dingdongDoor_Start_Dict = new Dictionary<string, DingdongDoor>();
            var dingdongDoor_End_Dict = new Dictionary<string, Transform>();
            
            var rollingCubeDict = new Dictionary<string, RollingCube>();
            var dingdongLockRollingCubesDict = new Dictionary<string, DingdongLockRollingCube>();
            var ringdongDict = new Dictionary<string, List<Dingdong>>();
            
            var brokableDict = new Dictionary<string, Brokable>();
            
            var decoMPDict = new Dictionary<string, Transform>();
            var decoBrokableDict = new Dictionary<string, Transform>();

            var leverDict = new Dictionary<string, Lever>();
            var leverDoorDict = new Dictionary<string, LeverDoor>();
            var leverDoorEndDict = new Dictionary<string, Transform>();
            
            var arrivalPortalDict = new Dictionary<string, Portal>();
            var portalSavePointDict = new Dictionary<string, SavePoint>();

            var enemySpotsToSpawnDict = new Dictionary<string, GameObject>();
            for (var i = 0; i < enemiesContainer.transform.childCount; i++)
            {
                var enemySpotObj = enemiesContainer.transform.GetChild(i).gameObject;
                var split = enemySpotObj.name.Split("_");
                var id = split[1];
                enemySpotsToSpawnDict.Add(id, enemySpotObj);
            }

            for (var i = 0; i < cloneObj.transform.childCount; i++)
            {
                var collectionT = cloneObj.transform.GetChild(i);

                if (collectionT.name.Contains("RollingCube"))
                {
                    foreach (var exist in collectionT.GetComponents<RollingCube>()) DestroyImmediate(exist);
                    foreach (var exist in collectionT.GetComponentsInChildren<RollingCube>()) DestroyImmediate(exist);

                    RollingCube rollingCube;
                    
                    if (collectionT.name.Contains("StandUp"))
                    {
                        rollingCube = collectionT.gameObject.AddComponent<StandUpRollingCube>();
                    }
                    else if (collectionT.name.Contains("DingdongLock")) 
                    {
                        rollingCube = collectionT.gameObject.AddComponent<DingdongLockRollingCube>();
                    }
                    else 
                    {
                        rollingCube = collectionT.gameObject.AddComponent<RollingCubeOld>();
                    }
                    
                    var id = collectionT.name.Split(".")[0] + collectionT.name.Split(".")[1];
                    rollingCube.ID = id;
                    rollingCube.LevelID = collectionT.name.Split(".")[3];

                    if (collectionT.name.Split(".").Length > 4)
                    {
                        var duration = collectionT.name.Split(".")[4];
                        rollingCube.RotationDuration = float.Parse(duration) / 1000;
                    }

                    if (collectionT.name.Split(".").Length > 5)
                    {
                        var inverse = collectionT.name.Split(".")[5];
                        rollingCube.Inverse = inverse == "i";
                    }
                    

                    var rb = rollingCube.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    
                    rollingCubeDict.Add(id, rollingCube);
                    if (rollingCube is DingdongLockRollingCube dingdongLockRollingCube) dingdongLockRollingCubesDict.Add(rollingCube.ID, dingdongLockRollingCube);
                    
                    continue;
                }

                foreach (var exist in collectionT.GetComponents<Level>()) DestroyImmediate(exist);

                var split = collectionT.name.Split(".");
                var level = collectionT.gameObject.AddComponent<Level>();
                level.LevelType = levelType;
                if (split.Length > 1)
                {
                    var id = collectionT.name.Split(".")[1];
                    level.ID = $"{gameObject.name}_{id}";
                }
                else
                {
                    level.ID = $"{gameObject.name}_000";
                }
                levels.Add(level);

                TraverseCollectionLevel(collectionT, level, out var dingdongDict, out ringdongDict);

                foreach (var movingPlatform in movingPlatform_Start_Dict.Values)
                {
                    var id = movingPlatform.ID;
                    movingPlatform.TargetPosition = movingPlatform_End_Dict[id].position;

                    if (movingPlatform is FallingMovingPlatform fallingMovingPlatform)
                    {
                        fallingMovingPlatform.Rope = brokableDict[fallingMovingPlatform.RopeBrokableID];
                    }

                    if (movingPlatform is LockedMovingPlatform lockedMovingPlatform)
                    {
                        var lever = leverDict[lockedMovingPlatform.LeverID];
                        lever.ConnectedUnlockable = lockedMovingPlatform;
                        lockedMovingPlatform.Lever = lever;
                        lever.transform.SetParent(lockedMovingPlatform.transform);
                    }
                }

                foreach (var dingdongDoor in dingdongDoor_Start_Dict.Values)
                {
                    var id = dingdongDoor.ID;
                    dingdongDoor.TargetPosition = dingdongDoor_End_Dict[id].position;
                }

                foreach (var keyValuePair in dingdongDict)
                {
                    var dingdongDoorID = keyValuePair.Key;
                    var value = keyValuePair.Value;

                    var dingdongDoor = dingdongDoor_Start_Dict[dingdongDoorID];
                    dingdongDoor.Dingdongs = value;
                }

                foreach (var pair in decoMPDict)
                {
                    var decoParentId = pair.Key;
                    var decoT = pair.Value;
                    decoT.SetParent(movingPlatform_Start_Dict[decoParentId].transform);
                }

                foreach (var pair in decoBrokableDict)
                {
                    var decoParentId = pair.Key;
                    var decoT = pair.Value;
                    decoT.SetParent(brokableDict[decoParentId].transform);
                }
            }

            // Find RollingCube's Level And Push To It
            foreach (var rollingCube in rollingCubeDict.Values)
            {
                var level = levels.Find(x => x.ID == rollingCube.LevelID);
                rollingCube.Level = level;
                level.RollingCubes.Add(rollingCube);

                rollingCube.transform.SetParent(level.transform);
             
                
                TraverseCollectionLevel(rollingCube.transform, level, out var dingdongDict, out var emptyDict);

                foreach (var keyValuePair in ringdongDict)
                {
                    var mastarID = keyValuePair.Key;
                    var value = keyValuePair.Value;

                    var dingdongDoor = dingdongLockRollingCubesDict[mastarID];
                    dingdongDoor.Ringdongs = value;
                }
            }
            
            foreach (var leverDoor in leverDoorDict.Values)
            {
                var lever = leverDict[leverDoor.LeverID];
                lever.ConnectedUnlockable = leverDoor;
                
                leverDoor.Lever = lever;
                var endTransform = leverDoorEndDict[leverDoor.ID];
                leverDoor.TargetPosition = endTransform.position;
            }
            
            foreach (var portal in arrivalPortalDict.Values)
            {
                portal.TargetSavePoint = portalSavePointDict[portal.ID];
            }
            
            if (optimizeTexture)
            {
                foreach (var level in levels)
                {
                    var uvScaler = level.gameObject.AddComponent<UVScaler>();
                    // uvScaler.ScaleUVsToTexelDensity(textureScale);
                    uvScaler.ScaleUVsToTexelDensity(Random.Range(2, 6));
                }
            }

            navMeshSurface.BuildNavMesh();
            
            
            return;
            void TraverseCollectionLevel(Transform T, Level level, out Dictionary<string, List<Dingdong>> dingdongDict, out Dictionary<string, List<Dingdong>> ringdongDict)
            {
                dingdongDict = new Dictionary<string, List<Dingdong>>();
                ringdongDict = new Dictionary<string, List<Dingdong>>();

                var spotLightContainerDict = new Dictionary<string, SpotLightSwitch>();
                var spotLightSwitchList = new List<SpotLightSwitch>();
                var pointLightList = new List<GameObject>();
                var spotLightChildList = new List<SpotLight>();
                
                for (var i = 0; i < T.childCount; i++)
                {
                    var child = T.GetChild(i).gameObject;
                    var split = child.name.Split("|");
                    if (useDeco)
                    {
                        if (split[1].Contains("Deco"))
                        {
                            if (split[1].Split(".").Length > 2)
                            {
                                var decoName = split[1].Split(".")[2];

                                if (decoName.Contains("MP"))
                                {
                                    if (decoName.Contains("Falling"))
                                    {
                                        var mpID = split[1].Split(".")[3].Split("_")[0];
                                        // Debug.Log(mpID + ", " + child.name); 
                                        decoMPDict.Add(mpID, child.transform);
                                    }
                                    else
                                    {
                                        var mpID = split[1].Split(".")[3].Split("_")[0];
                                        // Debug.Log(mpID + ", " + child.name);
                                        decoMPDict.Add(mpID, child.transform);
                                    }
                                }
                                else if (decoName.Contains("Brokable"))
                                {
                                    var mpID = split[1].Split(".")[3].Split("_")[0];
                                    // Debug.Log(mpID + ", " + child.name);
                                    decoBrokableDict.Add(mpID, child.transform);
                                }
                            }
                        }
                        else
                        {
                            if (!child.GetComponent<MeshRenderer>()) Debug.Log(T.name + ", " + child.name);
                            else child.GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                    else
                    {
                        if (split[1].Contains("Deco"))
                        {
                            if (!child.GetComponent<MeshRenderer>()) Debug.Log(T.name + ", " + child.name);
                            else child.GetComponent<MeshRenderer>().enabled = false;                            
                        }
                    }

                    // Debug.Log(split[1]);
                    // Debug.Log(child.name);
                    if (split[1].Contains("Bound"))
                    {
                        child.layer = LayerMask.NameToLayer("Bound");
                        var meshCollider = child.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        meshCollider.isTrigger = true;
                        var bound = child.AddComponent<Bound>();
                        bound.Level = level;
                        child.GetComponent<MeshRenderer>().enabled = false;
                        
                        // child.gameObject.SetActive(false);
                    }

                    if (split[1].Contains("Ceiling"))
                    {
                        child.layer = LayerMask.NameToLayer("Ceiling");

                        var boxCollider1 = child.AddComponent<BoxCollider>();
                        // child.gameObject.SetActive(true);
                    }

                    if (split[1].Contains("MP"))
                    {
                        if (split[1].Contains("Falling"))
                        {
                            child.layer = LayerMask.NameToLayer("Ground");
                            var mpSplit = split[1].Split(".");

                            var id = $"FallingMP_{gameObject.name}_{ mpSplit[1]}";
                            var position = mpSplit[2];

                            if (position.Contains("Start"))
                            {
                                var exists = child.GetComponents<MovingPlatform>();
                                foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                                child.gameObject.tag = "IgnoreCamCollider";
                                var mp = child.AddComponent<FallingMovingPlatform>();
                                var audioSource = mp.gameObject.AddComponent<AudioSource>();
                                audioSource.clip = movingPlatformPrefab.FinishAudioSource.clip;
                                audioSource.playOnAwake = false;
                                audioSource.volume = 0.3f;
                                audioSource.maxDistance = 50;
                                
                                mp.Level = level;
                                movingPlatform_Start_Dict.Add(id, mp);
                                mp.ID = id;

                                mp.RopeBrokableID = $"{gameObject.name}_{mpSplit[3].Split("_")[0]}";
                                // mp.EaseCurveStart = movingPlatformEaseCurveStart;
                                // mp.EaseCurveBack = movingPlatformEaseCurveEnd;

                                var boxCollider1 = child.AddComponent<BoxCollider>();
                                // var boxCollider2 = child.AddComponent<BoxCollider>();
                                // boxCollider2.isTrigger = true;
                                // boxCollider2.center = new Vector3(0, boxCollider2.size.y, 0);

                                level.MovingPlatforms.Add(mp);
                            }
                            else if (position.Contains("End"))
                            {
                                movingPlatform_End_Dict.Add(id, child.transform);
                                child.gameObject.SetActive(false);
                            }
                        }
                        else if (split[1].Contains("Locked"))
                        {
                            child.layer = LayerMask.NameToLayer("Ground");
                            var mpSplit = split[1].Split(".");

                            var id = $"LockedMP_{gameObject.name}_{mpSplit[1]}";
                            var position = mpSplit[2];

                            if (position.Contains("Start"))
                            {
                                var exists = child.GetComponents<MovingPlatform>();
                                foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                                child.gameObject.tag = "IgnoreCamCollider";
                                var mp = child.AddComponent<LockedMovingPlatform>();
                                var audioSource = mp.gameObject.AddComponent<AudioSource>();
                                audioSource.clip = movingPlatformPrefab.FinishAudioSource.clip;
                                audioSource.playOnAwake = false;
                                audioSource.volume = 0.3f;
                                mp.Level = level;
                                movingPlatform_Start_Dict.Add(id, mp);
                                mp.ID = id;
                                
                                var leverId = mpSplit[3].Split("_")[0];
                                mp.LeverID = leverId;

                                var boxCollider1 = child.AddComponent<BoxCollider>();

                                level.MovingPlatforms.Add(mp);
                            }
                            else if (position.Contains("End"))
                            {
                                movingPlatform_End_Dict.Add(id, child.transform);
                                child.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            // MP.001.End_L_level_0_2_3.blend
                            child.layer = LayerMask.NameToLayer("Ground");
                            var mpSplit = split[1].Split(".");

                            var id = $"AccMP_{gameObject.name}_{mpSplit[1]}";
                            var position = mpSplit[2];

                            if (position.Contains("Start"))
                            {
                                var exists = child.GetComponents<MovingPlatform>();
                                foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                                child.gameObject.tag = "IgnoreCamCollider";
                                var mp = child.AddComponent<AccMovingPlatform>();
                                var audioSource = mp.gameObject.AddComponent<AudioSource>();
                                audioSource.clip = movingPlatformPrefab.FinishAudioSource.clip;
                                audioSource.playOnAwake = false;
                                audioSource.volume = 0.3f;
                                mp.Level = level;
                                movingPlatform_Start_Dict.Add(id, mp);
                                mp.ID = id;
                                // mp.EaseCurveStart = movingPlatformEaseCurveStart;
                                // mp.EaseCurveBack = movingPlatformEaseCurveEnd;

                                var boxCollider1 = child.AddComponent<BoxCollider>();
                                // var boxCollider2 = child.AddComponent<BoxCollider>();
                                // boxCollider2.isTrigger = true;
                                // boxCollider2.center = new Vector3(0, boxCollider2.size.y, 0);

                                level.MovingPlatforms.Add(mp);
                            }
                            else if (position.Contains("End"))
                            {
                                movingPlatform_End_Dict.Add(id, child.transform);
                                child.gameObject.SetActive(false);
                            }
                        }
                        
                    }
                    else if (split[1].Contains("Bubble"))
                    {
                        // child.layer = LayerMask.NameToLayer("Ground");
                        var bubble = child.AddComponent<Bubble>();
                        var sphereCollider = child.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                        // child.gameObject.SetActive(false);
                    }
                    else if (split[1].Contains("Lever"))
                    {
                        var ddSplit = split[1].Split(".");
                        var id = ddSplit[1].Split("_")[0];
                        
                        child.layer = LayerMask.NameToLayer("Ground");
                        var lever = child.AddComponent<Lever>();
                        // lever.ID = id;
                        child.AddComponent<BoxCollider>();
                        leverDict.Add(id, lever);
                    }
                    else if (split[1].Contains("Portal"))
                    {
                        var boxCollider = child.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                        var portal = child.AddComponent<Portal>();
                        var split2 = split[1].Split(".");
                        var position = split2[2];
                        portal.ID = split2[1];
                        portals.Add(portal);
                        if (position.Contains("Start"))
                        {
                            portal.PortalType = Portal.Type.Depart;
                            portal.ArrivalID = split2[3].Split("_")[0];
                        }
                        else if (position.Contains("End"))
                        {
                            portal.PortalType = Portal.Type.Arrival;
                            arrivalPortalDict.Add(portal.ID, portal);
                        }
                    }
                    else if (split[1].Contains("KDoor"))
                    {
                        child.AddComponent<BoxCollider>();
                        var split2 = split[1].Split(".");
                        var position = split2[2];
                        var id = level.ID + split2[0] + split2[1];
                        
                        if (position.Contains("Start"))
                        {
                            var leverId = split2[3].Split("_")[0];
                            var keyDoor = child.AddComponent<KeyDoor>();
                            keyDoor.ID = id;
                            
                            keyDoor.LeverID = leverId;
                            // Debug.Log(leverId);

                            var keyType = split2[4];
                            if (keyType.Contains("SilverKey"))
                            {
                                keyDoor.KeyType = KeyData.KeyType.Silver;
                            }
                            else if (keyType.Contains("SilverKey"))
                            {
                                keyDoor.KeyType = KeyData.KeyType.Gold;
                            }
                            
                            leverDoorDict.Add(keyDoor.ID, keyDoor);
                        }
                        else if (position.Contains("End"))
                        {
                            leverDoorEndDict.Add(id, child.transform);
                            child.gameObject.SetActive(false);
                        }
                    }
                    else if (split[1].Contains("LDoor")) 
                    {
                        child.AddComponent<BoxCollider>();
                        var split2 = split[1].Split(".");
                        var position = split2[2];
                        
                        if (position.Contains("Start"))
                        {
                            var id = split2[0] + split2[1];
                            
                            var leverId = split2[3].Split("_")[0];
                            var leverDoor = child.AddComponent<LeverDoor>();
                            leverDoor.ID = id;
                            leverDoor.LeverID = leverId;
                            leverDoorDict.Add(id, leverDoor);
                        }
                        else if (position.Contains("End"))
                        {
                            var id = split2[0] + split2[1];
                            
                            leverDoorEndDict.Add(id, child.transform);
                            child.gameObject.SetActive(false);
                        }
                    }
                    else if (split[1].Contains("DingdongDoor"))
                    {
                        child.layer = LayerMask.NameToLayer("Ground");
                        var ddSplit = split[1].Split(".");

                        var id = ddSplit[1];
                        var position = ddSplit[2];

                        if (position.Contains("Start"))
                        {
                            var exists = child.GetComponents<DingdongDoor>();
                            foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                            var dd = child.AddComponent<DingdongDoor>();
                            dd.Level = level;
                            dingdongDoor_Start_Dict.Add(id, dd);
                            dd.ID = id;
                            // mp.EaseCurveStart = movingPlatformEaseCurveStart;
                            // mp.EaseCurveBack = movingPlatformEaseCurveEnd;

                            var boxCollider1 = child.AddComponent<BoxCollider>();
                            // var boxCollider2 = child.AddComponent<BoxCollider>();
                            // boxCollider2.isTrigger = true;
                            // boxCollider2.center = new Vector3(0, boxCollider2.size.y, 0);

                            level.DingdongDoors.Add(dd);
                        }
                        else if (position.Contains("End"))
                        {
                            dingdongDoor_End_Dict.Add(id, child.transform);
                            child.gameObject.SetActive(false);
                        }
                    }
                    else if (split[1].Contains("Dingdong") && !split[1].Contains("DingdongLockRollingCube"))
                    {
                        var ddSplit = split[1].Split(".");

                        var doorID = ddSplit[1];

                        var dingdong = child.AddComponent<Dingdong>();
                        dingdong.masterID = doorID;

                        if (dingdongDict.ContainsKey(doorID))
                        {
                            var dingdongs = dingdongDict[doorID];
                            dingdongs.Add(dingdong);
                        }
                        else
                        {
                            dingdongDict.Add(doorID, new List<Dingdong>());
                            var dingdongs = dingdongDict[doorID];
                            dingdongs.Add(dingdong);
                        }


                        child.GetComponent<MeshRenderer>().enabled = true;
                        var sphereCollider = child.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    
                    else if (split[1].Contains("Ringdong"))
                    {
                        var ddSplit = split[1].Split(".");

                        var mastarID = "DingdongLockRollingCube" + ddSplit[1];

                        var dingdong = child.AddComponent<Dingdong>();
                        dingdong.masterID = mastarID;

                        if (ringdongDict.ContainsKey(mastarID))
                        {
                            var dingdongs = ringdongDict[mastarID];
                            dingdongs.Add(dingdong);
                        }
                        else
                        {
                            ringdongDict.Add(mastarID, new List<Dingdong>());
                            var dingdongs = ringdongDict[mastarID];
                            dingdongs.Add(dingdong);
                        }


                        child.GetComponent<MeshRenderer>().enabled = true;

                        var sphereCollider = child.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    
                    else if (split[1].Contains("Wall"))
                    {
                        child.layer = LayerMask.NameToLayer("Wall");
                        var meshCollider = child.AddComponent<MeshCollider>();
                        var rb = child.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        // child.gameObject.SetActive(false);
                    }
                    else if (split[1].Contains("Hazard"))
                    {   
                        child.layer = LayerMask.NameToLayer("Hazard");
                        // child.gameObject.SetActive(false);
                        var meshRenderer = child.GetComponent<MeshRenderer>();
                        // meshRenderer.enabled = false;
                        var meshCollider = child.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        meshCollider.isTrigger = true;

                        var exists = child.GetComponentsInChildren<Hazard>();
                        foreach (var exist in exists)
                        { 
                            DestroyImmediate(exist);
                        }

                        var hazard = child.AddComponent<Hazard>();
                        hazard.Level = level;   
                        level.Hazards.Add(hazard);
                    }
                    else if (split[1].Contains("Brokable") && !split[1].Contains("Deco"))
                    {   
                        var exists = child.GetComponentsInChildren<Brokable>();
                        foreach (var exist in exists)
                        { 
                            DestroyImmediate(exist);
                        }
                        
                        child.layer = LayerMask.NameToLayer("Ground");
                        var mpSplit = split[1].Split(".");
                        var id = $"{gameObject.name}_{mpSplit[1].Split("_")[0]}";

                        var brokable = child.AddComponent<Brokable>();
                        brokable.ID = id;
                        // Debug.Log(id);
                        brokableDict.Add(id, brokable);
                        
                        var rb = child.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        var boxCollider = child.AddComponent<BoxCollider>();
                    }
                    else if (split[1].Contains("WJumpCountUp"))
                    {
                        child.GetComponent<MeshRenderer>().enabled = false;
                        var collectable = Instantiate(wallJumpCountUpPrefab, child.transform);
                        collectable.ID = $"Collectable_{level.ID}{collectableCount++}";
                        collectable.gameObject.SetActive(true);
                    }
                    else if (split[1].Contains("SlideJumpUp"))
                    {
                        var levelReward = split[1].Split(".")[1];
                        
                        child.GetComponent<MeshRenderer>().enabled = false;

                        if (levelReward.Contains("LevelReward"))
                        {
                            level.RewardPrefabs.Add(slideJumpUpPrefab.gameObject);
                            level.RewardObjectPositions.Add(child.transform.position);
                        }
                        else
                        {
                            var collectable = Instantiate(slideJumpUpPrefab, child.transform);
                            collectable.ID = $"Collectable_{level.ID}{collectableCount++}";
                            collectable.gameObject.SetActive(true);
                        }
                    }
                    else if (split[1].Contains("Strawberry"))
                    {
                        child.GetComponent<MeshRenderer>().enabled = false;
                        var collectable = Instantiate(strawberryPrefab, child.transform);
                        collectable.ID = $"Collectable_{level.ID}{collectableCount++}";
                        collectable.gameObject.SetActive(true);
                    }
                    else if (split[1].Contains("Ability"))
                    {
                        child.GetComponent<MeshRenderer>().enabled = false;
                        var abilityName = split[1].Split(".")[1];
                        var playerCharacter = FindAnyObjectByType<PlayerCharacter>();
                        var collectable = Instantiate(abilityPrefab, child.transform);
                        collectable.ID = $"Collectable_{level.ID}{collectableCount++}";
                        collectable.gameObject.SetActive(true);
                        
                        if (abilityName.Contains("Attack"))
                        {
                            collectable.TargetStates = new List<AnimationState>()
                            {
                                playerCharacter.transform.GetComponentInChildrenOfType<Attack_01>(true),
                                playerCharacter.transform.GetComponentInChildrenOfType<Attack_02>(true),
                                playerCharacter.transform.GetComponentInChildrenOfType<Attack_03>(true),
                            };
                        }
                        else
                        {
                            if (abilityName.Contains("Jump"))
                            {
                                collectable.TargetStates = new List<AnimationState>() { playerCharacter.transform.GetComponentInChildrenOfType<JumpState>(true) };
                            }
                            else if (abilityName.Contains("SlideDash"))
                            {
                                collectable.TargetStates = new List<AnimationState>() { playerCharacter.transform.GetComponentInChildrenOfType<SlideDashState>(true) };
                            }
                            else if (abilityName.Contains("GroundPounding"))
                            {
                                collectable.TargetStates = new List<AnimationState>() { playerCharacter.transform.GetComponentInChildrenOfType<GroundPoundingState>(true) };
                            }
                            else if (abilityName.Contains("Climb"))
                            {
                                collectable.TargetStates = new List<AnimationState>() { playerCharacter.transform.GetComponentInChildrenOfType<ClimbState>(true) };
                            }
                        }
                    }
                    else if (split[1].Contains("SpotLight"))
                    {
                        var descriptionSplit = split[1].Split("_")[0].Split(".");
                        if (descriptionSplit.Length > 2)
                        {
                            var description = descriptionSplit[2];
                            if (description == "Static")
                            {
                                
                                var spotLightSwitch = child.AddComponent<SpotLightSwitch>();
                                spotLightSwitch.GetComponent<MeshRenderer>().enabled = false;
                                // var col = child.AddComponent<BoxCollider>();
                                // var trigger = child.AddComponent<BoxCollider>();
                                // trigger.isTrigger = true;
                                spotLightSwitch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                                child.layer = LayerMask.NameToLayer("GroundUnlit");
                                var rb = child.AddComponent<Rigidbody>();
                                rb.isKinematic = true;
                                spotLightSwitchList.Add(spotLightSwitch);
                        
                                var spotLight = Instantiate(spotLightPrefab);
                                spotLight.transform.position = child.transform.position;
                                spotLight.gameObject.SetActive(true);
                                spotLight.OwnSwitch = spotLightSwitch;

                                if (spotLightSwitch.SpotLights is null) spotLightSwitch.SpotLights = new List<SpotLight>();
                                spotLightSwitch.SpotLights.Add(spotLight);

                                spotLight.OnValidate();
                                spotLight.LightCompo.spotAngle = 50;
                                spotLight.IsStatic = true;
                                spotLight.TurnOn();
                            }
                            else if (description == "Child")
                            {
                                var extraSwitchID = descriptionSplit[3];
                                
                                var spotLightSwitch = child.AddComponent<SpotLightSwitch>();
                                var col = child.AddComponent<BoxCollider>();
                                // var trigger = child.AddComponent<BoxCollider>();
                                // trigger.isTrigger = true;
                                spotLightSwitch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                                child.layer = LayerMask.NameToLayer("GroundUnlit");
                                var rb = child.AddComponent<Rigidbody>();
                                rb.isKinematic = true;
                                spotLightSwitchList.Add(spotLightSwitch);
                        
                                var spotLight = Instantiate(spotLightPrefab);
                                spotLight.transform.position = child.transform.position;
                                spotLight.gameObject.SetActive(true);
                                spotLight.OwnSwitch = spotLightSwitch;

                                if (spotLightSwitch.SpotLights is null) spotLightSwitch.SpotLights = new List<SpotLight>();
                                spotLightSwitch.SpotLights.Add(spotLight);

                                // Debug.Log(switchID);
                                spotLight.ContainerSwitchID = extraSwitchID;
                                spotLightChildList.Add(spotLight);
                                
                                spotLight.TurnOff();

                                if (descriptionSplit.Length > 4)
                                {
                                    if (descriptionSplit[4] == "SwitchOnly")
                                    {
                                        spotLight.IsSwitchOnly = true;
                                    }
                                }
                            }
                            else if (description == "Container")
                            {
                                var spotLightSwitch = child.AddComponent<SpotLightSwitch>();
                                var col = child.AddComponent<BoxCollider>();
                                // var trigger = child.AddComponent<BoxCollider>();
                                // trigger.isTrigger = true;
                                spotLightSwitch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                                child.layer = LayerMask.NameToLayer("GroundUnlit");
                                var rb = child.AddComponent<Rigidbody>();
                                rb.isKinematic = true;
                                
                                spotLightSwitch.SpotLights = new List<SpotLight>();
                                
                                
                                spotLightSwitch.ID = descriptionSplit[1];
                                spotLightContainerDict.Add(spotLightSwitch.ID, spotLightSwitch);

                                var spotLight = Instantiate(spotLightPrefab, child.transform);
                                var meshCollider = spotLight.GetComponentInChildren<MeshCollider>();
                                meshCollider.enabled = false;
                                spotLight.LightCompo.intensity = 0;
                                spotLightSwitch.ContainerLight = spotLight;
                                spotLight.gameObject.SetActive(true);
                                spotLight.OwnSwitch = spotLightSwitch;
                                spotLight.TurnOff();
                            }
                        }
                        else
                        {
                            var spotLightSwitch = child.AddComponent<SpotLightSwitch>();
                            var col = child.AddComponent<BoxCollider>();
                            // var trigger = child.AddComponent<BoxCollider>();
                            // trigger.isTrigger = true;
                            spotLightSwitch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                            child.layer = LayerMask.NameToLayer("GroundUnlit");
                            var rb = child.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            spotLightSwitchList.Add(spotLightSwitch);
                        
                            var spotLight = Instantiate(spotLightPrefab);
                            spotLight.transform.position = child.transform.position;
                            spotLight.gameObject.SetActive(true);
                            spotLight.OwnSwitch = spotLightSwitch;

                            if (spotLightSwitch.SpotLights is null) spotLightSwitch.SpotLights = new List<SpotLight>();
                            spotLightSwitch.SpotLights.Add(spotLight);

                            spotLight.OnValidate();
                            spotLight.LightCompo.spotAngle = 50;
                            
                            spotLight.IsStatic = false;
                            spotLight.TurnOff();
                        }
                        
                    }
                    else if (split[1].Contains("PointLight"))
                    {
                        var pointLightInstance = Extensions.FindInactiveObjectByName("PointLightInstance");
                        var pointLightObj = Instantiate(pointLightInstance);
                        pointLightObj.transform.position = child.transform.position;
                        pointLightObj.SetActive(true);
                        pointLightList.Add(pointLightObj);
                    }
                    else if (split[1].Contains("Save"))
                    {
                        child.layer = LayerMask.NameToLayer("Save");

                        var meshRenderer = child.GetComponent<MeshRenderer>();
                        meshRenderer.enabled = false;
                        var meshCollider = child.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        meshCollider.isTrigger = true;

                        var exists = child.GetComponentsInChildren<SavePoint>();
                        foreach (var exist in exists)
                        {
                            DestroyImmediate(exist);
                        }

                        var savePoint = child.AddComponent<SavePoint>();
                        savePoint.Level = level;
                        // level.SavePoint = savePoint;

                        if (split[1].Contains("InitialSavePoint"))
                        {
                            FindAnyObjectByType<PlayerCharacter>().InitialSavePoint = savePoint;
                        }
                        
                        var split2 = split[1].Split(".");
                        if (split2.Length > 1)
                        {
                            if (split2[1].Contains("PEnd"))
                            {
                                var portalID = split2[2].Split("_")[0];
                                portalSavePointDict.Add(portalID, savePoint);
                            }
                        }
                    }
                    else if (split[1].Contains("Ground"))
                    {
                        child.layer = LayerMask.NameToLayer("Ground");
                        child.AddComponent<MeshCollider>();
                        // var rb = child.AddComponent<Rigidbody>();
                        // rb.isKinematic = true;
                        // child.AddComponent<MeshCollider>();
                    }
                    else if (split[1].Contains("Enemy"))
                    {
                        var strings = split[1].Split(".");
                        var prefabID = strings[1];
                        var numberID = strings[2];
                        var lootObjName = strings[3];
                        
                        var enemySpotInstanceToSpawn = enemySpotsToSpawnDict[prefabID];
                        var enemySpotObj = Instantiate(enemySpotInstanceToSpawn, child.transform.position, child.transform.rotation, child.transform);
                        enemySpotObj.SetActive(true);

                        child.GetComponent<MeshRenderer>().enabled = false;
                        
                        var enemy = enemySpotObj.GetComponentInChildren<Enemy>();
                        var chapterID = gameObject.name;
                        enemy.ID = $"{prefabID}_{chapterID}_{numberID}"; // 고유
                       
                            
                        if (lootObjName.Contains("SilverKey"))
                        {
                            enemy.LootObj = silverKeyCollectablePrefab.gameObject;
                        }
                        else if (lootObjName.Contains("GoldKey"))
                        {
                            enemy.LootObj = goldKeyCollectablePrefab.gameObject;
                        }
                        else if (lootObjName.Contains("SB"))
                        {
                            enemy.LootObj = strawberryPrefab.gameObject;
                        }
                        else if (lootObjName.Contains("SJumpUp"))
                        {
                            enemy.LootObj = slideJumpUpPrefab.gameObject;
                            // child.GetComponent<MeshRenderer>().enabled = false;
                            // var collectable = Instantiate(slideJumpUpPrefab, child.transform);
                            // collectable.ID = $"Collectable_{level.ID}{collectableCount++}";
                            // collectable.gameObject.SetActive(true);
                        }
                    }
                }
                
                //////
                foreach (var spotLightSwitch in spotLightSwitchList)
                {
                    foreach (var spotLight in spotLightSwitch.SpotLights)
                    {
                        spotLight.transform.SetParent(spotLightSwitch.transform);
                    }
                }
                
                foreach (var pointLightObj in pointLightList)
                {
                    pointLightObj.transform.SetParent(level.transform);
                }
                
                foreach (var spotLight in spotLightChildList)
                {
                    var spotLightSwitch = spotLightContainerDict[spotLight.ContainerSwitchID];
                    // if (spotLightSwitch.SpotLights is null) spotLightSwitch.SpotLights = new List<SpotLight>();
                    spotLightSwitch.SpotLights.Add(spotLight);
                    spotLight.transform.SetParent(spotLightSwitch.transform);
                    spotLight.ContainerSwitch = spotLightSwitch;
                }
            }
        }

        [Button]
        private void RemoveColliders()
        {
            var colliders = cloneObj.GetComponentsInChildren<Collider>();
            for (var i = colliders.Length - 1; i > -1; i--)
            {
                DestroyImmediate(colliders[i]);
            }
        }
    }
}
#endif
