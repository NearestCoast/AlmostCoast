#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.ActionStates.MeleeAttacks;
using _Project.Maps.Climber.Objects;
using _Project.Maps.Climber.Objects.Collectables;
using _Project.Maps.Climber.Objects.Variants;
using _Project.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private Level.Type levelType;
        [SerializeField] private bool useDeco;
        [SerializeField] private GameObject cloneObj;
        [SerializeField] private List<Level> levels = new List<Level>();
        // [SerializeField] private GameObject ringdongPrefab;
        [SerializeField] private Transform mapInstanceContainer;

        [SerializeField, TitleGroup("Instances")] private Ability abilityInstance;
        [SerializeField, TitleGroup("Instances")] private MovingPlatform movingPlatformInstance;
        [SerializeField, TitleGroup("Instances")] private SpotLight spotLightInstance;
        [SerializeField, TitleGroup("Instances")] private Transform objectPrefabContainer;

        private void OnValidate()
        {
            if (Application.isPlaying) return;   
            
            mapInstanceContainer ??= Extensions.FindInactiveObjectByName("MapInstances").transform;
            objectPrefabContainer ??= Extensions.FindInactiveObjectByName("ObjectPrefabs").transform;
            abilityInstance ??= Extensions.FindInactiveObjectByName("AbilityInstance").GetComponent<Ability>();
            // movingPlatformInstance = GameObject.Find("MovingPlatformInstance").GetComponent<MovingPlatform>();
            movingPlatformInstance ??= Extensions.FindInactiveObjectByName("MovingPlatformInstance").GetComponent<AccMovingPlatform>();
            spotLightInstance ??= Extensions.FindInactiveObjectByName("SpotLightInstance").GetComponent<SpotLight>();
        }

        [Button]
        private void PutColliders()
        {
            levels = new List<Level>();
            
            if (cloneObj) DestroyImmediate(cloneObj);
            cloneObj = Instantiate(gameObject, mapInstanceContainer);
            var cloneMap = cloneObj.GetComponent<Map>();
            cloneMap.levels = levels;

            gameObject.SetActive(false);
            cloneObj.SetActive(true);

            RemoveColliders();

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
                    level.ID = collectionT.name.Split(".")[1];
                }
                else
                {
                    level.ID = "000";
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
                        child.gameObject.SetActive(false);
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

                            var id = mpSplit[1];
                            var position = mpSplit[2];

                            if (position.Contains("Start"))
                            {
                                var exists = child.GetComponents<MovingPlatform>();
                                foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                                child.gameObject.tag = "IgnoreCamCollider";
                                var mp = child.AddComponent<FallingMovingPlatform>();
                                var audioSource = mp.gameObject.AddComponent<AudioSource>();
                                audioSource.clip = movingPlatformInstance.FinishAudioSource.clip;
                                audioSource.playOnAwake = false;
                                audioSource.volume = 0.3f;
                                audioSource.maxDistance = 50;
                                
                                mp.Level = level;
                                movingPlatform_Start_Dict.Add(id, mp);
                                mp.ID = id;

                                mp.RopeBrokableID = mpSplit[3].Split("_")[0];
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
                        else
                        {
                            // MP.001.End_L_level_0_2_3.blend
                            child.layer = LayerMask.NameToLayer("Ground");
                            var mpSplit = split[1].Split(".");

                            var id = mpSplit[1];
                            var position = mpSplit[2];

                            if (position.Contains("Start"))
                            {
                                var exists = child.GetComponents<MovingPlatform>();
                                foreach (var movingPlatform in exists) DestroyImmediate(movingPlatform);

                                child.gameObject.tag = "IgnoreCamCollider";
                                var mp = child.AddComponent<AccMovingPlatform>();
                                var audioSource = mp.gameObject.AddComponent<AudioSource>();
                                audioSource.clip = movingPlatformInstance.FinishAudioSource.clip;
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
                        Debug.Log(mastarID);

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
                        var boxCollider = child.AddComponent<BoxCollider>();
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
                        var boxCollider = child.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;

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
                        var id = mpSplit[1].Split("_")[0];

                        var brokable = child.AddComponent<Brokable>();
                        brokable.ID = id;
                        // Debug.Log(id);
                        brokableDict.Add(id, brokable);
                        
                        var rb = child.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        var boxCollider = child.AddComponent<BoxCollider>();
                    }
                    else if (split[1].Contains("Ability"))
                    {
                        var ability = Instantiate(abilityInstance, child.transform);
                        ability.gameObject.SetActive(true);
                        // var sphereCollider = ability.gameObject.AddComponent<SphereCollider>();
                        
                        var abilityName = split[1].Split(".")[1];
                        
                        if (abilityName.Contains("Attack"))
                        {
                            ability.TargetState = GameObject.FindObjectOfType<Attack_01>(true);
                        }
                        else if (abilityName.Contains("SlideDash"))
                        {
                            ability.TargetState = GameObject.FindObjectOfType<SlideDashState>(true);
                        }
                        else if (abilityName.Contains("GroundPounding"))
                        {
                            ability.TargetState = GameObject.FindObjectOfType<GroundPoundingState>(true);
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
                        
                                var spotLight = Instantiate(spotLightInstance);
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
                        
                                var spotLight = Instantiate(spotLightInstance);
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

                                var spotLight = Instantiate(spotLightInstance, child.transform);
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
                        
                            var spotLight = Instantiate(spotLightInstance);
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
                        var boxCollider = child.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;

                        var exists = child.GetComponentsInChildren<SavePoint>();
                        foreach (var exist in exists)
                        {
                            DestroyImmediate(exist);
                        }

                        var savePoint = child.AddComponent<SavePoint>();
                        savePoint.Level = level;
                        // level.SavePoint = savePoint;
                    }
                    else if (split[1].Contains("Ground"))
                    {
                        child.layer = LayerMask.NameToLayer("Ground");
                        child.AddComponent<MeshCollider>();
                        // var rb = child.AddComponent<Rigidbody>();
                        // rb.isKinematic = true;
                        // child.AddComponent<MeshCollider>();
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