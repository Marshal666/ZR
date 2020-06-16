using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;

public static class LevelLoader
{

    public static string currentLevel;

    public static void Load(string file)
    {
        currentLevel = file;
        XmlDocument doc = new XmlDocument();
        doc.Load(file);
        LoadScene(doc);
        System.GC.Collect();
        Scene.main.clearUIAndStartLevel();
    }

    public static T ParseType<T>(string value)
    {
        T v = default(T);
        switch (typeof(T).Name)
        {
            case "Vector3":
                List<string> parts = new List<string>(value.Split('(', ',', ')', ' ', '\n'));
                parts.RemoveAll(string.IsNullOrEmpty);
                v = (T)System.Convert.ChangeType(new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])), typeof(T));
                break;
            case "RigidbodyConstraints":
                //tttfff
                if (value.Length >= 6)
                {
                    RigidbodyConstraints rc = new RigidbodyConstraints();
                    rc |= value[0] == 't' ? RigidbodyConstraints.FreezePositionX : 0;
                    rc |= value[1] == 't' ? RigidbodyConstraints.FreezePositionY : 0;
                    rc |= value[2] == 't' ? RigidbodyConstraints.FreezePositionZ : 0;
                    rc |= value[3] == 't' ? RigidbodyConstraints.FreezeRotationX : 0;
                    rc |= value[4] == 't' ? RigidbodyConstraints.FreezeRotationY : 0;
                    rc |= value[5] == 't' ? RigidbodyConstraints.FreezeRotationZ : 0;
                    v = (T)System.Convert.ChangeType(rc, typeof(T));
                }
                break;
            case "Color":
                //FFFFFFFF
                if (value.Length >= 8)
                {
                    v = (T)System.Convert.ChangeType(new Color(
                        byte.Parse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                        byte.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                        byte.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                        byte.Parse(value.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255f),
                        typeof(T));
                    //Debug.Log(v);
                }
                break;
            default:

                break;
        }
        return v;
    }

    static ObjImporter objImporter = new ObjImporter();

    static Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();

    static Dictionary<string, Material> loadedMaterials = new Dictionary<string, Material>();

    static Mesh LoadMesh(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        Mesh mesh;
        if (value.Substring(0, 10) == "(#Interal)")
        {
            switch (value.Substring(10, value.Length - 10))
            {
                case "Cube":
                    mesh = GameData.GameMeshes[0];
                    break;
                case "Sphere":
                    mesh = GameData.GameMeshes[1];
                    break;
                case "Capsule":
                    mesh = GameData.GameMeshes[2];
                    break;
                case "Quad":
                    mesh = GameData.GameMeshes[3];
                    break;
                default:
                    mesh = GameData.GameMeshes[0];  //Set cube as default mesh
                    break;
            }
        }
        else
        {
            mesh = objImporter.ImportFile(System.IO.Directory.GetCurrentDirectory() + value);
        }
        return mesh;
    }

    static Material LoadMaterial(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        if (loadedMaterials.ContainsKey(value.Trim()))
        {
            return loadedMaterials[value.Trim()];
        }
        else
        {
            if (value.Replace(" ", "") == "(#Interal)Default")
            {
                return GameData.DefaultGameMaterial;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(System.IO.Directory.GetCurrentDirectory() + value);
            string keym = value.Trim();
            loadedMaterials[keym] = new Material(GameData.DefaultShader);
            loadedMaterials[keym].SetFloat("_Glossiness", 0f);
            loadedMaterials[keym].SetFloat("_Metallic", 0.5f);
            if (!string.IsNullOrEmpty(doc.ChildNodes[0].ChildNodes[0].InnerText))
            {
                string key = doc.ChildNodes[0].ChildNodes[0].InnerText.Trim();
                if (!loadedTextures.ContainsKey(key))
                {
                    loadedTextures[key] = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                    loadedTextures[key].LoadImage(System.IO.File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + key), false);
                    loadedTextures[key].wrapMode = TextureWrapMode.Repeat;
                    loadedTextures[key].filterMode = (FilterMode)System.Enum.Parse(typeof(FilterMode), doc.ChildNodes[0].ChildNodes[0].Attributes[0].Value);
                    loadedTextures[key].anisoLevel = int.Parse(doc.ChildNodes[0].ChildNodes[0].Attributes[1].Value);
                    if (doc.ChildNodes[0].ChildNodes[0].Attributes["Transparent"] != null)
                    {
                        //loadedTextures[key].alphaIsTransparency = bool.Parse(doc.ChildNodes[0].ChildNodes[0].Attributes["Transparent"].InnerText);
                        if (bool.Parse(doc.ChildNodes[0].ChildNodes[0].Attributes["Transparent"].InnerText))
                        {
                            loadedMaterials[keym].shader = GameData.TransparentShader;
                            loadedMaterials[keym].SetFloat("_Mode", 1f);
                            loadedMaterials[keym].SetFloat("_Metallic", 1f);
                            loadedMaterials[keym].SetFloat("_Glossiness", 0f);
                            loadedMaterials[keym].SetFloat("_Cutoff", 0.65f);
                            loadedMaterials[keym].EnableKeyword("_ALPHATEST_ON");
                        }
                    }
                    loadedMaterials[keym].mainTexture = loadedTextures[key];
                }
                else
                {
                    loadedMaterials[keym].mainTexture = loadedTextures[key];
                }
            }
            loadedMaterials[keym].color = ParseType<Color>(doc.ChildNodes[0].ChildNodes[1].InnerText);
            if (!string.IsNullOrEmpty(doc.ChildNodes[0].ChildNodes[2].InnerText))
            {
                string key = doc.ChildNodes[0].ChildNodes[2].InnerText.Trim();
                if (!loadedTextures.ContainsKey(key))
                {
                    loadedTextures[key] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    loadedTextures[key].LoadImage(System.IO.File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + key), false);
                    loadedMaterials[keym].SetTexture("_BumbMap", loadedTextures[key]);
                }
                else
                {
                    loadedMaterials[keym].SetTexture("_BumbMap", loadedTextures[key]);
                }
            }
            return loadedMaterials[keym];
        }
    }

    static void LoadCollider(string name, GameObject obj, bool convex = false, bool trigger = false)
    {
        if (string.IsNullOrEmpty(name))
            return;
        switch (name)
        {
            case "BoxCollider":
                obj.AddComponent<BoxCollider>().isTrigger = trigger;
                break;
            case "SphereCollider":
                obj.AddComponent<SphereCollider>().isTrigger = trigger;
                break;
            case "CapsuleCollider":
                obj.AddComponent<CapsuleCollider>().isTrigger = trigger;
                break;
            case "MeshCollider":
                MeshCollider mc = obj.AddComponent<MeshCollider>();
                mc.convex = convex;
                mc.isTrigger = trigger;
                break;
            default:
                obj.AddComponent<BoxCollider>().isTrigger = trigger;
                break;
        }
    }

    public static void LoadObj(string path, GameObject obj)
    {
        if (string.IsNullOrEmpty(path))
            return;
        //Debug.Log(path);
        if(path.Contains("(#Interal)"))
        {
            path = path.Replace("(#Interal)", "");
            switch(path)
            {
                case "Water":
                    Vector3 scalew = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[0].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalew;
                    obj.isStatic = true;
                    //Debug.Log("Water");
                    break;
                case "ShooterPlant":
                    Vector3 scalesp = obj.transform.localScale;
                    var obja = Object.Instantiate(GameData.InteralObjects[1].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obja.transform.localScale = scalesp;
                    obja.name = obj.name;
                    Object.Destroy(obj);
                    obj = obja;
                    AI aisp = obj.GetComponent<AI>();
                    //aisp.anim["idle"].speed = 2.25f;
                    //aisp.anim.speed = aisp.anim["shoot"].length / (1f / aisp.rateOfFire + aisp.relaxTime);
                    AnimationClip ac = null;
                    for (int i = 0; i < aisp.anim.runtimeAnimatorController.animationClips.Length; i++)
                        if (aisp.anim.runtimeAnimatorController.animationClips[i].name == "shoot") {
                            ac = aisp.anim.runtimeAnimatorController.animationClips[i];
                            break;
                        }
                    if (ac != null)
                        aisp.anim.SetFloat("ShootAnimSpeed", ac.length / (1f / aisp.rateOfFire + aisp.relaxTime));
                    //aisp.firingStartDelay = aisp.anim["shoot"].length / aisp.anim["shoot"].speed * 0.6f;
                    /*if (aisp.anim["shoot"].clip.events.Length == 0)
                    {
                        AnimationEvent ae = new AnimationEvent();
                        ae.functionName = "shootAmmo";
                        ae.time = aisp.anim["shoot"].length * 0.6f;
                        ae.messageOptions = SendMessageOptions.DontRequireReceiver;
                        aisp.anim["shoot"].clip.AddEvent(ae);
                    }*/
                    break;
                case "AnkPatrol":
                    Vector3 scalespap = obj.transform.localScale;
                    var objak = Object.Instantiate(GameData.InteralObjects[2].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    objak.transform.localScale = scalespap;
                    objak.name = obj.name;
                    Object.Destroy(obj);
                    obj = objak;
                    //aisp.anim["idle"].speed = 2.25f;
                    //aisp.anim.speed = aisp.anim["shoot"].length / (1f / aisp.rateOfFire + aisp.relaxTime);
                    break;
                case "AnkAttack":
                    Vector3 scalespapa = obj.transform.localScale;
                    var objaka = Object.Instantiate(GameData.InteralObjects[3].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    objaka.transform.localScale = scalespapa;
                    objaka.name = obj.name;
                    Object.Destroy(obj);
                    obj = objaka;
                    break;
                case "LVL1Base":
                    Vector3 scalel1 = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[4].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalel1;
                    obj.isStatic = true;
                    break;
                case "IntroMesh":
                    Vector3 scalel2 = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[5].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalel2;
                    obj.isStatic = true;
                    break;
                case "LargeCliff":
                    Vector3 scalel3 = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[6].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalel3;
                    obj.isStatic = true;
                    break;
                case "SniperPlant":
                    Vector3 scalessp = obj.transform.localScale;
                    var objas = Object.Instantiate(GameData.InteralObjects[7].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    objas.transform.localScale = scalessp;
                    objas.name = obj.name;
                    Object.Destroy(obj);
                    obj = objas;
                    AI aisps = obj.GetComponent<AI>();
                    //aisp.anim["idle"].speed = 2.25f;
                    //aisp.anim.speed = aisp.anim["shoot"].length / (1f / aisp.rateOfFire + aisp.relaxTime);
                    AnimationClip acs = null;
                    for (int i = 0; i < aisps.anim.runtimeAnimatorController.animationClips.Length; i++)
                        if (aisps.anim.runtimeAnimatorController.animationClips[i].name == "shoot")
                        {
                            acs = aisps.anim.runtimeAnimatorController.animationClips[i];
                            break;
                        }
                    if (acs != null)
                        aisps.anim.SetFloat("ShootAnimSpeed", acs.length / (1f / aisps.rateOfFire + aisps.relaxTime));
                    break;
                case "LVL4Base":
                    Vector3 scalel4 = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[8].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalel4;
                    obj.isStatic = true;
                    break;
                case "LVL5Base":
                    Vector3 scalel5 = obj.transform.localScale;
                    obj = Object.Instantiate(GameData.InteralObjects[9].obj, obj.transform.position, obj.transform.rotation, obj.transform.parent);
                    obj.transform.localScale = scalel5;
                    obj.isStatic = true;
                    break;
            }
            return;
        }
        if (!File.Exists(path))
            path = System.IO.Directory.GetCurrentDirectory() + path;
        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlNode objData = doc.ChildNodes[0];
        switch (objData.Attributes["Type"].Value)
        {
            case "Obstacle":
                if (objData.Attributes["CustomTag"] != null)
                    obj.tag = objData.Attributes["CustomTag"].Value;
                else
                    obj.tag = "Obstacle";
                obj.isStatic = true;
                obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[0].ChildNodes[0].InnerText);
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = LoadMaterial(objData.ChildNodes[0].ChildNodes[1].InnerText);
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                LoadCollider(objData.ChildNodes[0].ChildNodes[2].InnerText, obj, objData.ChildNodes[0].ChildNodes[2].Attributes.Count != 0 ? bool.Parse(objData.ChildNodes[0].ChildNodes[2].Attributes[0].Value) : false);
                break;
            case "MovingObstacle":
                if (objData.Attributes["CustomTag"] != null)
                    obj.tag = objData.Attributes["CustomTag"].Value;
                else
                    obj.tag = "MovingObstacle";
                obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[1].ChildNodes[0].InnerText);
                MeshRenderer rend = obj.AddComponent<MeshRenderer>();
                rend.sharedMaterial = LoadMaterial(objData.ChildNodes[1].ChildNodes[1].InnerText);
                rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                LoadCollider(objData.ChildNodes[1].ChildNodes[2].InnerText, obj, objData.ChildNodes[1].ChildNodes[2].Attributes.Count != 0 ? bool.Parse(objData.ChildNodes[1].ChildNodes[2].Attributes[0].Value) : false);
                Rigidbody r = obj.AddComponent<Rigidbody>();
                r.mass = float.Parse(objData.ChildNodes[0].ChildNodes[0].InnerText);
                r.useGravity = bool.Parse(objData.ChildNodes[0].ChildNodes[1].InnerText);
                r.interpolation = (RigidbodyInterpolation)System.Enum.Parse(typeof(RigidbodyInterpolation), objData.ChildNodes[0].ChildNodes[2].InnerText);
                r.collisionDetectionMode = (CollisionDetectionMode)System.Enum.Parse(typeof(CollisionDetectionMode), objData.ChildNodes[0].ChildNodes[3].InnerText);
                r.constraints = ParseType<RigidbodyConstraints>(objData.ChildNodes[0].ChildNodes[4].InnerText + objData.ChildNodes[0].ChildNodes[5].InnerText);
                MovingObstacle mo = obj.AddComponent<MovingObstacle>();
                mo.type = (MovingObstacle.ObstacleType)System.Enum.Parse(typeof(MovingObstacle.ObstacleType), objData.ChildNodes[1].Attributes[0].Value);
                XmlNodeList l = objData.ChildNodes[1].ChildNodes[3].ChildNodes;
                mo.points = new MovingObstacle.Point[l.Count];
                for (int j = 0; j < l.Count; j++)
                {
                    mo.points[j] = new MovingObstacle.Point(float.Parse(l[j].ChildNodes[0].InnerText) < Time.fixedDeltaTime ? Time.fixedDeltaTime : float.Parse(l[j].ChildNodes[0].InnerText), ParseType<Vector3>(l[j].ChildNodes[1].InnerText));
                }
                break;
            case "Decoration":
                if (objData.Attributes["CustomTag"] != null)
                    obj.tag = objData.Attributes["CustomTag"].Value;
                else
                    obj.tag = "Decoration";
                obj.isStatic = true;
                obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[0].ChildNodes[0].InnerText);
                MeshRenderer rend_ = obj.AddComponent<MeshRenderer>();
                rend_.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rend_.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                rend_.material = LoadMaterial(objData.ChildNodes[0].ChildNodes[1].InnerText);
                break;
            case "StartPos":
                Scene.player.transform.position = obj.transform.position;
                break;
            case "Trigger":
                if (objData.ChildNodes[0].ChildNodes[1].InnerText.Trim() == "MeshCollider")
                {
                    obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[0].ChildNodes[0].InnerText);
                    LoadCollider(objData.ChildNodes[0].ChildNodes[1].InnerText, obj, true, true);
                }
                else
                {
                    LoadCollider(objData.ChildNodes[0].ChildNodes[1].InnerText, obj, false, true);
                }
                Rigidbody tr = obj.AddComponent<Rigidbody>();
                tr.constraints = RigidbodyConstraints.FreezeAll;
                tr.useGravity = false;
                Trigger trig = obj.AddComponent<Trigger>();
                List<string> ls = new List<string>(objData.ChildNodes[0].ChildNodes[2].InnerText.Split(' ', '(', ')', '\n'));
                ls.RemoveAll(string.IsNullOrEmpty);
                if (ls.Count > 0)
                {
                    switch (ls[0])
                    {
                        case "PlayerLose":
                            trig.OnPlayerEnter = Trigger.EventAction.PlayerLose;
                            trig.arg1 = null;
                            break;
                        case "PlayerGainHp":
                            trig.OnPlayerEnter = Trigger.EventAction.PlayerGainHp;
                            trig.arg1 = float.Parse(ls[1]);
                            break;
                        case "PlayerWin":
                            trig.OnPlayerEnter = Trigger.EventAction.PlayerWin;
                            trig.arg1 = null;
                            break;
                        default:
                            ///NOTHING
                            break;
                    }
                }
                ls = new List<string>(objData.ChildNodes[0].ChildNodes[3].InnerText.Split(' ', '(', ')', '\n'));
                ls.RemoveAll(string.IsNullOrEmpty);
                if (ls.Count > 0)
                {
                    switch (ls[0])
                    {
                        case "PlayerLose":
                            trig.OnPlayerStay = Trigger.EventAction.PlayerLose;
                            trig.arg2 = null;
                            break;
                        case "PlayerGainHp":
                            trig.OnPlayerStay = Trigger.EventAction.PlayerGainHp;
                            trig.arg2 = float.Parse(ls[1]);
                            break;
                        case "PlayerWin":
                            trig.OnPlayerStay = Trigger.EventAction.PlayerWin;
                            trig.arg2 = null;
                            break;
                        default:
                            ///NOTHING
                            break;
                    }
                }
                ls = new List<string>(objData.ChildNodes[0].ChildNodes[4].InnerText.Split(' ', '(', ')', '\n'));
                ls.RemoveAll(string.IsNullOrEmpty);
                if (ls.Count > 0)
                {
                    switch (ls[0])
                    {
                        case "PlayerLose":
                            trig.OnPlayerExit = Trigger.EventAction.PlayerLose;
                            trig.arg3 = null;
                            break;
                        case "PlayerGainHp":
                            trig.OnPlayerExit = Trigger.EventAction.PlayerGainHp;
                            trig.arg3 = float.Parse(ls[1]);
                            break;
                        case "PlayerWin":
                            trig.OnPlayerExit = Trigger.EventAction.PlayerWin;
                            trig.arg3 = null;
                            break;
                    }
                }
                break;
            case "CollectableItem":
                obj.layer = 8;
                obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[0].ChildNodes[0].InnerText);
                MeshRenderer rend__ = obj.AddComponent<MeshRenderer>();
                rend__.material = LoadMaterial(objData.ChildNodes[0].ChildNodes[1].InnerText);
                rend__.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rend__.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                LoadCollider(objData.ChildNodes[0].ChildNodes[2].InnerText, obj, objData.ChildNodes[0].ChildNodes[2].Attributes.Count != 0 ? bool.Parse(objData.ChildNodes[0].ChildNodes[2].Attributes[0].Value) : false, true);
                Rigidbody rig__ = obj.AddComponent<Rigidbody>();
                rig__.interpolation = RigidbodyInterpolation.Interpolate;
                rig__.useGravity = false;
                CollectableItem ci = obj.AddComponent<CollectableItem>();
                ci.action = (CollectableItem.CollectableItemGain)System.Enum.Parse(typeof(CollectableItem.CollectableItemGain), objData.ChildNodes[0].ChildNodes[3].InnerText);
                ci.dissapearEffect = UnityEngine.MonoBehaviour.Instantiate(GameData.ParticleEffects[int.Parse(objData.ChildNodes[0].ChildNodes[6].InnerText)], Scene.rootObject.transform);
                ci.dissapearEffect.transform.position = ci.transform.position;
                ci.dissapearEffect.SetActive(false);
                switch (ci.action)
                {
                    case CollectableItem.CollectableItemGain.MaxHpIncrease:
                        ci.arg1 = float.Parse(objData.ChildNodes[0].ChildNodes[4].InnerText);
                        ci.arg2 = null;
                        break;
                    case CollectableItem.CollectableItemGain.HealPlayer:
                        ci.arg1 = float.Parse(objData.ChildNodes[0].ChildNodes[4].InnerText);
                        ci.arg2 = null;
                        break;
                    case CollectableItem.CollectableItemGain.BoostSpeed:
                        ci.arg1 = float.Parse(objData.ChildNodes[0].ChildNodes[4].InnerText);
                        ci.arg2 = float.Parse(objData.ChildNodes[0].ChildNodes[5].InnerText);
                        break;
                    default:
                        break;
                }
                break;
            case "AI":
                obj.layer = 9;
                obj.tag = "AI";
                Rigidbody airig = obj.AddComponent<Rigidbody>();
                airig.mass = float.Parse(objData.ChildNodes[0].ChildNodes[0].InnerText);
                airig.useGravity = bool.Parse(objData.ChildNodes[0].ChildNodes[1].InnerText);
                airig.interpolation = (RigidbodyInterpolation)System.Enum.Parse(typeof(RigidbodyInterpolation), objData.ChildNodes[0].ChildNodes[2].InnerText);
                airig.collisionDetectionMode = (CollisionDetectionMode)System.Enum.Parse(typeof(CollisionDetectionMode), objData.ChildNodes[0].ChildNodes[3].InnerText);
                airig.constraints = ParseType<RigidbodyConstraints>(objData.ChildNodes[0].ChildNodes[4].InnerText + objData.ChildNodes[0].ChildNodes[5].InnerText);
                obj.AddComponent<MeshFilter>().mesh = LoadMesh(objData.ChildNodes[1].ChildNodes[0].InnerText);
                MeshRenderer rend___ = obj.AddComponent<MeshRenderer>();
                rend___.material = LoadMaterial(objData.ChildNodes[1].ChildNodes[1].InnerText);
                rend___.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rend___.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                LoadCollider(objData.ChildNodes[1].ChildNodes[2].InnerText, obj, false, false);
                BoxCollider boxcol = obj.AddComponent<BoxCollider>();
                boxcol.isTrigger = true;
                boxcol.size = ParseType<Vector3>(objData.ChildNodes[1].ChildNodes[3].InnerText);
                boxcol.center = ParseType<Vector3>(objData.ChildNodes[1].ChildNodes[4].InnerText);
                AI ai = obj.AddComponent<AI>();
                ai.hp = float.Parse(objData.ChildNodes[1].ChildNodes[5].InnerText);
                ai.hp_max = float.Parse(objData.ChildNodes[1].ChildNodes[6].InnerText);
                ai.type = (AI.AIType)System.Enum.Parse(typeof(AI.AIType), objData.ChildNodes[1].ChildNodes[7].InnerText);
                ai.speed = float.Parse(objData.ChildNodes[1].ChildNodes[8].InnerText);
                ai.rotateStop = bool.Parse(objData.ChildNodes[1].ChildNodes[9].InnerText);
                ai.rotSpeed = float.Parse(objData.ChildNodes[1].ChildNodes[10].InnerText);
                ai.patrolPoints = new Vector3[objData.ChildNodes[1].ChildNodes[11].ChildNodes.Count];
                for (int i = 0; i < ai.patrolPoints.Length; i++)
                    ai.patrolPoints[i] = ParseType<Vector3>(objData.ChildNodes[1].ChildNodes[11].ChildNodes[i].InnerText);
                ai.toleratedDistance = float.Parse(objData.ChildNodes[1].ChildNodes[12].InnerText);
                ai.activeDistance = float.Parse(objData.ChildNodes[1].ChildNodes[13].InnerText);
                ai.onGroundRaycastLenght = float.Parse(objData.ChildNodes[1].ChildNodes[14].InnerText);
                ai.checksBeforeItGoes = bool.Parse(objData.ChildNodes[1].ChildNodes[15].InnerText);
                ai.forwardRacastCheckLength = float.Parse(objData.ChildNodes[1].ChildNodes[16].InnerText);
                ai.downRaycastCheckLength = float.Parse(objData.ChildNodes[1].ChildNodes[17].InnerText);
                GameObject ammo = new GameObject();
                ammo.transform.parent = Scene.rootObject.transform;
                LoadObj(System.IO.Directory.GetCurrentDirectory() + objData.ChildNodes[1].ChildNodes[18].InnerText, ammo);
                ai.ammo = ammo;
                ai.ammoLifeTime = float.Parse(objData.ChildNodes[1].ChildNodes[19].InnerText);
                ai.rateOfFire = float.Parse(objData.ChildNodes[1].ChildNodes[20].InnerText);
                ai.relaxTime = float.Parse(objData.ChildNodes[1].ChildNodes[21].InnerText);
                ai.shootPointOffset = ParseType<Vector3>(objData.ChildNodes[1].ChildNodes[22].InnerText);
                ai.shootHeight = float.Parse(objData.ChildNodes[1].ChildNodes[23].InnerText);
                ai.damageOnCollision = float.Parse(objData.ChildNodes[1].ChildNodes[24].InnerText);
                break;
            case "Ammo":
                obj.layer = 11;
                Rigidbody ammorig = obj.AddComponent<Rigidbody>();
                ammorig.mass = float.Parse(objData.ChildNodes[0].ChildNodes[0].InnerText);
                ammorig.useGravity = bool.Parse(objData.ChildNodes[0].ChildNodes[1].InnerText);
                ammorig.interpolation = (RigidbodyInterpolation)System.Enum.Parse(typeof(RigidbodyInterpolation), objData.ChildNodes[0].ChildNodes[2].InnerText);
                ammorig.collisionDetectionMode = (CollisionDetectionMode)System.Enum.Parse(typeof(CollisionDetectionMode), objData.ChildNodes[0].ChildNodes[3].InnerText);
                ammorig.constraints = ParseType<RigidbodyConstraints>(objData.ChildNodes[0].ChildNodes[4].InnerText + objData.ChildNodes[0].ChildNodes[5].InnerText);
                obj.AddComponent<Ammo>();
                GameObject effect = (MonoBehaviour.Instantiate(GameData.ParticleEffects[int.Parse(objData.ChildNodes[1].ChildNodes[0].InnerText)]));
                effect.transform.position = obj.transform.position;
                effect.transform.parent = obj.transform;
                LoadCollider(objData.ChildNodes[1].ChildNodes[1].InnerText, obj, false, true);
                obj.transform.localScale = ParseType<Vector3>(objData.ChildNodes[1].ChildNodes[2].InnerText);
                obj.SetActive(false);
                break;
        }
    }

    static void LoadScene(XmlDocument document)
    {
        Scene.ClearScene();
        XmlNodeList objs = document.GetElementsByTagName("Object");
        for (int i = 0; i < objs.Count; i++)
        {
            if (objs[i].Attributes.Count != 0)
            {
                GameObject obj = new GameObject(objs[i].Attributes["Name"].Value);
                //Debug.Log("Adding: " + obj.name);
                obj.transform.position = ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[0].InnerText);
                obj.transform.eulerAngles = ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[1].InnerText);
                obj.transform.localScale = ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[2].InnerText);
                obj.transform.parent = Scene.rootObject.transform;
                if (!string.IsNullOrEmpty(objs[i].ChildNodes[0].ChildNodes[3].InnerText))
                {
                    GameObject p = GameObject.Find(objs[i].ChildNodes[0].ChildNodes[3].InnerText);
                    if (p != null)
                    {
                        obj.transform.parent = p.transform;
                        obj.isStatic = false;
                        //Debug.Log("Parent found: " + p.name); 
                    }
                }
                if (objs[i].ChildNodes[1].InnerText != null || objs[i].ChildNodes[1].InnerText != "")
                {
                    LoadObj(objs[i].ChildNodes[1].InnerText, obj);
                }
            }
        }
        XmlNode scene = document.ChildNodes[1].ChildNodes[1];
        Scene.minHeight = float.Parse(scene.ChildNodes[0].InnerText);
        XmlNode player = document.ChildNodes[1].ChildNodes[2];
        Scene.player.transform.position = ParseType<Vector3>(player.ChildNodes[0].ChildNodes[0].InnerText);
        Scene.player.transform.eulerAngles = ParseType<Vector3>(player.ChildNodes[0].ChildNodes[1].InnerText);
        Scene.player.transform.localScale = ParseType<Vector3>(player.ChildNodes[0].ChildNodes[2].InnerText);
        Scene.player.hp = Scene.player.hp_max;
        Scene.player.isAlive = true;
        Player.playing = true;
        XmlNode camOptions = document.ChildNodes[1].ChildNodes[3];
        PlayerCamera.main.transform.position = ParseType<Vector3>(camOptions.ChildNodes[0].InnerText);
        PlayerCamera.main.transform.eulerAngles = ParseType<Vector3>(camOptions.ChildNodes[1].InnerText);
        PlayerCamera.main.mx = PlayerCamera.main.my = 0f;
        PlayerCamera.main.mode = (PlayerCamera.PlayerCameraMode)System.Enum.Parse(typeof(PlayerCamera.PlayerCameraMode), camOptions.ChildNodes[2].InnerText);
        if (PlayerCamera.main.mode == PlayerCamera.PlayerCameraMode.PointLookAt)
        {
            PlayerCamera.main.points = new Vector3[camOptions.ChildNodes[3].ChildNodes.Count];
            for (int i = 0; i < PlayerCamera.main.points.Length; i++)
            {
                PlayerCamera.main.points[i] = ParseType<Vector3>(camOptions.ChildNodes[3].ChildNodes[i].InnerText);
            }
            PlayerCamera.main.pointOffset = ParseType<Vector3>(camOptions.ChildNodes[4].ChildNodes[0].InnerText);
        }
        XmlNode nextLevel = document.ChildNodes[1].ChildNodes[4];
        Scene.nextLevel = Directory.GetCurrentDirectory() + nextLevel.InnerText;
        if (!File.Exists(Scene.nextLevel))
        {
            Scene.nextLevel = "";
            GameData.main.nextLevelButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().color = GameData.main.editorDisabledColor;
            GameData.main.nextLevelButton.onClick.RemoveAllListeners();
        }
        else
        {
            string n = Scene.nextLevel;
            GameData.main.nextLevelButton.onClick.AddListener((delegate { Load(n); }));
            GameData.main.nextLevelButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().color = GameData.main.editorDeselectedColor;
        }
        /*XmlNode locked = document.ChildNodes [1].ChildNodes [5];
		if (bool.Parse (locked.InnerText)) {
			string l = currentLevel.Replace (Directory.GetCurrentDirectory (), "");
			if (!GameData.UnlockedLevels.Contains (l)) {
				XmlDocument gs = new XmlDocument ();
				gs.Load (Directory.GetCurrentDirectory () + "/GameSettings.xml");
				XmlNode lp = gs.CreateElement (string.Empty, "LevelPath", string.Empty);
				lp.AppendChild (gs.CreateTextNode (l));
				gs.ChildNodes [1].ChildNodes [0].AppendChild (lp);
				gs.Save (Directory.GetCurrentDirectory () + "/GameSettings.xml");
				GameData.UnlockedLevels.Add (l);
				Debug.Log ("Unlocked "+
					l);
			}
		}*/
    }

}
