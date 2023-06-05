using UnityEngine;

namespace RidingSystem.Controller
{
    [System.Serializable]
    public class MPivots
    {
        public string name = "Pivot";
        public Vector3 position = Vector3.up;
        public Vector3 dir = Vector3.down;
        public float multiplier = 1;
        public int interval = 1;


        [HideInInspector] public bool EditorModify = false;
        [HideInInspector] public int EditorDisplay = 0;
        [HideInInspector] public Color PivotColor = Color.blue;

        public RaycastHit hit;

        public MPivots(string name, Vector3 pos, float mult)
        {
            this.name = name;
            position = pos;
            multiplier = mult;
            dir = Vector3.down;
            PivotColor = Color.blue;
        }

        public Vector3 World(Transform t)
        { return t.TransformPoint(position); }

        public Vector3 WorldDir(Transform t)
        { return t.TransformDirection(dir); }
    }
}