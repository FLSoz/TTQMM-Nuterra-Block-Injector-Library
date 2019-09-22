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
            public string MeshTextureName;
            public string MeshGlossTextureName;
            public string MeshEmissionTextureName;
            public string MeshMaterialName;
            public int Faction;
            public int Category;
            public int Grade;
            public int Price;
            public int HP;
            public float Mass;
            public IntVector3 BlockExtents;
            public bool APsOnlyAtBottom;
            public IntVector3[] Cells;
            public Vector3[] APs;
            public Vector3? ReferenceOffset;
            public Vector3? ReferenceRotationOffset;
            public string Recipe;
            public SubObj[] SubObjects;

            public JObject JSONBLOCK;

            public struct SubObj
            {
                public string SubOverrideName;
                public string MeshName;
                public bool DestroyExistingColliders;
                public bool MakeBoxCollider;
                public string ColliderMeshName;
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

        internal static Type MeshT = typeof(Mesh), Texture2DT = typeof(Texture2D), MaterialT = typeof(Material);

        /*private static void ApplyTex(string FileName, string MatName, Texture2D Tex)
        {
            if (FileName.EndsWith(".1.png"))
            {
                Console.WriteLine($"Set {MatName} to {FileName}");
                GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, MatName, true).mainTexture = Tex;
            }
            else if (FileName.EndsWith(".2.png"))
            {
                Console.WriteLine($"Set {MatName}'s gloss to {FileName}");
                GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, MatName, true).SetTexture("_MetallicGlossMap", Tex);
            }
            else if (FileName.EndsWith(".3.png"))
            {
                Console.WriteLine($"Set {MatName}'s emission to {FileName}");
                GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, MatName, true).SetTexture("_EmissionMap", Tex);
            }
        }

        private static void ApplyTexToMultiple(string FileName, string MatName, Texture2D Tex)
        {
            if (FileName.EndsWith(".1.png"))
            {
                foreach (var game_mat in GameObjectJSON.GetObjectsFromGameResources<Material>(MaterialT, MatName))
                {
                    try
                    {
                        if (game_mat.mainTexture.width >= 1024)
                        {
                            Console.WriteLine($"Set {game_mat.name} to {FileName}");
                            game_mat.mainTexture = Tex;
                        }
                    }
                    catch { }
                }
            }
            else if (FileName.EndsWith(".2.png"))
            {
                foreach (var game_mat in GameObjectJSON.GetObjectsFromGameResources<Material>(MaterialT, MatName))
                {
                    try
                    {
                        if (game_mat.GetTexture("_MetallicGlossMap").width >= 1024)
                        {
                            Console.WriteLine($"Set {game_mat.name}'s gloss to {FileName}");
                            game_mat.SetTexture("_MetallicGlossMap", Tex);
                        }
                    }
                    catch { }
                }
            }
            else if (FileName.EndsWith(".3.png"))
            {
                foreach (var game_mat in GameObjectJSON.GetObjectsFromGameResources<Material>(MaterialT, MatName))
                {
                    try
                    {
                        if (game_mat.GetTexture("_EmissionMap").width >= 1024)
                        {
                            Console.WriteLine($"Set {game_mat.name}'s emission to {FileName}");
                            game_mat.SetTexture("_EmissionMap", Tex);
                        }
                    }
                    catch { }
                }
            }
        }
        */

        public static void LoadBlocks()
        {
            var dir = new DirectoryInfo(Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "../../../"));
            string BlockPath = Path.Combine(dir.FullName, "Custom Blocks");
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
            var cbJson = CustomBlocks.GetFiles("*.json", SearchOption.AllDirectories);
            var cbObj = CustomBlocks.GetFiles("*.obj", SearchOption.AllDirectories);
            var cbPng = CustomBlocks.GetFiles("*.png", SearchOption.AllDirectories);

            foreach (FileInfo Png in cbPng)
            {
                try
                {
                    Texture2D tex = GameObjectJSON.ImageFromFile(Png.FullName);
                    GameObjectJSON.AddObjectToUserResources(tex, Png.Name);
                    //Console.WriteLine("Added " + Png.Name + "\n from " + Png.FullName);
                    //if (Png.Name.Length > 7)
                    //{
                    //    string mat = Png.Name.Substring(0, Png.Name.Length - 6);
                    //    try
                    //    {
                    //        if (mat == "Corp")
                    //        {
                    //            ApplyTexToMultiple(Png.Name, "Corp_", tex);
                    //            Texture2D gso_tex = GameObjectJSON.CropImage(tex, new Rect(0f, 0f, 0.5f, 0.5f));
                    //            ApplyTexToMultiple(Png.Name, "GSO_", gso_tex);

                    //            Texture2D gc_tex = GameObjectJSON.CropImage(tex, new Rect(0.5f, 0f, 0.5f, 0.5f));
                    //            ApplyTexToMultiple(Png.Name, "GC_", gc_tex);

                    //            Texture2D ven_tex = GameObjectJSON.CropImage(tex, new Rect(0f, 0.5f, 0.5f, 0.5f));
                    //            ApplyTexToMultiple(Png.Name, "VEN_", ven_tex);

                    //            Texture2D he_tex = GameObjectJSON.CropImage(tex, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                    //            ApplyTexToMultiple(Png.Name, "HE_", he_tex);
                    //        }
                    //        else if (mat.EndsWith("$"))
                    //        {
                    //            ApplyTexToMultiple(Png.Name, mat.Substring(0, mat.Length - 1), tex);
                    //        }
                    //        else
                    //        {
                    //            ApplyTex(Png.Name, mat, tex);
                    //        }
                    //    }
                    //    catch
                    //    {
                    //        Console.WriteLine("Could not apply texture to game material " + mat);
                    //    }
                    //}
                }
                catch (Exception E)
                {
                    Console.WriteLine("Could not read image " + Png.Name + "\n at " + Png.FullName + "\n" + E.Message + "\n" + E.StackTrace);
                }
            }
            foreach (FileInfo Obj in cbObj)
            {
                try
                {
                    GameObjectJSON.AddObjectToUserResources(GameObjectJSON.MeshFromFile(Obj.FullName), Obj.Name);
                    //Console.WriteLine("Added " + Obj.Name + "\n from " + Obj.FullName);
                }
                catch (Exception E)
                {
                    Console.WriteLine("Could not read mesh " + Obj.Name + "\n at " + Obj.FullName + "\n" + E.Message + "\n" + E.StackTrace);
                }
            }
            foreach (FileInfo Json in cbJson)
            {
                try
                {
                    //Read JSON
                    JObject jObject = JObject.Parse(StripComments(File.ReadAllText(Json.FullName)));
                    var buildablock = jObject.ToObject<BlockBuilder>(new JsonSerializer() { MissingMemberHandling = MissingMemberHandling.Ignore });
                    BlockPrefabBuilder blockbuilder;
                    bool BlockJSON = buildablock.JSONBLOCK != null;

                    bool HasSubObjs = buildablock.SubObjects != null && buildablock.SubObjects.Length != 0;

                    const bool BlockAlreadyExists = false;//ManSpawn.inst.IsValidBlockToSpawn((BlockTypes)buildablock.ID);
                    bool Prefabbed = buildablock.GamePrefabReference != null && buildablock.GamePrefabReference != "";
                    /*
                    if (BlockAlreadyExists)
                    { // BLOCK ALREADY EXISTS
                        blockbuilder = new BlockPrefabBuilder(buildablock.GamePrefabReference, false);
                    }
                    else
                    */
                    {
                        //Prefab reference
                        if (!Prefabbed)
                        {
                            blockbuilder = new BlockPrefabBuilder();
                        }
                        else
                        {
                            Prefabbed = true;
                            if (buildablock.ReferenceOffset.HasValue && buildablock.ReferenceOffset != Vector3.zero)
                            {
                                //Offset Prefab
                                blockbuilder = new BlockPrefabBuilder(buildablock.GamePrefabReference, buildablock.ReferenceOffset.Value, !buildablock.KeepReferenceRenderers);
                            }
                            else
                            {
                                blockbuilder = new BlockPrefabBuilder(buildablock.GamePrefabReference, !buildablock.KeepReferenceRenderers);
                            }

                            if (buildablock.ReferenceRotationOffset.HasValue && buildablock.ReferenceRotationOffset != Vector3.zero)
                            {
                                //Add Rotation
                                blockbuilder.Prefab.transform.RotateChildren(buildablock.ReferenceRotationOffset.Value);
                            }
                        }
                    }

                    //If gameobjectJSON exists, use it
                    if (BlockJSON)
                    {
                        GameObjectJSON.CreateGameObject(jObject.Property("JSONBLOCK").Value.ToObject<JObject>(), blockbuilder.Prefab);
                    }

                    //Set IP
                    if (!BlockAlreadyExists)
                    {
                        blockbuilder.SetBlockID(buildablock.ID);
                    }

                    //Set Category
                    if (!BlockAlreadyExists)
                        if (buildablock.Category != 0)
                        {
                            blockbuilder.SetCategory((BlockCategories)buildablock.Category);
                        }
                        else
                        {
                            blockbuilder.SetCategory(BlockCategories.Standard);
                        }

                    //Set Faction (Corp)
                    if (!BlockAlreadyExists)
                        if (buildablock.Faction != 0)
                        {
                            blockbuilder.SetFaction((FactionSubTypes)buildablock.Faction);
                        }
                        else
                        {
                            blockbuilder.SetFaction(FactionSubTypes.GSO);
                        }

                    //Set Block Grade
                    if (!BlockAlreadyExists)
                        blockbuilder.SetGrade(buildablock.Grade);

                    //Set HP
                    if (buildablock.HP != 0)
                    {
                        blockbuilder.SetHP(buildablock.HP);
                    }
                    else
                    {
                        if (!BlockAlreadyExists)
                            blockbuilder.SetHP(100);
                    }

                    //Set Icon
                    if (buildablock.IconName != null && buildablock.IconName != "")
                    {
                        var Tex = GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, buildablock.IconName);
                        if (Tex == null)
                        {
                            Tex = GameObjectJSON.GetObjectFromGameResources<Texture2D>(Texture2DT, buildablock.IconName);
                            if (Tex == null)
                            {
                                var Spr = GameObjectJSON.GetObjectFromGameResources<Sprite>(buildablock.IconName);
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
                    if (buildablock.MeshMaterialName != null && buildablock.MeshMaterialName != "")
                    {
                        buildablock.MeshMaterialName.Replace("Venture_", "VEN_");
                        buildablock.MeshMaterialName.Replace("GeoCorp_", "GC_");
                        try
                        {
                            localmat = GameObjectJSON.GetObjectFromGameResources<Material>(MaterialT, buildablock.MeshMaterialName);
                        }
                        catch { Console.WriteLine(buildablock.MeshMaterialName + " is not a valid Game Material!"); }
                    }
                    if (localmat == null)
                    {
                        localmat = GameObjectJSON.MaterialFromShader();
                    }

                    bool missingflag1 = string.IsNullOrWhiteSpace(buildablock.MeshTextureName),
                        missingflag2 = string.IsNullOrWhiteSpace(buildablock.MeshGlossTextureName),
                        missingflag3 = string.IsNullOrWhiteSpace(buildablock.MeshEmissionTextureName);
                    localmat = GameObjectJSON.SetTexturesToMaterial(true, localmat,
                        missingflag1 ? null :
                        GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, buildablock.MeshTextureName),
                        missingflag2 ? null :
                        GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, buildablock.MeshGlossTextureName),
                        missingflag3 ? null :
                        GameObjectJSON.GetObjectFromUserResources<Texture2D>(Texture2DT, buildablock.MeshEmissionTextureName));
                    //Set Model
                    {
                        //-Get Mesh
                        Mesh mesh = null;
                        if ((buildablock.MeshName != null && buildablock.MeshName != ""))
                        {
                            mesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, buildablock.MeshName);
                        }
                        if (mesh == null && !HasSubObjs && !BlockAlreadyExists)
                        {
                            mesh = GameObjectJSON.GetObjectFromGameResources<Mesh>(MeshT, "Cube");
                            if (mesh == null)
                            {
                                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                mesh = go.GetComponent<MeshFilter>().mesh;
                                GameObject.Destroy(go);
                            }
                        }
                        //-Get Collider
                        Mesh colliderMesh = null;
                        if (buildablock.ColliderMeshName != null && buildablock.ColliderMeshName != "")
                        {
                            colliderMesh = GameObjectJSON.GetObjectFromUserResources<Mesh>(MeshT, buildablock.ColliderMeshName);
                        }
                        //-Apply
                        if (mesh != null)
                        {
                            if (colliderMesh == null)
                            {
                                blockbuilder.SetModel(mesh, !buildablock.SupressBoxColliderFallback, localmat);
                            }
                            else
                            {
                                blockbuilder.SetModel(mesh, colliderMesh, true, localmat);
                            }
                        }
                    }
                    if (HasSubObjs) //Set SUB MESHES
                    {
                        var tr = blockbuilder.Prefab.transform;
                        foreach (var sub in buildablock.SubObjects) //For each SUB
                        {
                            Transform childT = null;
                            if (sub.SubOverrideName != null && sub.SubOverrideName != "") childT = tr.RecursiveFind(sub.SubOverrideName);
                            GameObject childG = null;
                            bool New = false;
                            if (childT != null)
                            {
                                childG = childT.gameObject;
                            }
                            else
                            {
                                childG = new GameObject();
                                childT = childG.transform;
                                childT.parent = tr;
                                childT.localPosition = Vector3.zero;
                                childT.localRotation = Quaternion.identity;
                                childG.layer = Globals.inst.layerTank;
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
                            }
                            if (sub.MakeBoxCollider)
                            {
                                if (mesh != null)
                                { 
                                    mesh.RecalculateBounds();
                                    var bc = childG.EnsureComponent<BoxCollider>();
                                    bc.size = mesh.bounds.size - Vector3.one * 0.2f;
                                    bc.center = mesh.bounds.center;
                                }
                                else
                                {
                                    var bc = childG.EnsureComponent<BoxCollider>();
                                    bc.size = Vector3.one;
                                    bc.center = Vector3.zero;
                                }
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
                    blockbuilder.SetName(buildablock.Name);

                    //Set Desc
                    blockbuilder.SetDescription(buildablock.Description);

                    //Set Price
                    if (buildablock.Price != 0)
                    {
                        blockbuilder.SetPrice(buildablock.Price);
                    }
                    else
                    {
                        blockbuilder.SetPrice(500);
                    }

                    //Set Size
                    if (!BlockAlreadyExists)
                    {
                        if (buildablock.Cells != null && buildablock.Cells.Length != 0)
                        {
                            blockbuilder.SetSizeManual(buildablock.Cells);
                        }
                        else
                        {
                            IntVector3 extents = buildablock.BlockExtents;
                            if (extents == null)
                            {
                                extents = IntVector3.one;
                            }
                            blockbuilder.SetSize(extents, (buildablock.APsOnlyAtBottom ? BlockPrefabBuilder.AttachmentPoints.Bottom : BlockPrefabBuilder.AttachmentPoints.All));
                        }
                        if (buildablock.APs != null)
                        {
                            blockbuilder.SetAPsManual(buildablock.APs);
                        }
                    }

                    //Set Mass
                    if (buildablock.Mass != 0f)
                    {
                        blockbuilder.SetMass(buildablock.Mass);
                    }
                    else
                    {
                        blockbuilder.SetMass(1f);
                    }

                    // REGISTER
                    blockbuilder.RegisterLater(6);

                    //Recipe
                    if (!BlockAlreadyExists && buildablock.Recipe != null && buildablock.Recipe != "")
                    {
                        Dictionary<int, int> RecipeBuilder = new Dictionary<int, int>();
                        Type cT = typeof(ChunkTypes);
                        string[] recipes = buildablock.Recipe.Replace(" ", "").Split(',');
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
                        switch ((FactionSubTypes)buildablock.Faction)
                        {
                            case FactionSubTypes.GC: fab = "gcfab"; break;
                            case FactionSubTypes.VEN: fab = "venfab"; break;
                            case FactionSubTypes.HE: fab = "hefab"; break;
                            case FactionSubTypes.BF: fab = "bffab"; break;
                        }

                        CustomRecipe.RegisterRecipe(Input, new CustomRecipe.RecipeOutput[1] {
                                new CustomRecipe.RecipeOutput(buildablock.ID)
                            }, RecipeTable.Recipe.OutputType.Items, fab);
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine("Could not read block " + Json.Name + "\n at " + Json.FullName + "\n\n" + E.Message + "\n" + E.StackTrace);
                    BlockLoader.Timer.blocks += $"\nCould not read #{Json.Name} - \"{E.Message}\"";
                }
            }
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

            var child = transform.Find(NameOfChild.Substring(NameOfChild.LastIndexOf('/') + 1));
            if ((child == null || HierarchyBuildup.EndsWith(NameOfChild)) && transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    child = RecursiveFind(transform.GetChild(i), NameOfChild, HierarchyBuildup + "/" + transform.name);
                    if (child != null)
                    {
                        return child;
                    }
                }
            }
            return child;
        }

        private static void RotateChildren(this Transform transform, Vector3 Rotation)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform Child = transform.GetChild(i);
                Child.Rotate(Rotation, Space.Self);
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