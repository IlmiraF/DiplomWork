using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    [AddComponentMenu("RidingSystem/Utilities/Tools/Tags")]
    public class Tags : MonoBehaviour, ITag
    {
        public static List<Tags> TagsHolders;

        public List<Tag> tags = new List<Tag>();
        protected Dictionary<int, Tag> tags_Dic;

        void OnEnable()
        {
            if (TagsHolders == null) TagsHolders = new List<Tags>();
            TagsHolders.Add(this);
        }
        void OnDisable()
        {
            TagsHolders.Remove(this);
        }

        private void Start()
        {
            tags_Dic = new Dictionary<int, Tag>();

            foreach (var tag in tags)
            {
                if (tag == null) continue;

                if (!tags_Dic.ContainsValue(tag))
                {
                    tags_Dic.Add(tag.ID, tag);
                }
            }

            tags = new List<Tag>();

            foreach (var item in tags_Dic)
            {
                tags.Add(item.Value);
            }
        }

        public static List<GameObject> GambeObjectbyTag(Tag tag) { return GambeObjectbyTag(tag.ID); }


        public static List<GameObject> GambeObjectbyTag(int tag)
        {
            List<GameObject> go = new List<GameObject>();

            if (Tags.TagsHolders == null || TagsHolders.Count == 0) return null;

            foreach (var item in TagsHolders)
            {
                if (item.HasTag(tag))
                {
                    go.Add(item.gameObject);
                }
            }

            if (go.Count == 0) return null;
            return go;
        }

        public bool HasTag(Tag tag) { return HasTag(tag.ID); }

        public bool HasTag(int key) { return tags_Dic.ContainsKey(key); }

        public bool HasTag(params Tag[] enteringTags)
        {
            foreach (var tag in enteringTags)
            {
                if (tags_Dic.ContainsKey(tag.ID))
                    return true;
            }
            return false;
        }

        public void AddTag(Tag t)
        {
            if (!tags_Dic.ContainsValue(t))
            {
                tags.Add(t);
                tags_Dic.Add(t.ID, t);
            }
        }


        public bool HasTag(params int[] enteringTags)
        {
            foreach (var tag in enteringTags)
            {
                if (!tags_Dic.ContainsKey(tag))
                    return false;
            }
            return true;
        }
    }

    public static class Tag_Transform_Extension
    {
        public static bool HasMalbersTag(this Transform t, Tag tag)
        {
            var tagC = t.GetComponent<Tags>();
            return tag != null ? tagC.HasTag(tag) : false;
        }

        public static bool HasMalbersTag(this GameObject t, Tag tag)
        {
            var tagC = t.GetComponent<Tags>();
            return tagC != null ? tagC.HasTag(tag) : false;
        }

        public static bool HasMalbersTag(this GameObject t, params Tag[] tags)
        {
            var tagC = t.GetComponent<Tags>();
            return tagC != null ? tagC.HasTag(tags) : false;
        }


        public static bool HasMalbersTagInParent(this Transform t, Tag tag)
        {
            var tagC = t.GetComponentInParent<Tags>();
            return tagC != null ? tagC.HasTag(tag) : false;
        }

        public static bool HasMalbersTagInParent(this Transform t, params Tag[] tags)
        {
            var tagC = t.GetComponentInParent<Tags>();
            return tagC != null ? tagC.HasTag(tags) : false;
        }

        public static bool HasMalbersTagInParent(this GameObject t, Tag tag)
        {
            var tagC = t.GetComponentInParent<Tags>();
            return tagC != null ? tagC.HasTag(tag) : false;
        }

        public static bool HasMalbersTagInParent(this GameObject t, params Tag[] tags)
        {
            var tagC = t.GetComponentInParent<Tags>();
            return tagC != null ? tagC.HasTag(tags) : false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Tags)), CanEditMultipleObjects]
    public class TagsEd : Editor
    {
        SerializedProperty tags;
        private void OnEnable()
        {
            tags = serializedObject.FindProperty("tags");
        }

        public override void OnInspectorGUI()
        {
            MalbersEditor.DrawDescription("Dupicated Tags will cause errors. Keep unique tags in the list");
            serializedObject.Update();
            EditorGUILayout.PropertyField(tags, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}