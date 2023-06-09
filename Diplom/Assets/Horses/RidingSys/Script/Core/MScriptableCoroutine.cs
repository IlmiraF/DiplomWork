using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RidingSystem
{
    public class MScriptableCoroutine : MonoBehaviour
    {
        internal List<ScriptableCoroutine> ScriptableCoroutines;
        public static MScriptableCoroutine Main;

        internal void Restart()
        {
            if (Main == null)
            {
                Main = this;
                ScriptableCoroutines = new List<ScriptableCoroutine>();
            }
        }

        private void Awake()
        {
            Restart();
            DontDestroyOnLoad(this);
        }


        public static void PlayCoroutine(ScriptableCoroutine SC, IEnumerator Coroutine)
        {
            Initialize();

            if (Main != null && Main.enabled && Main.isActiveAndEnabled)
            {
                if (!Main.ScriptableCoroutines.Contains(SC))
                {
                    Main.ScriptableCoroutines.Add(SC);
                }
                Main.StartCoroutine(Coroutine);
            }
        }

        public static void Stop_Coroutine(IEnumerator Coroutine)
        {
            Main.StopCoroutine(Coroutine);
        }

        public static void Initialize()
        {
            if (Main == null && Application.isPlaying)
            {
                var ScriptCoro = new GameObject();
                ScriptCoro.name = "Scriptable Coroutines";
                ScriptCoro.AddComponent<MScriptableCoroutine>();
            }
        }

        protected virtual void OnDisable()
        {
            if (ScriptableCoroutines != null)
                foreach (var c in ScriptableCoroutines)
                    c.CleanCoroutine();

            StopAllCoroutines();
        }
    }
}