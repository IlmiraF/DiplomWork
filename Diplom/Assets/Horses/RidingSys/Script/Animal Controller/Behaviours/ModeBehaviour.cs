using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.Controller
{
    public class ModeBehaviour : StateMachineBehaviour
    {
        public ModeID ModeID;

        public bool EnterMode = true;
        public bool ExitMode = true;

        public bool ExitOnAbility = false;
        public int ExitAbility = -1;

        private MAnimal animal;
        private Mode ModeOwner;
        private Ability ActiveAbility;


        public void InitializeBehaviour(MAnimal animal)
        {
            this.animal = animal;
            if (ModeID != null)
            {
                ModeOwner = animal.Mode_Get(ModeID);
            }
            else
            {
                Destroy(this);
            }
        }

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animal) return;

            if (ModeID == null) { Debug.LogError("Mode behaviour needs an ID"); return; }
            if (ModeOwner == null) { Debug.LogError($"There's no [{ModeID.name}] mode on your character"); return; }

            ActiveAbility = ModeOwner.ActiveAbility;
            if (animal.ModeStatus == Int_ID.Loop) return;

            if (EnterMode) ModeOwner.AnimationTagEnter();
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animal) return;
            if (animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash) return;

            if (ExitMode) ModeOwner.AnimationTagExit(ActiveAbility, ExitOnAbility ? ExitAbility : -1);
        }

        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animal) return;
            ModeOwner.OnModeStateMove(stateInfo, animator, layerIndex);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModeBehaviour))]
    public class ModeBehaviourED : Editor
    {
        SerializedProperty EnterMode, ExitMode, ModeID, ExitOnAbility, ExitAbility;
        Color RequiredColor = new Color(1, 0.4f, 0.4f, 1);

        void OnEnable()
        {

            ModeID = serializedObject.FindProperty("ModeID");
            EnterMode = serializedObject.FindProperty("EnterMode");
            ExitMode = serializedObject.FindProperty("ExitMode");
            ExitOnAbility = serializedObject.FindProperty("ExitOnAbility");
            ExitAbility = serializedObject.FindProperty("ExitAbility");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();

            var currentGUIColor = GUI.color;
            GUI.color = ModeID.objectReferenceValue == null ? RequiredColor : currentGUIColor;
            EditorGUIUtility.labelWidth = 70;
            EditorGUILayout.PropertyField(ModeID);

            var width = 42;
            GUI.color = EnterMode.boolValue ? Color.green : currentGUIColor;

            EnterMode.boolValue = GUILayout.Toggle(EnterMode.boolValue,
                               new GUIContent("Enter"), EditorStyles.miniButton, GUILayout.Width(width));


            GUI.color = ExitMode.boolValue ? Color.green : currentGUIColor;

            ExitMode.boolValue = GUILayout.Toggle(ExitMode.boolValue,
                               new GUIContent("Exit"), EditorStyles.miniButton, GUILayout.Width(width));

            GUI.color = currentGUIColor;

            EditorGUILayout.EndHorizontal();

            if (ExitMode.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                ExitOnAbility.boolValue = EditorGUILayout.ToggleLeft(
                    new GUIContent("Exit on Ability", "Used for Playing an Ability and Finish on another Ability Mode."), ExitOnAbility.boolValue, GUILayout.Width(105));

                if (ExitOnAbility.boolValue)
                    EditorGUILayout.PropertyField(ExitAbility,
                    new GUIContent("   ", "Next Ability to do on the Mode. -1 is the Default and the Exit On Ability Logic will be ignored.\n" +
                    "Used for Playing an Ability and Finish on another Ability Mode. E.g.: The wolf can Howl he is Sit. The Howl Ability will go to the Sit Ability Right After"));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUIUtility.labelWidth = 0;
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}