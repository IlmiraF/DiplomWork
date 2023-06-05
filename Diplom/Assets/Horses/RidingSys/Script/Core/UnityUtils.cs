using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace RidingSystem
{
    [AddComponentMenu("RidingSystem/Utilities/Tools/Unity Utilities")]
    public class UnityUtils : MonoBehaviour
    {
        public virtual void PauseEditor() => Debug.Break();

        public virtual void Scale_By_Float(float scale) => transform.localScale = Vector3.one * scale;

        public virtual void PauseAllAudio(bool pause)
        {
            if (pause)
            {
                AudioSource[] audios = FindObjectsOfType<AudioSource>();
                foreach (var audio in audios)
                    if (audio.isPlaying) audio.Pause();
            }
            else
            {
                AudioSource[] audios = FindObjectsOfType<AudioSource>();
                foreach (var audio in audios) audio.UnPause();
            }
        }

        public virtual void Time_Freeze(bool value) => Time_Scale(value ? 0 : 1);
        public virtual void Time_Scale(float value) => Time.timeScale = value;
        public virtual void Freeze_Time(bool value) => Time_Freeze(value);

        public void DestroyMe(float time) => Destroy(gameObject, time);

        public void DestroyMe() => Destroy(gameObject);

        public void DestroyMeNextFrame() => StartCoroutine(DestroyNextFrame());

        public void DestroyGameObject(GameObject go) => Destroy(go);

        public void DestroyComponent(Component component) => Destroy(component);

        public void Reset_GameObject(GameObject go) => StartCoroutine(C_Reset_GameObject(go));

        public void Reset_Monobehaviour(MonoBehaviour go) => StartCoroutine(C_Reset_Mono(go));

        public void GameObjectHide(float time) => Invoke(nameof(DisableGo), time);

        public void RandomRotateAroundX() => transform.Rotate(new Vector3(Random.Range(0, 360), 0, 0), Space.Self);

        public void RandomRotateAroundY() => transform.Rotate(new Vector3(0, Random.Range(0, 360), 0), Space.Self);
        public void RandomRotateAroundZ() => transform.Rotate(new Vector3(0, 0, Random.Range(0, 360)), Space.Self);

        public void DebugLog(string value) => Debug.Log($"[{name}]-[{value}]", this);
        public void DebugLog(object value) => Debug.Log($"[{name}]-[{value}]", this);



        public void Rotation_Reset() => transform.localRotation = Quaternion.identity;

        public void Position_Reset() => transform.localPosition = Vector3.zero;

        public void Rotation_Reset(GameObject go) => go.transform.localRotation = Quaternion.identity;

        public void Position_Reset(GameObject go) => go.transform.localPosition = Vector3.zero;

        public void Rotation_Reset(Transform go) => go.localRotation = Quaternion.identity;

        public void Position_Reset(Transform go) => go.localPosition = Vector3.zero;


        public void Parent(Transform value) => transform.parent = value;
        public void Parent(GameObject value) => Parent(value.transform);
        public void Parent(Component value) => Parent(value.transform);


        public void Unparent(Transform value) => value.parent = null;
        public void Unparent(GameObject value) => Unparent(value.transform);
        public void Unparent(Component value) => Unparent(value.transform);

        public void Behaviour_Disable(int index)
        {
            var components = GetComponents<Behaviour>();
            if (components != null)
            {
                components[index % components.Length].enabled = false;
            }
        }

        public void Behaviour_Enable(int index)
        {
            var components = GetComponents<Behaviour>();
            if (components != null)
            {
                components[index % components.Length].enabled = true;
            }
        }

        public void Dont_Destroy_On_Load(GameObject value) => DontDestroyOnLoad(value);

        public void Load_Scene_Additive(string value)
        {
            SceneManager.LoadScene(value, LoadSceneMode.Additive);
        }

        public void Load_Scene(string value)
        {
            SceneManager.LoadScene(value, LoadSceneMode.Single);
        }

        public void Parent_Local(Transform value)
        {
            transform.parent = value;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void Parent_Local(GameObject value) => Parent_Local(value.transform);
        public void Parent_Local(Component value) => Parent_Local(value.transform);


        public void Instantiate(GameObject value) => Instantiate(value, transform.position, transform.rotation);

        public void InstantiateAndParent(GameObject value) => Instantiate(value, transform.position, transform.rotation, transform);


        public static void ShowCursor(bool value)
        {
            Cursor.lockState = !value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = value;
        }

        public static void ShowCursorInvert(bool value) => ShowCursor(!value);


        private void DisableGo() => gameObject.SetActive(false);


        private IEnumerator C_Reset_GameObject(GameObject go)
        {
            if (go.activeInHierarchy)
            {
                go.SetActive(false);
                yield return null;
                go.SetActive(true);

            }
            yield return null;
        }

        IEnumerator C_Reset_Mono(MonoBehaviour go)
        {
            if (go.gameObject.activeInHierarchy)
            {
                go.enabled = (false);
                yield return null;
                go.enabled = (true);

            }
            yield return null;
        }

        IEnumerator DestroyNextFrame()
        {
            yield return null;
            Destroy(gameObject);
        }
    }
}
