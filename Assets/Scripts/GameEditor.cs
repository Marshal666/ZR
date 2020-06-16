using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using UnityEngine;

public class GameEditor : MonoBehaviour
{

    public static GameEditor main;

    public static bool editingSettings { get { return loadLevels || editSettings; } }

    public static bool loadLevels = false, editSettings = false;

    public GameObject hierarchyItem, objectItem, objectPropertyItem;

    public GameObject hierarchyContainer, objectsContainer, PropertyContainer;

    public GameObject EditorObjectsContainer;

    public RectTransform X;

    public UnityEngine.EventSystems.EventSystem eventSystem;

    public EditorHiererachyItem playerItem, cameraItem;

    public GameObject moveTool, rotateTool, scaleTool;

    public GameObject[] editLevelStuff;

    float startCameraDistanceFromTools;

    public enum EditTool { move, rotate, scale }
    public enum EditAxis { all, x, y, z }

    public EditTool currentTool = EditTool.move;

    public EditAxis currentAxis = EditAxis.all;

    public LayerMask editorToolsLayer;

    public LayerMask renderLayer;

    public List<GameObject> selectedObjects = new List<GameObject>();

    public Camera renderCamera;

    public GameObject editorCamera;

    public UnityEngine.UI.InputField posInput, rotInput, scaleInput, parentInput, nameInput;

    bool dragging = false;
    GameObject dragObj;
    List<Vector3> dragOffsetPos = new List<Vector3>();
    List<Vector3> scales = new List<Vector3>(), rots = new List<Vector3>();
    Vector3 dragScreenPoint;
    Vector2 startDragPoint;

    public float scaleFactor, rotationFactor;

    public string levelName = "Unnamed";

    public float minAllowedHeight = -10f;

    public UnityEngine.UI.Dropdown cameraModeInput;

    public UnityEngine.UI.InputField levlNameInput, MAHInput, CPOInput;

    public GameObject pointSphere;

    public GameObject lineRendererTemplate;

    public HashSet<string> names = new HashSet<string>();

    public Dictionary<string, EditorObjectItem> LoadedEditorObjects = new Dictionary<string, EditorObjectItem>();

    public GameObject CustomLevelsListContainer, CustomLevelListStuff, CustomLevelButton, NextLevelContainer;

    public UnityEngine.UI.Text nextLevelText;

    public static bool isChildOfRoot(GameObject obj)
    {
        Transform t = obj.transform;
        if (t == Scene.rootObject.transform)
            return true;
        while (t.parent != null)
        {
            t = t.parent;
            if (t == Scene.rootObject.transform)
                return true;
        }

        return false;
    }

    void Start()
    {
        main = this;
    }

