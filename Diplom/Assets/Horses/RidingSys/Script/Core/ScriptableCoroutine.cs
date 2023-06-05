using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RidingSystem
{
    public abstract class ScriptableCoroutine : ScriptableObject
    {
        internal Dictionary<Component, IEnumerator> Coroutine;

        internal void StartCoroutine(Component component, IEnumerator ICoroutine)
        {
            if (Coroutine == null) Coroutine = new Dictionary<Component, IEnumerator>();

            if (!Coroutine.ContainsKey(component))
            {
                Coroutine.Add(component, ICoroutine);
                MScriptableCoroutine.PlayCoroutine(this, ICoroutine);
            }
        }


        internal virtual void Stop(Component component)
        {
            if (Coroutine == null) return;

            if (Coroutine.TryGetValue(component, out IEnumerator CurrentCoro))
            {
                MScriptableCoroutine.Stop_Coroutine(CurrentCoro);

                Coroutine.Remove(component);
            }
        }

        internal abstract void Evaluate(MonoBehaviour mono, Transform target, float time, AnimationCurve curve);

        internal virtual void CleanCoroutine()
        {
            if (Coroutine != null)
                foreach (var c in Coroutine)
                    ExitValue(c.Key);

            Coroutine = null;
        }

        internal virtual void ExitValue(Component compoennt) { }
    }
}