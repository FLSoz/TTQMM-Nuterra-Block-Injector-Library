﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nuterra.BlockInjector
{
    internal static class DirectoryBlockLoader
    {

        internal struct BlockBuilder
        {
            public string Name;
            public string Description;
            public bool KeepReferenceRenderers;
            public string GamePrefabReference;
            public int ID;
            public string IconName;
            public string MeshName;
            public string ColliderMeshName;
            public bool SupressBoxColliderFallback;
            public float? Friction;
            public float? StaticFriction;
            public float? Bounciness;
            public string MeshTextureName;
            public string MeshGlossTextureName;
            public string MeshEmissionTextureName;
            public string MeshMaterialName;
            public int Faction;
            public int Category;
            public int Grade;
            public int Price;
            public int HP;
            public int? DamageableType;
            public int Rarity;
            public float? Fragility;
            public float Mass;
            public IntVector3? BlockExtents;
            public bool APsOnlyAtBottom;
            public IntVector3[] Cells;
            public Vector3[] APs;
            public Vector3? ReferenceOffset;
            public Vector3? ReferenceScale;
            public Vector3? ReferenceRotationOffset;
            public string Recipe;
            public SubObj[] SubObjects;

            public JObject JSONBLOCK;

            public struct SubObj
            {
                public string SubOverrideName;
                public string MeshName;
                public int? Layer;
                public bool DestroyExistingColliders;
                public bool MakeBoxCollider;
                public bool MakeSphereCollider;
                public string ColliderMeshName;
                public float? Friction;
                public float? StaticFriction;
                public float? Bounciness;
                public string MeshTextureName;
                public string MeshGlossTextureName;
                public string MeshEmissionTextureName;
                public string MeshMaterialName;
                public Vector3? SubPosition;
                public Vector3? SubScale;
                public Vector3? SubRotation;
                public bool DestroyExistingRenderer;
                //PUT ANIMATION CURVES HERE
                public AnimInfo[] Animation;
                public struct AnimInfo
                {
                    public string ClipName;
                    public Curve[] Curves;

                    public AnimationCurve[] GetAnimationCurves()
                    {
                        var result = new AnimationCurve[Curves.Length];
                        for (int i = 0; i < Curves.Length; i++)
                        {
                            result[i] = Curves[i].ToAnimationCurve();
                        }
                        return result;
                    }

                    public struct Curve
                    {
                        public string ComponentName;
                        public string PropertyName;
                        public Key[] Keys;
                        public AnimationCurve ToAnimationCurve()
                        {
                            var Keyframes = new Keyframe[Keys.Length];
                            for(int i = 0; i < Keys.Length; i++)
                            {
                                Keyframes[i] = Keys[i].ToKeyframe();
                            }
                            return new AnimationCurve(Keyframes);
                        }

                        public struct Key
                        {
                            public float Time;
                            public float Value;
                            public float inTangent;
                            public float outTangent;
                            public Keyframe ToKeyframe()
                            {
                                return new Keyframe(Time, Value, inTangent, outTangent);
                            }
                        }
                    }
                }
            }
        }

        internal static readonly Type MeshT = typeof(Mesh), Texture2DT = typeof(Texture2D), MaterialT = typeof(Material), TextureT = typeof(Texture),
            SpriteT = typeof(Sprite),
            cT = typeof(ChunkTypes);

        static Dictionary<string, DateTime> FileChanged = new Dictionary<string, DateTime>();

        public static void LoadBlocks(bool ParseJSON = true)
        {
            string BlockPath = Path.Combine(
                new DirectoryInfo(
                    Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "../../../"))
                .FullName, "Custom Blocks");
            try
            {
                if (!Directory.Exists(BlockPath))
                {
                    Directory.CreateDirectory(BlockPath);
                }
                string path = BlockPath + "/Example.json";
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, Properties.Resources.ExampleJson);
                }
            }
            catch(Exception E)
            {
                Console.WriteLine("Could not access \"" + BlockPath + "\"!\n"+E.Message);
            }
            var CustomBlocks = new DirectoryInfo(BlockPath);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var cbPng = CustomBlocks.GetFiles("*.png", SearchOption.AllDirectories);
            foreach (FileInfo Png in cbPng)
            {
                try
                {
                    if (!FileChanged.TryGetValue(Png.FullName, out DateTime lastEdit) || lastEdit != Png.LastWriteTime)
                    {
                        Texture2D tex = GameObjectJSON.ImageFromFile(Png.FullName);
                        GameObjectJSON.AddObjectToUserResources<Texture2D>(Texture2DT, tex, Png.Name);
                        GameObjectJSON.AddObjectToUserResources<Texture>(TextureT, tex, Png.Name);
                        GameObjectJSON.AddObjectToUserResources<Sprite>(SpriteT, GameObjectJSON.SpriteFromImage(tex), Png.Name);
                        FileChanged[Png.FullName] = Png.LastWriteTime;
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine("Could not read image " + Png.Name + "\n at " + Png.FullName + "\n" + E.Message + "\n" + E.StackTrace);
                }
            }
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} MS to get json images");

            sw.Restart();
            var cbObj = CustomBlocks.GetFiles("*.obj", SearchOption.AllDirectories);
            foreach (FileInfo Obj in cbObj)
            {
                try
                {
                    if (!FileChanged.TryGetValue(Obj.FullName, out DateTime lastEdit) || lastEdit != Obj.LastWriteTime)
                    {
                        GameObjectJSON.AddObjectToUserResources(GameObjectJSON.MeshFromFile(Obj.FullName), Obj.Name);
                        FileChanged[Obj.FullName] = Obj.LastWriteTime;
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine("Could not read mesh " + Obj.Name + "\n at " + Obj.FullName + "\n" + E.Message + "\n" + E.StackTrace);
                }
            }
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} MS to get json models");

            if (ParseJSON)
            {
                var cbJson = CustomBlocks.GetFiles("*.json", SearchOption.AllDirectories);
                foreach (FileInfo Json in cbJson)
                {
                    if (!FileChanged.TryGetValue(Json.FullName, out DateTime lastEdit) || lastEdit != Json.LastWriteTime)
                    {
                        sw.Restart();
                        CreateJSONBlock(Json);
                        Console.WriteLine($"Took {sw.ElapsedMilliseconds} MS to parse {Json.Name}");
                        FileChanged[Json.FullName] = Json.LastWriteTime;
                    }
                }
            }
            sw.Stop();
        }

        private static void CreateJSONBlock(FileInfo Json)
        {
            try
            {
                //Read JSON
                JObject jObject = JObject.Parse(StripComments(File.ReadAllText(Json.FullName)));
                BlockBuilder jBlock = jObject.ToObject<BlockBuilder>(new JsonSerializer() { MissingMemberHandling = MissingMemberHandling.Ignore });
                BlockPrefabBuilder blockbuilder;
                bool JSONParser = jBlock.JSONBLOCK != null;

                bool HasSubObjs = jBlock.SubObjects != null && jBlock.SubObjects.Length != 0;

                bool BlockAlreadyExists = BlockLoader.CustomBlocks.TryGetValue(jBlock.ID, out var ExistingJSONBlock);
                bool Prefabbed = !string.IsNullOrEmpty(jBlock.GamePrefabReference);
                //Prefab reference
                if (!Prefabbed)
                {
                    blockbuilder = new BlockPrefabBuilder();
                }
                else
                {
                    if (jBlock.ReferenceOffset.HasValue && jBlock.ReferenceOffset != Vector3.zero)
                    {
                        //Offset Prefab
                        blockbuilder = new BlockPrefabBuilder(jBlock.GamePrefabReference, jBlock.ReferenceOffset.Value, !jBlock.KeepReferenceRenderers);
                    }
                    else
                    {
                        blockbuilder = new BlockPrefabBuilder(jBlock.GamePrefabReference, !jBlock.KeepReferenceRenderers);
                    }

                    if (jBlock.ReferenceRotationOffset.HasValue && jBlock.ReferenceRotationOffset != Vector3.zero)
                    {
                        //Add Rotation
                        blockbuilder.Prefab.transform.RotateChildren(jBlock.ReferenceRotationOffset.Value);
                    }

                    if (jBlock.ReferenceScale.HasValue && jBlock.ReferenceScale != Vector3.zero)
                    {
                        for (int ti = 0; ti < blockbuilder.Prefab.transform.childCount; ti++)
                        {
                            var chi = blockbuilder.Prefab.transform.GetChild(ti);
                            //Stretch
                            chi.localPosition = Vector3.Scale(chi.localPosition, jBlock.ReferenceScale.Value);
                            chi.localScale = Vector3.Scale(chi.localScale, jBlock.ReferenceScale.Value);
                        }
                    }
                }

                //If gameobjectJSON exists, use it
                if (JSONParser)
                {
                    GameObjectJSON.CreateGameObject(jObject.Property("JSONBLOCK").Value.ToObject<JObject>(), blockbuilder.Prefab);
                }

                //Set IP
                blockbuilder.SetBlockID(jBlock.ID);

                //Set Category
                if (jBlock.Category != 0)
                {
                    blockbuilder.SetCategory((BlockCategories)jBlock.Category);
                }
                else
                {
                    blockbuilder.SetCategory(BlockCategories.Standard);
                }

                //Set Faction (Corp)
                if (jBlock.Faction != 0)
                {
                    blockbuilder.SetFaction((FactionSubTypes)jBlock.Faction);
                }
                else
                {
                    blockbuilder.SetFaction(FactionSubTypes.GSO);
                }

                //Set Block Grade
                blockbuilder.SetGrade(jBlock.Grade);

                //Set HP
                if (jBlock.HP != 0)
                {
                    blockbuilder.SetHP(jBlock.HP);
                }
                else
                {
                    blockbuilder.SetHP(250);
                }

                //Set DamageableType
                if (jBlock.DamageableType.HasValue)
                {
                    blockbuilder.SetDamageableType((ManDamage.DamageableType)jBlock.DamageableType.Value);
                }

                //Set DetachFragility
                if (jBlock.Fragility.HasValue)
                {
                    blockbuilder.SetDetachFragility(jBlock.Fragility.Value);
                }

                //Set Rarity
                blockbuilder.SetRarity((BlockRarity)jBlock.Rarity);

                //Set Icon
                if (jBlock.IconName != null && jBlock.IconName != "")
                {
                    var Tex = GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, jBlock.IconName);
                    if (Tex == null)
                    {
                        Tex = GameObjectJSON.GetObjectFromGameResources<Texture2D>(Texture2DT, jBlock.IconName);
                        if (Tex == null)
                        {
                            var Spr = GameObjectJSON.GetObjectFromGameResources<Sprite>(jBlock.IconName);
                            if (Spr == null)
                            {
                                blockbuilder.SetIcon((Sprite)null);
                            }
                            else
                            {
                                blockbuilder.SetIcon(Spr);
                            }
                        }
                        else
                        {
                            blockbuilder.SetIcon(Tex);
                        }
                    }
                    else
                    {
                        blockbuilder.SetIcon(Tex);
                    }
                }

                Material localmat = null;
                //Get Material
                if (jBlock.MeshMaterialName != null && jBlock.MeshMaterialName != "")
                {
                    jBlock.MeshMaterialName.Replace("Venture_", "VEN_");
                    jBlock.MeshMaterialName.Replace("GeoCorp_", "GC_");
                    try
                    {
                        localmat = GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, jBlock.MeshMaterialName);
                    }
                    catch { Console.WriteLine(jBlock.MeshMaterialName + " is not a valid Game Material!"); }
                }
                if (localmat == null)
                {
                    localmat = GameObjectJSON.MaterialFromShader();
                }

                bool missingflag1 = string.IsNullOrWhiteSpace(jBlock.MeshTextureName),
                    missingflag2 = string.IsNullOrWhiteSpace(jBlock.MeshGlossTextureName),
                    missingflag3 = string.IsNullOrWhiteSpace(jBlock.MeshEmissionTextureName);
                localmat = GameObjectJSON.SetTexturesToMaterial(true, localmat,
                    missingflag1 ? null :
                    GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, jBlock.MeshTextureName),
                    missingflag2 ? null :
                    GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, jBlock.MeshGlossTextureName),
                    missingflag3 ? null :
                    GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, jBlock.MeshEmissionTextureName));


                PhysicMaterial localphysmat = new PhysicMaterial();
                //Get Collision Material
                if (jBlock.Friction.HasValue)
                {
                    localphysmat.dynamicFriction = jBlock.Friction.Value;
                }
                if (jBlock.StaticFriction.HasValue)
                {
                    localphysmat.staticFriction = jBlock.StaticFriction.Value;
                }
                if (jBlock.Bounciness.HasValue)
                {
                    localphysmat.bounciness = jBlock.Bounciness.Value;
                }

                //Set Model
                {
                    //-Get Mesh
                    Mesh mesh = null;
                    if ((jBlock.MeshName != null && jBlock.MeshName != ""))
                    {
                        mesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, jBlock.MeshName);
                    }
                    //if (mesh == null && !HasSubObjs)
                    //{
                    //    mesh = GameObjectJSON.GetObjectFromGameResources<Mesh>(MeshT, "Cube");
                    //    if (mesh == null)
                    //    {
                    //        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //        mesh = go.GetComponent<MeshFilter>().mesh;
                    //        GameObject.Destroy(go);
                    //    }
                    //}
                    //-Get Collider
                    Mesh colliderMesh = null;
                    if (jBlock.ColliderMeshName != null && jBlock.ColliderMeshName != "")
                    {
                        colliderMesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, jBlock.ColliderMeshName);
                    }
                    //-Apply
                    if (mesh != null)
                    {
                        if (colliderMesh == null)
                        {
                            blockbuilder.SetModel(mesh, !jBlock.SupressBoxColliderFallback, localmat, localphysmat);
                        }
                        else
                        {
                            blockbuilder.SetModel(mesh, colliderMesh, true, localmat, localphysmat);
                        }
                    }
                }
                if (HasSubObjs) //Set SUB MESHES
                {
                    var tr = blockbuilder.Prefab.transform;
                    foreach (var sub in jBlock.SubObjects) //For each SUB
                    {
                        Transform childT = null;
                        string LocalPath;
                        if (sub.SubOverrideName != null && sub.SubOverrideName != "") childT = tr.RecursiveFind(sub.SubOverrideName);
                        GameObject childG = null;
                        bool New = false;
                        if (childT != null)
                        {
                            childG = childT.gameObject;
                            if (sub.Layer.HasValue)
                            {
                                childG.layer = sub.Layer.Value;
                            }
                        }
                        else
                        {
                            string name = "SubObject_" + (tr.childCount + 1).ToString();
                            LocalPath = "/" + name;
                            childG = new GameObject(name);
                            childT = childG.transform;
                            childT.parent = tr;
                            childT.localPosition = Vector3.zero;
                            childT.localRotation = Quaternion.identity;
                            if (sub.Layer.HasValue)
                            {
                                childG.layer = sub.Layer.Value;
                            }
                            else
                            {
                                childG.layer = Globals.inst.layerTank;
                            }
                            New = true;
                        }
                        //-Offset
                        if (sub.SubPosition.HasValue)
                        {
                            childT.localPosition = sub.SubPosition.Value;
                        }
                        if (sub.SubRotation.HasValue)
                        {
                            childT.localRotation = Quaternion.Euler(sub.SubRotation.Value);
                        }
                        //-DestroyCollidersOnObj
                        if (sub.DestroyExistingColliders)
                        {
                            foreach (var collider in childG.GetComponents<Collider>())
                            {
                                Component.DestroyImmediate(collider);
                            }
                        }
                        //-DestroyRendersOnObj
                        if (sub.DestroyExistingRenderer)
                        {
                            foreach (var comp1 in childG.GetComponents<MeshRenderer>())
                            {
                                Component.DestroyImmediate(comp1);
                            }
                            foreach (var comp2 in childG.GetComponents<MeshFilter>())
                            {
                                Component.DestroyImmediate(comp2);
                            }
                        }
                        //-Get Mesh
                        Mesh mesh = null;
                        if (sub.MeshName != null && sub.MeshName != "")
                        {
                            mesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, sub.MeshName);
                        }
                        //-Get Collider
                        Mesh colliderMesh = null;
                        if (sub.ColliderMeshName != null && sub.ColliderMeshName != "")
                        {
                            colliderMesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, sub.ColliderMeshName);
                        }
                        //-Get Material
                        Material mat = localmat;
                        if (!New && !sub.DestroyExistingRenderer)
                        {
                            var ren = childG.GetComponent<MeshRenderer>();
                            if (ren != null)
                            {
                                mat = ren.material;
                            }
                        }
                        if (sub.MeshMaterialName != null && sub.MeshMaterialName != "")
                        {
                            sub.MeshMaterialName.Replace("Venture_", "VEN_");
                            sub.MeshMaterialName.Replace("GeoCorp_", "GC_");
                            try
                            {
                                var mat2 = GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, sub.MeshMaterialName);
                                if (mat2 == null) Console.WriteLine(sub.MeshMaterialName + " is not a valid Game Material!");
                                else mat = mat2;
                            }
                            catch { Console.WriteLine(sub.MeshMaterialName + " is not a valid Game Material!"); }
                        }
                        mat = GameObjectJSON.SetTexturesToMaterial(true, mat,
                            string.IsNullOrWhiteSpace(sub.MeshTextureName) ? null :
                            GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, sub.MeshTextureName),
                            string.IsNullOrWhiteSpace(sub.MeshGlossTextureName) ? null :
                            GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, sub.MeshGlossTextureName),
                            string.IsNullOrWhiteSpace(sub.MeshEmissionTextureName) ? null :
                            GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, sub.MeshEmissionTextureName));

                        //-Get Collision Material
                        PhysicMaterial physmat = localphysmat;
                        bool newphysmat = false;
                        if (sub.Friction.HasValue && sub.Friction.Value != localphysmat.dynamicFriction)
                        {
                            if (!newphysmat) { physmat = CopyPhysicMaterial(localphysmat); newphysmat = true; }
                            physmat.dynamicFriction = sub.Friction.Value;
                        }
                        if (sub.StaticFriction.HasValue && sub.StaticFriction.Value != localphysmat.staticFriction)
                        {
                            if (!newphysmat) { physmat = CopyPhysicMaterial(localphysmat); newphysmat = true; }
                            physmat.staticFriction = sub.StaticFriction.Value;
                        }
                        if (sub.Bounciness.HasValue && sub.Bounciness.Value != localphysmat.bounciness)
                        {
                            if (!newphysmat) { physmat = CopyPhysicMaterial(localphysmat); newphysmat = true; }
                            physmat.bounciness = sub.Bounciness.Value;
                        }
                        //-Apply
                        if (mesh != null)
                        {
                            if (New) childG.AddComponent<MeshFilter>().sharedMesh = mesh;
                            else childG.EnsureComponent<MeshFilter>().sharedMesh = mesh;
                        }

                        childG.EnsureComponent<MeshRenderer>().material = mat;

                        if (colliderMesh != null)
                        {
                            MeshCollider mc;
                            if (New) mc = childG.AddComponent<MeshCollider>();
                            else mc = childG.EnsureComponent<MeshCollider>();
                            mc.convex = true;
                            mc.sharedMesh = colliderMesh;
                            mc.sharedMaterial = physmat;
                        }
                        if (sub.MakeBoxCollider)
                        {
                            if (mesh != null)
                            {
                                mesh.RecalculateBounds();
                                var bc = childG.EnsureComponent<BoxCollider>();
                                bc.size = mesh.bounds.size - Vector3.one * 0.2f;
                                bc.center = mesh.bounds.center;
                                bc.sharedMaterial = physmat;
                            }
                            else
                            {
                                var bc = childG.EnsureComponent<BoxCollider>();
                                bc.size = Vector3.one;
                                bc.center = Vector3.zero;
                                bc.sharedMaterial = physmat;
                            }
                        }
                        if (sub.MakeSphereCollider)
                        {
                            var bc = childG.EnsureComponent<SphereCollider>();
                            bc.radius = 0.5f;
                            bc.center = Vector3.zero;
                            bc.sharedMaterial = physmat;
                        }
                        if (sub.SubScale.HasValue && sub.SubScale != Vector3.zero)
                        {
                            childT.localScale = sub.SubScale.Value;
                        }
                        //-Animation
                        if (sub.Animation != null)
                        {
                            Console.WriteLine("Animation block detected");
                            var mA = tr.GetComponentsInChildren<Animator>(true);
                            if (mA.Length != 0)
                            {
                                var Animator = mA[0];
                                GameObjectJSON.DumpAnimation(Animator);
                                foreach (var anim in sub.Animation)
                                {
                                    GameObjectJSON.ModifyAnimation(Animator, anim.ClipName, childT.GetPath(Animator.transform), GameObjectJSON.AnimationCurveStruct.ConvertToStructArray(anim.Curves));
                                }
                            }
                        }
                    }
                }

                //Set Name
                blockbuilder.SetName(jBlock.Name);

                //Set Desc
                blockbuilder.SetDescription(jBlock.Description);

                //Set Price
                if (jBlock.Price != 0)
                {
                    blockbuilder.SetPrice(jBlock.Price);
                }
                else
                {
                    blockbuilder.SetPrice(500);
                }

                if (jBlock.Cells != null && jBlock.Cells.Length != 0)
                {
                    blockbuilder.SetSizeManual(jBlock.Cells, true);
                }
                else if (jBlock.BlockExtents.HasValue)
                {
                    blockbuilder.SetSize(jBlock.BlockExtents.Value, (jBlock.APsOnlyAtBottom ? BlockPrefabBuilder.AttachmentPoints.Bottom : BlockPrefabBuilder.AttachmentPoints.All));
                }
                if (jBlock.APs != null)
                {
                    blockbuilder.SetAPsManual(jBlock.APs);
                }

                //Set Mass
                if (jBlock.Mass != 0f)
                {
                    blockbuilder.SetMass(jBlock.Mass);
                }
                else
                {
                    blockbuilder.SetMass(1f);
                }

                // REGISTER
                if (BlockAlreadyExists && BlockLoader.AcceptOverwrite)
                {
                    BlockLoader.Register(blockbuilder.Build());
                    blockbuilder.Prefab.SetActive(false);
                }
                else
                    blockbuilder.RegisterLater(6);

                //Recipe
                if (!BlockAlreadyExists && jBlock.Recipe != null && jBlock.Recipe != "")
                {
                    Dictionary<int, int> RecipeBuilder = new Dictionary<int, int>();
                    string[] recipes = jBlock.Recipe.Replace(" ", "").Split(',');
                    foreach (string recipe in recipes)
                    {
                        int chunk = (int)ChunkTypes.Null;
                        try
                        {
                            chunk = (int)(ChunkTypes)Enum.Parse(cT, recipe, true);
                        }
                        catch
                        {
                            if (int.TryParse(recipe, out int result))
                            {
                                chunk = result;
                            }
                        }
                        if (chunk != (int)ChunkTypes.Null)
                        {
                            if (!RecipeBuilder.ContainsKey(chunk))
                            {
                                RecipeBuilder.Add(chunk, 1);
                            }
                            else
                            {
                                RecipeBuilder[chunk]++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("No ChunkTypes found matching given name, nor could parse as ID (int): " + recipe);
                        }
                    }

                    var Input = new CustomRecipe.RecipeInput[RecipeBuilder.Count];
                    int ite = 0;
                    foreach (var pair in RecipeBuilder)
                    {
                        Input[ite] = new CustomRecipe.RecipeInput(pair.Key, pair.Value);
                        ite++;
                    }

                    string fab = "gsofab";
                    switch ((FactionSubTypes)jBlock.Faction)
                    {
                        case FactionSubTypes.GC: fab = "gcfab"; break;
                        case FactionSubTypes.VEN: fab = "venfab"; break;
                        case FactionSubTypes.HE: fab = "hefab"; break;
                        case FactionSubTypes.BF: fab = "bffab"; break;
                    }

                    CustomRecipe.RegisterRecipe(Input, new CustomRecipe.RecipeOutput[1] {
                                new CustomRecipe.RecipeOutput(jBlock.ID)
                            }, RecipeTable.Recipe.OutputType.Items, fab);
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Could not read block " + Json.Name + "\n at " + Json.FullName + "\n\n" + E.Message + "\n" + E.StackTrace);
                BlockLoader.Timer.blocks += $"\nCould not read #{Json.Name} - \"{E.Message}\"";
            }
        }

        private static PhysicMaterial CopyPhysicMaterial(PhysicMaterial original)
        {
            return new PhysicMaterial() { dynamicFriction = original.dynamicFriction, bounciness = original.bounciness, staticFriction = original.staticFriction };
        }
        private static string GetPath(this Transform transform, Transform targetParent = null)
        {
            if (transform == targetParent) return "";
            string result = transform.name;
            Transform parent = transform.parent;
            while(!(parent == targetParent || parent == null))
            {
                result = parent.name + "/" + result;
                parent = parent.parent;
            }
            return result;
        }

        private static Transform RecursiveFind(this Transform transform, string NameOfChild, string HierarchyBuildup = "")
        {
            string cName = NameOfChild.Substring(NameOfChild.LastIndexOf('/') + 1);
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                //Console.WriteLine(child.name);
                if (child.name == cName)
                {
                    HierarchyBuildup += "/" + cName;
                    //Console.WriteLine(HierarchyBuildup + "  " + NameOfChild);
                    if (HierarchyBuildup.EndsWith(NameOfChild))
                    {
                        return child;
                    }
                }
            }
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                var child = RecursiveFind(c, NameOfChild, HierarchyBuildup + "/" + c.name);
                if (child != null)
                {
                    return child;
                }
            }
            return null;
        }

        private static void RotateChildren(this Transform transform, Vector3 Rotation)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform Child = transform.GetChild(i);
                Child.Rotate(Rotation, Space.Self);
                Child.localPosition = Quaternion.Euler(Rotation) * Child.localPosition;
            }
        }
        public static string StripComments(string input)
        {
            // JavaScriptSerializer doesn't accept commented-out JSON,
            // so we'll strip them out ourselves;
            // NOTE: for safety and simplicity, we only support comments on their own lines,
            // not sharing lines with real JSON
            input = Regex.Replace(input, @"^\s*//.*$", "", RegexOptions.Multiline);  // removes comments like this
            input = Regex.Replace(input, @"^\s*/\*(\s|\S)*?\*/\s*$", "", RegexOptions.Multiline); /* comments like this */

            return input;
        }
    }
}