    public static void Setup()
    {
        //GameEditor.main.startCameraDistanceFromTools = Vector3.Distance(GameEditor.main.moveTool.transform.position, Camera.main.transform.position);
        main.startCameraDistanceFromTools = 10;
        PlayerCamera.main.UpdateEditorRotation();
        var lg = GameEditor.main.objectsContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        float s = ((Screen.width * 0.5f) - lg.padding.left - lg.spacing.x * 4f) / 4f;
        lg.cellSize = new Vector2(s, s);
        main.names.Add("Player");
        main.names.Add("Root");
        main.names.Add("Camera");
        main.editorCamera.SetActive(true);
        main.UpdateHierarchy();
        if (editingSettings)
        {
            for (int i = 0; i < main.editLevelStuff.Length; i++)
            {
                main.editLevelStuff[i].SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < main.editLevelStuff.Length; i++)
            {
                main.editLevelStuff[i].SetActive(false);
            }
        }
        //Load objects from Objects folder
        string dir = Directory.GetCurrentDirectory() + "/Objects/";
        string[] files = Directory.GetFiles(dir);
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            if (Path.GetExtension(fileName) == ".xml")
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(files[i]);
                string imgpath = doc.ChildNodes[0].ChildNodes[doc.ChildNodes[0].ChildNodes.Count - 1].InnerText;
                GameObject objItem = Instantiate(GameEditor.main.objectItem);
                objItem.transform.SetParent(GameEditor.main.objectsContainer.transform);
                EditorObjectItem eoi = objItem.GetComponent<EditorObjectItem>();
                //print(files[i]);
                main.LoadedEditorObjects.Add(files[i], eoi);
                eoi.obj = new GameObject();
                eoi.obj.name = fileName.Remove(fileName.Length - 4, 4);
                LevelLoader.LoadObj(files[i], eoi.obj);
                EditorObject eo = eoi.obj.AddComponent<EditorObject>();
                eo.src = files[i].Replace(Directory.GetCurrentDirectory(), "");
                eoi.obj.SetActive(false);
                var co = eoi.obj.GetComponent<Collider>();
                if (co)
                {
                    if (co.GetType() == typeof(MeshCollider))
                        ((MeshCollider)co).convex = true;
                    co.isTrigger = true;
                }
                MeshFilter mf = eoi.obj.GetComponent<MeshFilter>();
                if (mf && mf.mesh)
                {
                    if (mf.mesh.vertices.Length == 0)
                    {
                        if (co)
                        {
                            if (co.GetType() == typeof(BoxCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[0];
                            }
                            else
                        if (co.GetType() == typeof(SphereCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[1];
                            }
                            else
                        if (co.GetType() == typeof(CapsuleCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[2];
                            }
                            else if (co.GetType() == typeof(MeshCollider))
                            {
                                mf.sharedMesh = eoi.obj.GetComponent<MeshCollider>().sharedMesh;
                            }
                        }
                        else
                        {
                            mf.mesh = null;
                        }
                    }
                }
                if (!mf)
                {
                    if (co)
                    {
                        if (!mf)
                            mf = eoi.obj.AddComponent<MeshFilter>();
                        if (co.GetType() == typeof(BoxCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[0];
                        }
                        else
                        if (co.GetType() == typeof(SphereCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[1];
                        }
                        else
                        if (co.GetType() == typeof(CapsuleCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[2];
                        }
                        else if (co.GetType() == typeof(MeshCollider))
                        {
                            mf.sharedMesh = eoi.obj.GetComponent<MeshCollider>().sharedMesh;
                        }
                    }
                }
                else
                {
                    if (!co)
                    {
                        MeshCollider mc = eoi.obj.AddComponent<MeshCollider>();
                        co = mc;
                        mc.convex = true;
                        mc.isTrigger = true;
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                MeshRenderer mr = eoi.obj.GetComponent<MeshRenderer>();
                if (!mr)
                {
                    mr = eoi.obj.AddComponent<MeshRenderer>();
                    Trigger t = eoi.obj.GetComponent<Trigger>();
                    if (t && (t.OnPlayerEnter == Trigger.EventAction.PlayerWin || t.OnPlayerStay == Trigger.EventAction.PlayerWin || t.OnPlayerExit == Trigger.EventAction.PlayerWin))
                    {
                        mr.sharedMaterial = GameData.main.WinTriggerMaterial;
                    }
                    else
                    {
                        mr.sharedMaterial = GameData.main.TriggerMaterial;
                    }
                }
                if (mr && !mr.sharedMaterial)
                {
                    if (eoi.obj)
                    {
                        mr.sharedMaterial = GameData.main.InvincibleColliderMaterial;
                    }
                    else
                    {
                        Trigger t = eoi.obj.GetComponent<Trigger>();
                        if (t && (t.OnPlayerEnter == Trigger.EventAction.PlayerWin || t.OnPlayerStay == Trigger.EventAction.PlayerWin || t.OnPlayerExit == Trigger.EventAction.PlayerWin))
                        {
                            mr.sharedMaterial = GameData.main.WinTriggerMaterial;
                        }
                        else
                        {
                            mr.sharedMaterial = GameData.main.TriggerMaterial;
                        }
                    }
                }
                eo.objMat = mr.sharedMaterial;
                Rigidbody ri = eoi.obj.GetComponent<Rigidbody>();
                if (ri)
                {
                    ri.useGravity = false;
                }
                eoi.obj.transform.parent = GameEditor.main.EditorObjectsContainer.transform;
                if (File.Exists(Directory.GetCurrentDirectory() + imgpath))
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(File.ReadAllBytes(Directory.GetCurrentDirectory() + imgpath));
                    objItem.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2());
                }
                else      //ako ne postoji ikona objekta, renderiraj jednu za taj objekt
                {
                    GameEditor.main.renderCamera.gameObject.SetActive(true);
                    eoi.obj.transform.position = Vector3.zero;
                    main.renderCamera.transform.position = eoi.obj.transform.TransformPoint(-1, 1, -1) * mr.bounds.size.magnitude;
                    eoi.obj.SetActive(true);
                    int oldlayer = eoi.obj.layer;
                    eoi.obj.layer = 13;
                    main.renderCamera.transform.LookAt(mr.bounds.center);
                    Renderer rend = eoi.obj.GetComponent<Renderer>();
                    Shader oldsh = null;
                    bool changed = false;
                    if(rend.material)
                    {
                        if(rend.material.shader == GameData.TransparentShader)
                        {
                            oldsh = rend.material.shader;
                            rend.material.shader = GameData.RenderTransparentShader;
                            rend.material.SetFloat("_Mode", 1f);
                            rend.material.SetFloat("_Metallic", 1f);
                            rend.material.SetFloat("_Glossiness", 0f);
                            rend.material.SetFloat("_Cutoff", 0.65f);
                            rend.material.EnableKeyword("_ALPHATEST_ON");
                            changed = true;
                        }
                    } else if(rend.sharedMaterial)
                    {
                        if(rend.sharedMaterial.shader == GameData.TransparentShader)
                        {
                            oldsh = rend.material.shader;
                            rend.sharedMaterial.shader = GameData.RenderTransparentShader;
                            rend.sharedMaterial.SetFloat("_Mode", 1f);
                            rend.sharedMaterial.SetFloat("_Metallic", 1f);
                            rend.sharedMaterial.SetFloat("_Glossiness", 0f);
                            rend.sharedMaterial.SetFloat("_Cutoff", 0.65f);
                            rend.sharedMaterial.EnableKeyword("_ALPHATEST_ON");
                            changed = true;
                        }
                    }
                    main.renderCamera.Render();
                    Texture2D tex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
                    RenderTexture.active = main.renderCamera.targetTexture;
                    tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    //tex.alphaIsTransparency = true;
                    tex.Apply();
                    objItem.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2());
                    var data = tex.EncodeToPNG();
                    File.WriteAllBytes(dir + fileName.Replace(".xml", ".png"), data);
                    doc.ChildNodes[0].ChildNodes[doc.ChildNodes[0].ChildNodes.Count - 1].InnerText = "/Objects/" + fileName.Replace(".xml", ".png");
                    if(changed)
                    {
                        if (rend.material)
                            rend.material.shader = oldsh;
                        if (rend.sharedMaterial)
                            rend.sharedMaterial.shader = oldsh;
                    }
                    eoi.obj.layer = oldlayer;
                    eoi.obj.SetActive(false);
                    RenderTexture.active = null;
                    main.renderCamera.targetTexture.Release();
                    GameEditor.main.renderCamera.gameObject.SetActive(false);
                    //print("rendered");
                }
                objItem.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = fileName.Remove(fileName.Length - 4, 4);
                doc.Save(files[i]);
            }
        }
        //Load interal objects
        for (int i = 0; i < GameData.InteralObjects.Length; i++)
        {
            GameObject objItem = Instantiate(GameEditor.main.objectItem);
            objItem.transform.SetParent(GameEditor.main.objectsContainer.transform);
            EditorObjectItem eoi = objItem.GetComponent<EditorObjectItem>();
            //print(files[i]);
            main.LoadedEditorObjects.Add(GameData.InteralObjects[i].src, eoi);
            eoi.obj = Instantiate(GameData.InteralObjects[i].obj);
            eoi.obj.name = GameData.InteralObjects[i].name;
            EditorObject eo = eoi.obj.AddComponent<EditorObject>();
            eo.src = GameData.InteralObjects[i].src;
            eoi.obj.SetActive(false);
            var co = eoi.obj.GetComponent<Collider>();
            if (co)
            {
                if (co.GetType() == typeof(MeshCollider))
                    ((MeshCollider)co).convex = true;
                co.isTrigger = true;
            }
            if (eoi.obj.layer != 9)
            {
                MeshFilter mf = eoi.obj.GetComponent<MeshFilter>();
                if (mf && mf.mesh)
                {
                    if (mf.mesh.vertices.Length == 0)
                    {
                        if (co)
                        {
                            if (co.GetType() == typeof(BoxCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[0];
                            }
                            else
                        if (co.GetType() == typeof(SphereCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[1];
                            }
                            else
                        if (co.GetType() == typeof(CapsuleCollider))
                            {
                                mf.sharedMesh = GameData.GameMeshes[2];
                            }
                            else if (co.GetType() == typeof(MeshCollider))
                            {
                                mf.sharedMesh = eoi.obj.GetComponent<MeshCollider>().sharedMesh;
                            }
                        }
                        else
                        {
                            mf.mesh = null;
                        }
                    }
                }
                if (!mf)
                {
                    if (co)
                    {
                        if (!mf)
                            mf = eoi.obj.AddComponent<MeshFilter>();
                        if (co.GetType() == typeof(BoxCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[0];
                        }
                        else
                        if (co.GetType() == typeof(SphereCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[1];
                        }
                        else
                        if (co.GetType() == typeof(CapsuleCollider))
                        {
                            mf.sharedMesh = GameData.GameMeshes[2];
                        }
                        else if (co.GetType() == typeof(MeshCollider))
                        {
                            mf.sharedMesh = eoi.obj.GetComponent<MeshCollider>().sharedMesh;
                        }
                    }
                }
                else
                {
                    if (!co)
                    {
                        MeshCollider mc = eoi.obj.AddComponent<MeshCollider>();
                        co = mc;
                        mc.convex = true;
                        mc.isTrigger = true;
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                MeshRenderer mr = eoi.obj.GetComponent<MeshRenderer>();
                if (!mr)
                {
                    mr = eoi.obj.AddComponent<MeshRenderer>();
                    Trigger t = eoi.obj.GetComponent<Trigger>();
                    if (t && (t.OnPlayerEnter == Trigger.EventAction.PlayerWin || t.OnPlayerStay == Trigger.EventAction.PlayerWin || t.OnPlayerExit == Trigger.EventAction.PlayerWin))
                    {
                        mr.sharedMaterial = GameData.main.WinTriggerMaterial;
                    }
                    else
                    {
                        mr.sharedMaterial = GameData.main.TriggerMaterial;
                    }
                }
                if (mr && !mr.sharedMaterial)
                {
                    if (eoi.obj)
                    {
                        mr.sharedMaterial = GameData.main.InvincibleColliderMaterial;
                    }
                    else
                    {
                        Trigger t = eoi.obj.GetComponent<Trigger>();
                        if (t && (t.OnPlayerEnter == Trigger.EventAction.PlayerWin || t.OnPlayerStay == Trigger.EventAction.PlayerWin || t.OnPlayerExit == Trigger.EventAction.PlayerWin))
                        {
                            mr.sharedMaterial = GameData.main.WinTriggerMaterial;
                        }
                        else
                        {
                            mr.sharedMaterial = GameData.main.TriggerMaterial;
                        }
                    }
                }
                eo.objMat = mr.sharedMaterial;
            } else      //objekt je AI
            {
                //eo.objMat = ((eoi.obj.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>()) != null ?? eoi.obj.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().sharedMaterial : eoi.obj.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
                SkinnedMeshRenderer skmr = eoi.obj.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
                if (skmr)
                {
                    eo.objMat = skmr.sharedMaterial;
                } else
                {
                    MeshRenderer mr = eoi.obj.transform.GetChild(0).GetComponent<MeshRenderer>();
                    eo.objMat = mr.sharedMaterial;
                }
            }
            Rigidbody ri = eoi.obj.GetComponent<Rigidbody>();
            if (ri)
            {
                ri.useGravity = false;
            }
            eoi.obj.transform.parent = GameEditor.main.EditorObjectsContainer.transform;
            objItem.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(GameData.InteralObjects[i].tex, new Rect(0, 0, 256, 256), new Vector2());
            objItem.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = GameData.InteralObjects[i].src;
        }
    }

    List<GameObject> hierarchyItems = new List<GameObject>();

    List<GameObject> camGizmoObjs = new List<GameObject>();

    LineRenderer lr;

    int countAllActiveChildren(Transform t)
    {
        int count = 0;
        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject.activeSelf)
            {
                count++;
                if (t.GetChild(i).childCount > 0)
                    count += countAllActiveChildren(t.GetChild(i));
            }
        }
        return count;
    }

    int ci = 0;

    void renameGizmoObjs()
    {
        for (int i = 0; i < camGizmoObjs.Count; i++)
        {
            camGizmoObjs[i].name = "CameraPoint" + (i + 1);
        }
    }

    string lvlToStr(int l)
    {
        if (l == 0)
        {
            return "";
        }
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < l; i++)
                sb.Append("\t");
            return sb.ToString();
        }
    }

    void buildHierarchy(Transform t, int level)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            while (i < t.childCount && (!t.GetChild(i).gameObject.activeSelf || !t.GetChild(i).GetComponent<EditorObject>())) i++;
            if (i >= t.childCount)
                break;
            if (ci >= hierarchyItems.Count)
            {
                hierarchyItems.Add(Instantiate(hierarchyItem));
                hierarchyItems[ci].transform.SetParent(hierarchyContainer.transform);
                hierarchyItems[ci].GetComponent<UnityEngine.UI.Text>().text = lvlToStr(level) + t.GetChild(i).name;
                EditorHiererachyItem ehi = hierarchyItems[ci].GetComponent<EditorHiererachyItem>();
                ehi.refObj = t.GetChild(i).gameObject;
                ehi.selected = selectedObjects.Contains(ehi.refObj);
            }
            if (hierarchyItems[ci] != null)
            {
                hierarchyItems[ci].SetActive(true);
                hierarchyItems[ci].GetComponent<UnityEngine.UI.Text>().text = lvlToStr(level) + t.GetChild(i).name;
                EditorHiererachyItem ehi = hierarchyItems[ci].GetComponent<EditorHiererachyItem>();
                ehi.refObj = t.GetChild(i).gameObject;
                ehi.selected = selectedObjects.Contains(ehi.refObj);
            }
            else
            {
                hierarchyItems.Add(Instantiate(hierarchyItem));
                hierarchyItems[ci].transform.SetParent(hierarchyContainer.transform);
                hierarchyItems[ci].GetComponent<UnityEngine.UI.Text>().text = lvlToStr(level) + t.GetChild(i).name;
                EditorHiererachyItem ehi = hierarchyItems[ci].GetComponent<EditorHiererachyItem>();
                ehi.refObj = t.GetChild(i).gameObject;
                ehi.selected = selectedObjects.Contains(ehi.refObj);
            }
            ci++;
            if (t.GetChild(i).childCount > 0)
                buildHierarchy(t.GetChild(i), level + 1);
        }
    }

    public void UpdateHierarchy()
    {
        Transform root = Scene.rootObject.transform;
        int objCount = countAllActiveChildren(root);
        for (int i = 0; i < hierarchyItems.Count; i++)
        {
            hierarchyItems[i].SetActive(false);
        }
        if (objCount > 0)
        {
            ci = 0;
            buildHierarchy(root, 1);
        }
    }

    public void selectObject(GameObject obj)
    {
        if (obj.GetComponent<Player>())
        {
            playerItem.selected = true;
            Player.main.mr.sharedMaterial = GameData.main.editorSelectedMaterial;
            selectedObjects.Add(obj);
        }
        else if (obj.tag == "EditorCam")
        {
            cameraItem.selected = true;
            obj.GetComponent<MeshRenderer>().sharedMaterial = GameData.main.editorSelectedMaterial;
            selectedObjects.Add(obj);
        }
        else
        {
            GameEditor.main.selectedObjects.Add(obj);
            if (obj.layer != 9)
            {
                var mr = obj.GetComponent<MeshRenderer>();
                if (mr)
                {
                    mr.material = GameData.main.editorSelectedMaterial;
                }
            } else
            {
                if (obj.transform.childCount > 0)
                {
                    var mr = obj.transform.GetChild(0).GetComponent<MeshRenderer>();
                    if (mr)
                    {
                        mr.material = GameData.main.editorSelectedMaterial;
                    }

                    else
                    {
                        var skmr = obj.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
                        if (skmr)
                        {
                            skmr.sharedMaterial = GameData.main.editorSelectedMaterial;
                        }
                    }
                } else
                {
                    var mr = obj.GetComponent<MeshRenderer>();
                    if (mr)
                    {
                        mr.material = GameData.main.editorSelectedMaterial;
                    }
                }
            }
            UpdateHierarchy();
        }
        dragging = false;
        scales.Clear();
        rots.Clear();
        dragOffsetPos.Clear();
    }

    public void deselectObject(GameObject obj)
    {
        if (obj.GetComponent<Player>())
        {
            playerItem.selected = false;
            Player.main.mr.material = obj.GetComponent<EditorObject>().objMat;
            selectedObjects.Remove(obj);
        }
        else if (obj.tag == "EditorCam")
        {
            cameraItem.selected = false;
            obj.GetComponent<MeshRenderer>().sharedMaterial = obj.GetComponent<EditorObject>().objMat;
            selectedObjects.Remove(obj);
        }
        else
        {
            selectedObjects.Remove(obj);
            if (obj.layer != 9)
            {
                var mr = obj.GetComponent<MeshRenderer>();
                if (mr)
                    mr.sharedMaterial = obj.GetComponent<EditorObject>().objMat;
            } else
            {
                if (obj.transform.childCount > 0)
                {
                    var mr = obj.transform.GetChild(0).GetComponent<MeshRenderer>();
                    if (mr)
                        mr.sharedMaterial = obj.GetComponent<EditorObject>().objMat;
                    else
                    {
                        var skmr = obj.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
                        if (skmr)
                            skmr.sharedMaterial = obj.GetComponent<EditorObject>().objMat;
                    }
                } else
                {
                    var mr = obj.GetComponent<MeshRenderer>();
                    if (mr)
                        mr.sharedMaterial = obj.GetComponent<EditorObject>().objMat;
                }
            }
            UpdateHierarchy();
        }
    }

    public void deselectAll()
    {
        if (selectedObjects.Count > 0)
        {
            playerItem.selected = false;
            cameraItem.selected = false;
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                if (selectedObjects[i])
                {
                    var mr = selectedObjects[i].GetComponent<MeshRenderer>();
                    if (mr)
                        mr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                    else if (selectedObjects[i].GetComponent<Player>())
                        Player.main.mr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                    if(selectedObjects[i].layer == 9)
                    {
                        if (selectedObjects[i].transform.childCount > 0)
                        {
                            var amr = selectedObjects[i].transform.GetChild(0).GetComponent<MeshRenderer>();
                            if (amr)
                                amr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                            else
                            {
                                var skmr = selectedObjects[i].transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
                                if (skmr)
                                    skmr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                            }
                        } else
                        {
                            mr = selectedObjects[i].GetComponent<MeshRenderer>();
                            if (mr)
                                mr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                        }
                    }
                }
            }
            selectedObjects.Clear();
        }
        UpdateHierarchy();
    }

    public void applyPos(string v)
    {
        if (selectedObjects.Count > 0)
        {
            try
            {
                Vector3 pos = LevelLoader.ParseType<Vector3>(v);
                selectedObjects[0].transform.position = pos;
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                posInput.text = selectedObjects[0].transform.position.ToString();
            }
        }
    }

    public void applyRot(string v)
    {
        if (selectedObjects.Count > 0 && !selectedObjects[0].GetComponent<Player>() && selectedObjects[0].tag != "EditorCamPoint")
        {
            try
            {
                Vector3 rot = LevelLoader.ParseType<Vector3>(v);
                selectedObjects[0].transform.eulerAngles = rot;
                rots.Clear();
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                rotInput.text = selectedObjects[0].transform.eulerAngles.ToString();
            }
        }
    }

    public void applyScale(string v)
    {
        if (selectedObjects.Count > 0 && !selectedObjects[0].GetComponent<Player>() && selectedObjects[0].tag != "EditorCam" && selectedObjects[0].tag != "EditorCamPoint")
        {
            try
            {
                Vector3 s = LevelLoader.ParseType<Vector3>(v);
                selectedObjects[0].transform.localScale = s;
                scales.Clear();
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                scaleInput.text = selectedObjects[0].transform.localScale.ToString();
            }
        }
    }

    public void applyParent(string v)
    {
        if (!string.IsNullOrEmpty(v))
        {
            if (v == "Root")
                selectedObjects[0].transform.parent = Scene.rootObject.transform;
            if (selectedObjects.Count > 0 && !selectedObjects[0].GetComponent<Player>() && selectedObjects[0].tag != "EditorCam" && selectedObjects[0].tag != "EditorCamPoint")
            {
                GameObject p = GameObject.Find(v);
                if (p && p.GetComponent<EditorObject>() && !p.GetComponent<Player>() && selectedObjects[0].tag != "EditorCam" && selectedObjects[0].tag != "EditorCamPoint" && isChildOfRoot(p))
                {
                    selectedObjects[0].transform.parent = p.transform;
                    UpdateHierarchy();
                }
                else
                {
                    parentInput.text = selectedObjects[0].transform.parent.name;
                }
            }
            else
            {
                parentInput.text = "";
            }
        }
    }

    public void applyName(string v)
    {
        if (selectedObjects.Count > 0 && !string.IsNullOrEmpty(v))
        {
            if (!selectedObjects[0].GetComponent<Player>() && selectedObjects[0].tag != "EditorCam" && selectedObjects[0].tag != "EditorCamPoint")
                if (!names.Contains(v))
                {
                    if (names.Contains(selectedObjects[0].name))
                        names.Remove(selectedObjects[0].name);
                    names.Add(v);
                    selectedObjects[0].name = v;
                    UpdateHierarchy();
                }
                else
                {
                    nameInput.text = selectedObjects[0].name;
                }
        }
    }

    string addNumberIn(string s)
    {
        bool hasNumberIn = false;
        int startIndex = s.Length;
        while (startIndex - 1 >= 0 && char.IsNumber(s[startIndex - 1]))
            startIndex--;
        hasNumberIn = (startIndex != s.Length);
        if (hasNumberIn)
        {
            int n = int.Parse(s.Substring(startIndex));
            s = s.Substring(0, startIndex) + (n + 1).ToString();
            return s;
        }
        else
        {
            return s + 1;
        }
    }

    public string findAnotherName(string n)
    {
        string s = n;
        while (names.Contains(s))
        {
            s = addNumberIn(s);
        }
        return s;
    }

    public void applyLevelName(string v)
    {
        if (!string.IsNullOrEmpty(v))
        {
            levelName = v;
        }
        else
        {
            levlNameInput.text = levelName;
        }

    }

    public void applyMinHeight(string v)
    {
        float f;
        if (float.TryParse(v, out f))
        {
            minAllowedHeight = f;
            MAHInput.text = f.ToString("F7");
        }
        else
        {
            MAHInput.text = minAllowedHeight.ToString("F7");
        }
    }

    void Update()
    {
        if (Scene.currentGameState == Scene.GameState.editing)
        {
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                dragging = false;
                dragOffsetPos.Clear();
                for (int i = 0; i < scales.Count && i < selectedObjects.Count; i++)
                {
                    scales[i] = selectedObjects[i].transform.localScale;
                }
                for (int i = 0; i < rots.Count && i < selectedObjects.Count; i++)
                    rots[i] = selectedObjects[i].transform.eulerAngles;
            }
            if (Input.GetKeyDown(KeyCode.W))
                currentTool = EditTool.move;
            if (Input.GetKeyDown(KeyCode.E))
                currentTool = EditTool.rotate;
            if (Input.GetKeyDown(KeyCode.R))
                currentTool = EditTool.scale;
            bool mouseIsInGameWindow = (Input.mousePosition.x / Screen.width >= 0.25f && Input.mousePosition.x / Screen.width <= 0.75f) && (Input.mousePosition.y / Screen.height >= 0.33f) && !editingSettings;
            if (cameraModeInput.value == (int)PlayerCamera.PlayerCameraMode.PointLookAt)
            {
                if (!lr)
                {
                    lr = Instantiate(lineRendererTemplate.gameObject).GetComponent<LineRenderer>();
                }
                lr.gameObject.SetActive(true);
                if (camGizmoObjs.Count == 0)
                {
                    var o = Instantiate(pointSphere, editorCamera.transform.position + Vector3.forward, Quaternion.identity, EditorObjectsContainer.transform);
                    o.name = "CameraPoint1";
                    camGizmoObjs.Add(o);
                }
                lr.positionCount = camGizmoObjs.Count;
                for (int i = 0; i < camGizmoObjs.Count; i++)
                {
                    lr.SetPosition(i, camGizmoObjs[i].transform.position);
                }
            }
            else
            {
                for (int i = 0; i < camGizmoObjs.Count; i++)
                {
                    camGizmoObjs[i].SetActive(false);
                }
                if (lr)
                    lr.gameObject.SetActive(false);
            }
            if (selectedObjects.Count == 1 && mouseIsInGameWindow)
            {
                PropertyContainer.SetActive(true);
                posInput.text = selectedObjects[0].transform.position.ToString();
                rotInput.text = selectedObjects[0].transform.eulerAngles.ToString();
                scaleInput.text = selectedObjects[0].transform.localScale.ToString();
                nameInput.text = selectedObjects[0].name;
                if (selectedObjects[0].transform.parent)
                    parentInput.text = selectedObjects[0].transform.parent.name;
                else
                    parentInput.text = "";
            }
            else if (selectedObjects.Count != 1)
            {
                PropertyContainer.SetActive(false);
            }
            if (selectedObjects.Count > 0)
            {
                Vector3 pos = Vector3.zero;
                for (int i = 0; i < selectedObjects.Count; i++)
                    pos += selectedObjects[i].transform.position;
                pos /= selectedObjects.Count;
                switch (currentTool)
                {
                    case EditTool.move:
                        moveTool.SetActive(true);
                        rotateTool.SetActive(false);
                        scaleTool.SetActive(false);
                        moveTool.transform.position = pos;
                        moveTool.transform.localScale = Vector3.one * (Vector3.Distance(moveTool.transform.position, Camera.main.transform.position) / startCameraDistanceFromTools);
                        break;
                    case EditTool.rotate:
                        moveTool.SetActive(false);
                        rotateTool.SetActive(true);
                        scaleTool.SetActive(false);
                        rotateTool.transform.position = pos;
                        rotateTool.transform.localScale = Vector3.one * (Vector3.Distance(moveTool.transform.position, Camera.main.transform.position) / startCameraDistanceFromTools);
                        break;
                    case EditTool.scale:
                        moveTool.SetActive(false);
                        rotateTool.SetActive(false);
                        scaleTool.SetActive(true);
                        scaleTool.transform.position = pos;
                        scaleTool.transform.localScale = Vector3.one * (Vector3.Distance(moveTool.transform.position, Camera.main.transform.position) / startCameraDistanceFromTools);
                        break;
                    default:
                        break;
                }
                if (Input.GetKey(KeyCode.Mouse0) && mouseIsInGameWindow)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 10000f, editorToolsLayer, QueryTriggerInteraction.Collide))
                    {
                        if (!dragging)
                        {
                            dragging = true;
                            if (hit.collider.gameObject.name == "MoveAll" || hit.collider.gameObject.name == "ScaleAll" || hit.collider.gameObject.name == "EditorRotateObject")
                                currentAxis = EditAxis.all;
                            if (hit.collider.gameObject.name == "XAxis" || hit.collider.name == "RotateX")
                                currentAxis = EditAxis.x;
                            if (hit.collider.gameObject.name == "YAxis" || hit.collider.name == "RotateY")
                                currentAxis = EditAxis.y;
                            if (hit.collider.gameObject.name == "ZAxis" || hit.collider.name == "RotateZ")
                                currentAxis = EditAxis.z;
                            dragScreenPoint = Camera.main.WorldToScreenPoint(pos);
                            startDragPoint = Input.mousePosition;
                            for (int i = 0; i < selectedObjects.Count; i++)
                            {
                                dragOffsetPos.Add(selectedObjects[i].transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z)));
                                scales.Add(selectedObjects[i].transform.localScale);
                            }
                        }
                    }
                    switch (currentTool)
                    {
                        case EditTool.move:
                            if (dragging)
                            {
                                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z);
                                switch (currentAxis)
                                {
                                    case EditAxis.all:
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (selectedObjects.Count > dragOffsetPos.Count)
                                            {
                                                for (int j = dragOffsetPos.Count; j < selectedObjects.Count; j++)
                                                    dragOffsetPos.Add(selectedObjects[j].transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z)));
                                            }
                                            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + dragOffsetPos[i];
                                            selectedObjects[i].transform.position = curPosition;
                                        }
                                        break;
                                    case EditAxis.x:
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (selectedObjects.Count > dragOffsetPos.Count)
                                            {
                                                for (int j = dragOffsetPos.Count; j < selectedObjects.Count; j++)
                                                    dragOffsetPos.Add(selectedObjects[j].transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z)));
                                            }
                                            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + dragOffsetPos[i];
                                            selectedObjects[i].transform.position = new Vector3(curPosition.x, selectedObjects[i].transform.position.y, selectedObjects[i].transform.position.z);
                                        }
                                        break;
                                    case EditAxis.y:
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (selectedObjects.Count > dragOffsetPos.Count)
                                            {
                                                for (int j = dragOffsetPos.Count; j < selectedObjects.Count; j++)
                                                    dragOffsetPos.Add(selectedObjects[j].transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z)));
                                            }
                                            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + dragOffsetPos[i];
                                            selectedObjects[i].transform.position = new Vector3(selectedObjects[i].transform.position.x, curPosition.y, selectedObjects[i].transform.position.z);
                                        }
                                        break;
                                    case EditAxis.z:
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (selectedObjects.Count > dragOffsetPos.Count)
                                            {
                                                for (int j = dragOffsetPos.Count; j < selectedObjects.Count; j++)
                                                    dragOffsetPos.Add(selectedObjects[j].transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragScreenPoint.z)));
                                            }
                                            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + dragOffsetPos[i];
                                            selectedObjects[i].transform.position = new Vector3(selectedObjects[i].transform.position.x, selectedObjects[i].transform.position.y, curPosition.z);
                                        }
                                        break;
                                }
                            }
                            break;
                        case EditTool.rotate:
                            if (dragging)
                            {
                                switch (currentAxis)
                                {
                                    case EditAxis.x:
                                        float d = (Input.mousePosition.y - startDragPoint.y) / (Screen.height * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (rots.Count < selectedObjects.Count)
                                            {
                                                for (int j = rots.Count; j < selectedObjects.Count; j++)
                                                    rots.Add(selectedObjects[j].transform.eulerAngles);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.eulerAngles = new Vector3(rots[i].x + d * rotationFactor, rots[i].y, rots[i].z);
                                            }
                                        }
                                        break;
                                    case EditAxis.y:
                                        float e = -(Input.mousePosition.x - startDragPoint.x) / (Screen.width * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (rots.Count < selectedObjects.Count)
                                            {
                                                for (int j = rots.Count; j < selectedObjects.Count; j++)
                                                    rots.Add(selectedObjects[j].transform.eulerAngles);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.eulerAngles = new Vector3(rots[i].x, rots[i].y + e * rotationFactor, rots[i].z);
                                            }
                                        }
                                        break;
                                    case EditAxis.z:
                                        float f = (Input.mousePosition.x - startDragPoint.x) / (Screen.width * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (rots.Count < selectedObjects.Count)
                                            {
                                                for (int j = rots.Count; j < selectedObjects.Count; j++)
                                                    rots.Add(selectedObjects[j].transform.eulerAngles);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.eulerAngles = new Vector3(rots[i].x, rots[i].y, rots[i].z + f * rotationFactor);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        case EditTool.scale:
                            if (dragging)
                            {
                                switch (currentAxis)
                                {
                                    case EditAxis.all:
                                        float d = (Input.mousePosition.x - startDragPoint.x) / (Screen.width * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (scales.Count < selectedObjects.Count)
                                            {
                                                for (int j = scales.Count; j < selectedObjects.Count; j++)
                                                    scales.Add(selectedObjects[j].transform.localScale);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                                                selectedObjects[i].transform.localScale = scales[i] * Mathf.Pow(scaleFactor, d);
                                        }
                                        break;
                                    case EditAxis.x:
                                        float e = (Input.mousePosition.x - startDragPoint.x) / (Screen.width * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (scales.Count < selectedObjects.Count)
                                            {
                                                for (int j = scales.Count; j < selectedObjects.Count; j++)
                                                    scales.Add(selectedObjects[j].transform.localScale);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.localScale = new Vector3(scales[i].x * Mathf.Pow(scaleFactor, e), scales[i].y, scales[i].z);
                                            }
                                        }
                                        break;
                                    case EditAxis.y:
                                        float f = (Input.mousePosition.y - startDragPoint.y) / (Screen.height * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (scales.Count < selectedObjects.Count)
                                            {
                                                for (int j = scales.Count; j < selectedObjects.Count; j++)
                                                    scales.Add(selectedObjects[j].transform.localScale);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.localScale = new Vector3(scales[i].x, scales[i].y * Mathf.Pow(scaleFactor, f), scales[i].z);
                                            }
                                        }
                                        break;
                                    case EditAxis.z:
                                        float g = (Input.mousePosition.x - startDragPoint.x) / (Screen.width * 0.5f);
                                        for (int i = 0; i < selectedObjects.Count; i++)
                                        {
                                            if (scales.Count < selectedObjects.Count)
                                            {
                                                for (int j = scales.Count; j < selectedObjects.Count; j++)
                                                    scales.Add(selectedObjects[j].transform.localScale);
                                            }
                                            if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                                            {
                                                selectedObjects[i].transform.localScale = new Vector3(scales[i].x, scales[i].y, scales[i].z * Mathf.Pow(scaleFactor, g));
                                            }
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))   ///Duplicate Objects
                {
                    for (int i = 0; i < selectedObjects.Count; i++)
                    {
                        if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                        {
                            var o = Instantiate(selectedObjects[i]);
                            o.transform.parent = selectedObjects[i].transform.parent;
                            o.name = selectedObjects[i].name;
                            if (names.Contains(o.name))
                            {
                                o.name = findAnotherName(o.name);
                                names.Add(o.name);
                            }
                            else
                            {
                                names.Add(o.name);
                            }
                            if (o.layer != 9)
                            {
                                MeshRenderer mr = selectedObjects[i].GetComponent<MeshRenderer>();
                                if (mr) mr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                            } else
                            {
                                MeshRenderer mr = selectedObjects[i].transform.GetChild(0).GetComponent<MeshRenderer>();
                                if (mr) mr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                                else
                                {
                                    SkinnedMeshRenderer skmr = selectedObjects[i].transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
                                    if(skmr)
                                        skmr.sharedMaterial = selectedObjects[i].GetComponent<EditorObject>().objMat;
                                }
                            }
                            
                            selectedObjects[i] = o;
                        }
                        else if (selectedObjects[i].tag == "EditorCamPoint")
                        {
                            var o = Instantiate(selectedObjects[i]);
                            o.name = "CameraPoint" + (camGizmoObjs.Count + 1);
                            o.transform.parent = EditorObjectsContainer.transform;
                            deselectObject(selectedObjects[i]);
                            selectObject(o);
                            camGizmoObjs.Add(o);
                        }
                    }
                    UpdateHierarchy();
                }
                if (Input.GetKey(KeyCode.Delete))   //Delete Object
                {
                    for (int i = 0; i < selectedObjects.Count; i++)
                    {
                        if (!selectedObjects[i].GetComponent<Player>() && selectedObjects[i].tag != "EditorCam" && selectedObjects[i].tag != "EditorCamPoint")
                        {
                            selectedObjects[i].SetActive(false);
                            names.Remove(selectedObjects[i].name);
                            Destroy(selectedObjects[i]);
                        }
                        else if (selectedObjects[i].tag == "EditorCamPoint" && camGizmoObjs.Count > 1)
                        {
                            selectedObjects[i].SetActive(false);
                            Destroy(selectedObjects[i]);
                            camGizmoObjs.Remove(selectedObjects[i]);
                            renameGizmoObjs();
                        }
                    }
                    dragOffsetPos.Clear();
                    scales.Clear();
                    rots.Clear();
                    dragging = false;
                    deselectAll();
                }
            }
            else
            {
                moveTool.SetActive(false);
                rotateTool.SetActive(false);
                scaleTool.SetActive(false);
                dragging = false;
            }
            if (Input.GetKeyDown(KeyCode.Mouse0) && mouseIsInGameWindow && !dragging)
            {
                dragging = false;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 10000f))
                {
                    if (hit.collider.GetComponent<EditorObject>())
                    {
                        GameObject obj = hit.collider.gameObject;
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            if (!selectedObjects.Contains(obj))
                            {
                                selectObject(obj);
                            }
                            else
                            {
                                deselectObject(obj);
                            }
                        }
                        else
                        {
                            if (selectedObjects.Count > 0)
                            {
                                deselectAll();
                                selectObject(obj);
                            }
                            else
                            {
                                selectObject(obj);
                            }
                        }
                    }
                }
                else
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                        deselectAll();
                }
            }
        }
    }

    static void eraseChildern(GameObject o)
    {
        for (int i = 0; i < o.transform.childCount; i++)
            Destroy(o.transform.GetChild(i).gameObject);
    }

    static void eraseChildern(GameObject o, string n)
    {
        for (int i = 0; i < o.transform.childCount; i++)
        {
            o.transform.GetChild(i).name = n;
            o.transform.GetChild(i).gameObject.SetActive(false);
            Destroy(o.transform.GetChild(i).gameObject);
        }
    }

    public static void Clear()
    {
        main.deselectAll();
        main.NextLevelContainer.SetActive(false);
        main.CPOInput.text = Vector3.zero.ToString();
        for (int i = 0; i < main.camGizmoObjs.Count; i++)
            Destroy(main.camGizmoObjs[i]);
        main.camGizmoObjs.Clear();
        editSettings = false;
        for (int i = 0; i < main.editLevelStuff.Length; i++)
        {
            main.editLevelStuff[i].SetActive(false);
        }
        loadLevels = false;
        main.CustomLevelListStuff.SetActive(false);
        eraseChildern(main.CustomLevelsListContainer);
        eraseChildern(main.EditorObjectsContainer);
        for (int i = 0; i < main.objectsContainer.transform.childCount; i++)
        {
            EditorObjectItem eoi = main.objectsContainer.transform.GetChild(i).GetComponent<EditorObjectItem>();
            if (eoi.copy)
                Destroy(eoi.copy);
            eoi.gameObject.name = "del";
            Destroy(eoi.gameObject);
        }
        main.editorCamera.SetActive(false);
        if (main.lr)
            main.lr.gameObject.SetActive(false);
        main.LoadedEditorObjects.Clear();
        main.names.Clear();
        main.moveTool.SetActive(false);
        main.rotateTool.SetActive(false);
        main.scaleTool.SetActive(false);
        main.scales.Clear();
        main.rots.Clear();
        main.dragOffsetPos.Clear();
    }

    public void OpenLevelSettings()
    {
        editSettings = !editSettings;

        NextLevelContainer.SetActive(false);

        if (loadLevels)
        {
            CustomLevelListStuff.SetActive(false);
            eraseChildern(CustomLevelsListContainer);
            loadLevels = false;
        }

        if (editSettings)
        {
            for (int i = 0; i < editLevelStuff.Length; i++)
            {
                editLevelStuff[i].SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < editLevelStuff.Length; i++)
            {
                editLevelStuff[i].SetActive(false);
            }
        }
    }

    void SaveObjs(XmlElement container, XmlDocument doc, GameObject root)
    {
        Transform rt = root.transform;
        for (int i = 0; i < rt.childCount; i++)
        {
            while (i < rt.childCount && !rt.GetChild(i).gameObject.activeSelf) i++;
            if (i < rt.childCount && rt.GetChild(i).GetComponent<EditorObject>())
            {
                XmlElement el = doc.CreateElement(string.Empty, "Object", string.Empty);
                el.SetAttribute("Name", rt.GetChild(i).name);
                XmlElement t = doc.CreateElement(string.Empty, "Transform", string.Empty);
                XmlElement pos = doc.CreateElement(string.Empty, "Position", string.Empty);
                XmlElement rot = doc.CreateElement(string.Empty, "Rotation", string.Empty);
                XmlElement scale = doc.CreateElement(string.Empty, "Scale", string.Empty);
                XmlElement parent = doc.CreateElement(string.Empty, "Parent", string.Empty);
                XmlText v = doc.CreateTextNode(rt.GetChild(i).transform.position.ToString("F7"));
                pos.AppendChild(v);
                v = doc.CreateTextNode(rt.GetChild(i).transform.eulerAngles.ToString("F7"));
                rot.AppendChild(v);
                v = doc.CreateTextNode(rt.GetChild(i).transform.localScale.ToString("F7"));
                scale.AppendChild(v);
                if (rt.GetChild(i).parent)
                    v = doc.CreateTextNode(rt.GetChild(i).parent.name);
                else
                    v = doc.CreateTextNode("");
                parent.AppendChild(v);
                t.AppendChild(pos);
                t.AppendChild(rot);
                t.AppendChild(scale);
                t.AppendChild(parent);
                el.AppendChild(t);
                XmlElement src = doc.CreateElement(string.Empty, "ObjRef", string.Empty);
                v = doc.CreateTextNode(rt.GetChild(i).GetComponent<EditorObject>().src);
                src.AppendChild(v);
                el.AppendChild(t);
                el.AppendChild(src);
                container.AppendChild(el);
                if (rt.GetChild(i).childCount > 0)
                    SaveObjs(container, doc, rt.GetChild(i).gameObject);
            }
        }
    }

    public void acitivateNextLevelList()
    {
        NextLevelContainer.SetActive(true);
        for (int i = 0; i < editLevelStuff.Length; i++)
            editLevelStuff[i].SetActive(false);
        Transform nlg = NextLevelContainer.transform.GetChild(1);
        string[] lvls = Directory.GetDirectories("CustomLevels");
        eraseChildern(nlg.gameObject);
        GameObject noneButton = Instantiate(CustomLevelButton, nlg);
        Transform _tn = noneButton.transform.GetChild(2);
        if (_tn)
            _tn.gameObject.SetActive(false);
        UnityEngine.UI.Text t = noneButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>();
        t.text = "None";
        t.color = Color.red;
        UnityEngine.UI.Button b = noneButton.GetComponent<UnityEngine.UI.Button>();
        string s = "None";
        b.onClick.AddListener(delegate { GameEditor.main.applyNextLevel(s, "None"); });
        for (int i = 0; i < lvls.Length; i++)
        {
            GameObject lb = Instantiate(CustomLevelButton, nlg);
            _tn = lb.transform.GetChild(2);
            if (_tn)
                _tn.gameObject.SetActive(false);
            UnityEngine.UI.Text lt = lb.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>();
            lt.text = ((lvls[i].Replace("CustomLevels", "")).Replace("\\", "")).Replace("/", "");
            UnityEngine.UI.Button lbc = lb.GetComponent<UnityEngine.UI.Button>();
            string l = "/" + lvls[i].Replace("\\", "/") + "/Level.xml";
            lbc.onClick.AddListener(delegate { GameEditor.main.applyNextLevel(l, lt.text); });
        }
    }

    public void applyNextLevel(string path, string n)
    {
        nextLevelText.text = n;
        nextLevelText.name = path;
        NextLevelContainer.SetActive(false);
        for (int i = 0; i < editLevelStuff.Length; i++)
            editLevelStuff[i].SetActive(true);
    }

    public void deleteLevel(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        if (loadLevels)
        {
            loadLevels = false;
            activateCustomLevelsList();
        }
    }

    public void activateCustomLevelsList()
    {
        loadLevels = !loadLevels;
        NextLevelContainer.SetActive(false);
        if (loadLevels)
        {
            if (editSettings)
            {
                for (int i = 0; i < editLevelStuff.Length; i++)
                {
                    editLevelStuff[i].SetActive(false);
                }
                editSettings = false;
            }
            CustomLevelListStuff.SetActive(true);
            if (CustomLevelsListContainer.transform.childCount > 0)
                eraseChildern(CustomLevelsListContainer);
            List<string> levels = new List<string>(System.IO.Directory.GetDirectories("CustomLevels"));
            for (int i = 0; i < levels.Count; i++)
            {
                GameObject obj = Instantiate(CustomLevelButton);
                obj.name = levels[i].Remove(0, 13);
                obj.transform.SetParent(CustomLevelsListContainer.transform);
                obj.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = obj.name;
                string arg = System.IO.Directory.GetCurrentDirectory() + "/" + levels[i] + "/Level.xml";
                obj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { GameEditor.main.LoadLevel(arg); });
                Transform delb = obj.transform.GetChild(2);
                if (delb)    //delete level button init
                {
                    string arg2 = System.IO.Directory.GetCurrentDirectory() + "/" + levels[i];
                    delb.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { GameEditor.main.deleteLevel(arg2); });
                }
            }
        }
        else
        {
            CustomLevelListStuff.SetActive(false);
            eraseChildern(CustomLevelsListContainer);
        }
    }

    public void applyCameraPointOffset(string v)
    {
        try
        {
            LevelLoader.ParseType<Vector3>(v);
        }
#pragma warning disable CS0168 // Variable is declared but never used
        catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
        {
            CPOInput.text = "(0,0,0)";
        }
    }

    public void LoadLevel(string path)
    {
        eraseChildern(Scene.rootObject, "deleted");
        Clear();
        Setup();
        UpdateHierarchy();
        levelName = Path.GetFileName(Path.GetDirectoryName(path));
        levlNameInput.text = levelName;
        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlNodeList objs = doc.ChildNodes[1].ChildNodes[0].ChildNodes;
        if (objs != null)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                if (objs[i].Attributes.Count != 0)
                {
                    //print(Directory.GetCurrentDirectory() + objs[i].ChildNodes[1].InnerText);
                    GameObject obj;
                    //print(objs[i].ChildNodes[1].InnerText);
                    if (objs[i].ChildNodes[1].InnerText.Contains("(#Interal)"))
                        obj = Instantiate(LoadedEditorObjects[objs[i].ChildNodes[1].InnerText].obj);
                    else
                        obj = Instantiate(LoadedEditorObjects[Directory.GetCurrentDirectory() + objs[i].ChildNodes[1].InnerText].obj);
                    obj.SetActive(true);
                    //print(obj.activeSelf);
                    obj.name = objs[i].Attributes["Name"].Value;
                    if (!names.Contains(obj.name))
                        names.Add(obj.name);
                    else
                    {
                        obj.name = findAnotherName(obj.name);
                        names.Add(obj.name);
                    }
                    obj.transform.position = LevelLoader.ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[0].InnerText);
                    obj.transform.eulerAngles = LevelLoader.ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[1].InnerText);
                    obj.transform.localScale = LevelLoader.ParseType<Vector3>(objs[i].ChildNodes[0].ChildNodes[2].InnerText);
                    obj.transform.SetParent(Scene.rootObject.transform);
                    if (!string.IsNullOrEmpty(objs[i].ChildNodes[0].ChildNodes[3].InnerText))
                    {
                        GameObject p = GameObject.Find(objs[i].ChildNodes[0].ChildNodes[3].InnerText);
                        if (p != null)
                        {
                            obj.transform.SetParent(p.transform);
                            //print(obj.name + "->" + obj.transform.parent.name);  
                            obj.isStatic = false;
                        }
                    }

                }
            }
        }
        XmlNode sceneInfo = doc.ChildNodes[1].ChildNodes[1];
        MAHInput.text = sceneInfo.ChildNodes[0].InnerText;
        XmlNode playerInfo = doc.ChildNodes[1].ChildNodes[2];
        XmlNode playerTransform = playerInfo.ChildNodes[0];
        Scene.player.transform.position = LevelLoader.ParseType<Vector3>(playerTransform.ChildNodes[0].InnerText);
        Scene.player.transform.eulerAngles = LevelLoader.ParseType<Vector3>(playerTransform.ChildNodes[1].InnerText);
        Scene.player.transform.localScale = LevelLoader.ParseType<Vector3>(playerTransform.ChildNodes[2].InnerText);
        XmlNode cam = doc.ChildNodes[1].ChildNodes[3];
        editorCamera.transform.position = LevelLoader.ParseType<Vector3>(cam.ChildNodes[0].InnerText);
        editorCamera.transform.eulerAngles = LevelLoader.ParseType<Vector3>(cam.ChildNodes[1].InnerText);
        cameraModeInput.value = (int)(PlayerCamera.PlayerCameraMode)System.Enum.Parse(typeof(PlayerCamera.PlayerCameraMode), cam.ChildNodes[2].InnerText);
        if (cameraModeInput.value == (int)PlayerCamera.PlayerCameraMode.PointLookAt)
        {
            XmlNode points = cam.ChildNodes[3];
            //add points
            if (!lr)
            {
                lr = Instantiate(lineRendererTemplate.gameObject).GetComponent<LineRenderer>();
            }
            lr.gameObject.SetActive(true);
            if (points.ChildNodes.Count == 0)
            {
                var o = Instantiate(pointSphere, editorCamera.transform.position + Vector3.forward, Quaternion.identity, EditorObjectsContainer.transform);
                o.name = "CameraPoint1";
                camGizmoObjs.Add(o);
            }
            for (int i = 0; i < points.ChildNodes.Count; i++)
            {
                var o = Instantiate(pointSphere, LevelLoader.ParseType<Vector3>(points.ChildNodes[i].InnerText), Quaternion.identity, EditorObjectsContainer.transform);
                o.name = "CameraPoint" + (camGizmoObjs.Count + 1);
                camGizmoObjs.Add(o);
            }
            lr.positionCount = camGizmoObjs.Count;
            for (int i = 0; i < camGizmoObjs.Count; i++)
            {
                lr.SetPosition(i, camGizmoObjs[i].transform.position);
            }
        }
        XmlNode cpo = cam.ChildNodes[4];
        CPOInput.text = cpo.InnerText;
        XmlNode nextLevel = doc.ChildNodes[1].ChildNodes[4];
        if (string.IsNullOrEmpty(nextLevel.InnerText))
        {
            nextLevelText.name = "None";
            nextLevelText.text = "None";
        }
        else
        {
            nextLevelText.name = nextLevel.InnerText;
            nextLevelText.text = (((nextLevel.InnerText.Replace("CustomLevels", "")).Replace("\\", "")).Replace("/", "")).Replace(".xml", "");
        }
        UpdateHierarchy();
    }

    public void SaveLevel()
    {
        XmlDocument doc = new XmlDocument();
        XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-16", null);
        XmlElement root = doc.DocumentElement;
        doc.InsertBefore(xmlDeclaration, root);
        XmlElement scene = doc.CreateElement(string.Empty, "Scene", string.Empty);  //main node
        doc.AppendChild(scene);
        XmlElement objs = doc.CreateElement(string.Empty, "Objects", string.Empty); //objects node
        SaveObjs(objs, doc, Scene.rootObject);  //save data about objects
        scene.AppendChild(objs);
        XmlElement sceneInfo = doc.CreateElement(string.Empty, "SceneInfo", string.Empty);  //scene info node
        scene.AppendChild(sceneInfo);
        XmlElement minHeight = doc.CreateElement(string.Empty, "MinHeight", string.Empty);
        XmlText heightLevel = doc.CreateTextNode(minAllowedHeight.ToString("F7"));
        minHeight.AppendChild(heightLevel);
        sceneInfo.AppendChild(minHeight);
        XmlElement playerInfo = doc.CreateElement(string.Empty, "PlayerInfo", string.Empty);    //player info
        scene.AppendChild(playerInfo);
        XmlElement transform = doc.CreateElement(string.Empty, "Transform", string.Empty);
        playerInfo.AppendChild(transform);
        XmlElement pos = doc.CreateElement(string.Empty, "Position", string.Empty);
        XmlElement rot = doc.CreateElement(string.Empty, "Rotation", string.Empty);
        XmlElement scale = doc.CreateElement(string.Empty, "Scale", string.Empty);
        XmlText val = doc.CreateTextNode(Scene.player.transform.position.ToString("F7"));
        pos.AppendChild(val);
        transform.AppendChild(pos);
        val = doc.CreateTextNode(Scene.player.transform.eulerAngles.ToString("F7"));
        rot.AppendChild(val);
        transform.AppendChild(rot);
        val = doc.CreateTextNode(Scene.player.transform.localScale.ToString("F7"));
        scale.AppendChild(val);
        transform.AppendChild(scale);
        XmlElement cameraInfo = doc.CreateElement(string.Empty, "CameraInfo", string.Empty);    //camera info
        pos = doc.CreateElement(string.Empty, "Position", string.Empty);
        val = doc.CreateTextNode(cameraItem.refObj.transform.position.ToString("F7"));
        pos.AppendChild(val);
        cameraInfo.AppendChild(pos);
        rot = doc.CreateElement(string.Empty, "Rotation", string.Empty);
        val = doc.CreateTextNode(cameraItem.refObj.transform.eulerAngles.ToString("F7"));
        rot.AppendChild(val);
        cameraInfo.AppendChild(rot);
        XmlElement cameraMode = doc.CreateElement(string.Empty, "CameraMode", string.Empty);
        val = doc.CreateTextNode(((PlayerCamera.PlayerCameraMode)cameraModeInput.value).ToString());
        cameraMode.AppendChild(val);
        cameraInfo.AppendChild(cameraMode);
        XmlElement points = doc.CreateElement(string.Empty, "Points", string.Empty);
        for (int i = 0; i < camGizmoObjs.Count; i++)
        {
            XmlElement point = doc.CreateElement(string.Empty, "Point", string.Empty);
            point.AppendChild(doc.CreateTextNode(camGizmoObjs[i].transform.position.ToString("F7")));
            points.AppendChild(point);
        }
        cameraInfo.AppendChild(points);
        XmlElement po = doc.CreateElement(string.Empty, "PointOffset", string.Empty);
        po.AppendChild(doc.CreateTextNode(CPOInput.text));
        cameraInfo.AppendChild(po);
        scene.AppendChild(cameraInfo);
        XmlElement nextLevel = doc.CreateElement(string.Empty, "NextLevel", string.Empty);
        nextLevel.AppendChild(doc.CreateTextNode(nextLevelText.name == "None" ? "" : nextLevelText.name));
        scene.AppendChild(nextLevel);
        XmlElement locked = doc.CreateElement(string.Empty, "Locked", string.Empty);
        locked.AppendChild(doc.CreateTextNode("False"));
        scene.AppendChild(locked);
        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/CustomLevels/" + levelName))
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/CustomLevels/" + levelName);
        doc.Save(Directory.GetCurrentDirectory() + "/CustomLevels/" + levelName + "/Level.xml");
#if UNITY_EDITOR
        print("Saved");
#endif
    }

}
