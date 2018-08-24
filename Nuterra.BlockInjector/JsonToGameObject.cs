﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Reflection;

namespace Nuterra.BlockInjector
{
    public static class GameObjectJSON
    {
        private static Dictionary<Type, Dictionary<string, UnityEngine.Object>> LoadedResources = new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();

        public static Material MaterialFromShader(string ShaderName = "Standard")
        {
            var shader = Shader.Find(ShaderName);
            return new Material(shader);
        }

        public static T GetObjectFromGameResources<T>(string targetName) where T : UnityEngine.Object
        {
            T searchresult = null;
            T[] search = Resources.FindObjectsOfTypeAll<T>();
            string failedsearch = "";
            for (int i = 0; i < search.Length; i++)
            {
                if (search[i].name == targetName)
                {
                    searchresult = search[i];
                    break;
                }
                failedsearch += search[i].name + "; ";
            }
            if (searchresult == null)
            {
                Debug.Log("Could not find resource: " + targetName + "\n\nThis is what exists for that type:\n" + (failedsearch == "" ? "Nothing. Nothing exists for that type." : failedsearch));
            }
            return searchresult;
        }

        public static Texture2D ImageFromFile(string localPath)
        {
            Texture2D texture;
            string _localPath = System.IO.Path.Combine(Assembly.GetCallingAssembly().Location, "../" + localPath);
            byte[] data;
            if (System.IO.File.Exists(_localPath))
                data = System.IO.File.ReadAllBytes(_localPath);
            else if (System.IO.File.Exists(localPath))
                data = System.IO.File.ReadAllBytes(localPath);
            else throw new NullReferenceException("The file specified could not be found in " + localPath + " or " + _localPath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            return texture;
        }

        public static Sprite SpriteFromImage(Texture2D texture, float Scale = 1f)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width * 0.5f, texture.height * 0.5f), Mathf.Max(texture.width, texture.height) * Scale);
        }

        public static Mesh MeshFromFile(string localPath)
        {
            string _localPath = System.IO.Path.Combine(Assembly.GetCallingAssembly().Location, "../" + localPath);
            if (!System.IO.File.Exists(_localPath))
            {
                if (System.IO.File.Exists(localPath))
                {
                    _localPath = localPath;
                }
                else throw new NullReferenceException("The file specified could not be found in " + localPath + " or " + _localPath);
            }
            return ObjImporter.ImportFile(_localPath);
        }

        public static void AddObjectToUserResources<T>(T Object, string Name) where T : UnityEngine.Object
        {
            Type type = typeof(T);
            if (!LoadedResources.ContainsKey(type))
            {
                LoadedResources.Add(type, new Dictionary<string, UnityEngine.Object>());
            }
            LoadedResources[type].Add(Name, Object);
        }

        public static GameObject CreateGameObject(string json)
        {
           return CreateGameObject(Newtonsoft.Json.Linq.JObject.Parse(json));
        }

        public static GameObject CreateGameObject(JObject json, GameObject GameObjectToPopulate = null)
        {
            GameObject result;
            if (GameObjectToPopulate == null)
            {
                result = new GameObject("Deserialized Object");
            }
            else
                result = GameObjectToPopulate;
            foreach (JProperty property in json.Properties())
            {
                try
                {
                    if (property.Name.StartsWith("UnityEngine.GameObject"))
                    {
                        string name = "Object Child";
                        int GetCustomName = property.Name.IndexOf('|');
                        if (GetCustomName != -1)
                        {
                            name = property.Name.Substring(GetCustomName + 1);
                        }
                        GameObject newGameObject = new GameObject(name);
                        newGameObject.transform.parent = result.transform;
                        CreateGameObject(property.Value as JObject, newGameObject);
                    }
                    else
                    {
                        Type componentType = Type.GetType(property.Name);
                        if (componentType == null)
                        {
                            Debug.LogWarning(property.Name + " is not a type!");
                            continue;
                        }
                        object component = result.GetComponent(componentType);
                        if (component as Component == null)
                        {
                            component = result.AddComponent(componentType);
                            if (component == null)
                            {
                                Debug.LogWarning(property.Name + " is a null component, but does not throw an exception...");
                                continue;
                            }
                            Debug.Log("Created " + property.Name);
                        }
                        ApplyValues(component, componentType, property.Value as JObject);
                        Debug.Log("Set values of " + property.Name);
                    }
                }
                catch (Exception E)
                {
                    Debug.LogException(E);
                }
            }

            return result;
        }

        public static object ApplyValues(object instance, Type instanceType, JObject json)
        {
            Debug.Log("Going down");
            object _instance = instance;
            BindingFlags bind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (JProperty property in json.Properties())
            {
                try
                {
                    Debug.Log(property.Name);
                    FieldInfo tField = instanceType.GetField(property.Name, bind);
                    PropertyInfo tProp = instanceType.GetProperty(property.Name, bind);
                    bool UseField = tProp == null;
                    if (UseField)
                    {
                        if (tField == null)
                        {
                            Debug.Log("skipping...");
                            continue;
                        }
                    }
                    if (property.Value is JObject)
                    {
                        if (UseField)
                        {
                            object original = tField.GetValue(instance);
                            object rewrite = ApplyValues(original, tField.FieldType, property.Value as JObject);
                            try { tField.SetValue(_instance, rewrite); } catch { }
                        }
                        else
                        {
                            object original = tProp.GetValue(instance, null);
                            object rewrite = ApplyValues(original, tProp.PropertyType, property.Value as JObject);
                            if (tProp.CanWrite)
                                try { tProp.SetValue(_instance, rewrite, null); } catch { }
                        }
                    }
                    if (property.Value is JValue)
                    {
                        try
                        {
                            Debug.Log("Setting value");
                            if (UseField)
                            {
                                tField.SetValue(_instance, property.Value.ToObject(tField.FieldType));
                            }
                            else
                            {
                                tProp.SetValue(_instance, property.Value.ToObject(tProp.PropertyType), null);
                            }
                        }
                        catch
                        {
                            string cache = property.Value.ToObject<string>();
                            string targetName;
                            Type type;
                            if (cache.Contains('|'))
                            {
                                string[] cachepart = cache.Split('|');
                                type = Type.GetType(cachepart[0]);
                                targetName = cachepart[1];
                            }
                            else
                            {
                                type = UseField ? tField.FieldType : tProp.PropertyType;
                                targetName = cache;
                            }
                            UnityEngine.Object searchresult = null;
                            if (LoadedResources.ContainsKey(type) && LoadedResources[type].ContainsKey(targetName))
                            {
                                searchresult = LoadedResources[type][targetName];
                                Debug.Log("Setting value from user resource reference");
                            }
                            else
                            {
                                UnityEngine.Object[] search = Resources.FindObjectsOfTypeAll(type);
                                string failedsearch = "";
                                for (int i = 0; i < search.Length; i++)
                                {
                                    if (search[i].name == targetName)
                                    {
                                        searchresult = search[i];
                                        Debug.Log("Setting value from existing resource reference");
                                        break;
                                    }
                                    failedsearch += "(" + search[i].name + ") ";
                                }
                                if (searchresult == null)
                                {
                                    Debug.Log("Could not find resource: " + targetName + "\n\nThis is what exists for that type:\n" + (failedsearch == "" ? "Nothing. Nothing exists for that type." : failedsearch));
                                }
                            }
                            if (UseField)
                            {
                                tField.SetValue(_instance, searchresult);
                            }
                            else
                            {
                                tProp.SetValue(_instance, searchresult, null);
                            }
                        }
                    }
                }
                catch (Exception E) { Debug.LogException(E); }
            }
            Debug.Log("Going up");
            return _instance;
        }
    }
}
