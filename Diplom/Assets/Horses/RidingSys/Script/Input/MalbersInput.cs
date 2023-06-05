using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using RidingSystem.Events;
using RidingSystem.Scriptables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    [AddComponentMenu("RidingSystem/Input/Malbers Input")]
    public class MalbersInput : MInput
    {
        #region Variables
        private ICharacterMove mCharacterMove;
        public IInputSystem InputSystem;

        public InputAxis Horizontal = new InputAxis("Horizontal", true, true);
        public InputAxis Vertical = new InputAxis("Vertical", true, true);
        public InputAxis UpDown = new InputAxis("UpDown", false, true);
        protected IAIControl AI;


        private float horizontal;
        private float vertical;
        private float upDown;
        #endregion

        protected Vector3 RawInputAxis;

        public virtual void SetMoveCharacter(bool val) => MoveCharacter = val;


        protected override void OnEnable()
        {
            base.OnEnable();

            if (UpDown.active)
            {
                try
                {
                    var UPDown = Input.GetAxis(UpDown.name);
                }
                catch
                {
                }
            }
        }

        private void CheckUpDown()
        {
            if (UpDown.active)
            {
#if UNITY_EDITOR
                bool found = false;

                var InputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
                var axesProperty = InputManager.FindProperty("m_Axes");
                for (int i = 0; i < axesProperty.arraySize; ++i)
                {
                    var property = axesProperty.GetArrayElementAtIndex(i);
                    if (property.FindPropertyRelative("m_Name").stringValue.Equals(UpDown.name))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogError($"<B>[Up Down]</B> input doesn't exist. Please select any Character with the Malbers Input Component and hit <b>UpDown -> [Create]</b>", this);
                    enabled = false;
                }
#endif
            }
        }



        protected override void OnDisable()
        {
            base.OnDisable();
            mCharacterMove?.Move(Vector3.zero);
        }


        void Awake()
        {
            InputSystem = DefaultInput.GetInputSystem(PlayerID);

            Horizontal.InputSystem = Vertical.InputSystem = UpDown.InputSystem = InputSystem;
            foreach (var i in inputs)
                i.InputSystem = InputSystem;

            List_to_Dictionary();
            InitializeCharacter();
            MoveCharacter = true;
            AI = this.FindInterface<IAIControl>();
        }

        protected void InitializeCharacter() => mCharacterMove = GetComponent<ICharacterMove>();


        public virtual void UpAxis(bool input)
        {
            if (upDown == -1) return;
            upDown = input ? 1 : 0;
        }

        public virtual void DownAxis(bool input) => upDown = input ? -1 : 0;

        void Update() => SetInput();


        protected override void SetInput()
        {
            horizontal = Horizontal.GetAxis;
            vertical = Vertical.GetAxis;
            upDown = UpDown.GetAxis;

            RawInputAxis = new Vector3(horizontal, upDown, vertical);

            if (mCharacterMove != null)
            {
                mCharacterMove.SetInputAxis(MoveCharacter ? RawInputAxis : Vector3.zero);
            }

            base.SetInput();
        }

        public void ResetInputAxis() => RawInputAxis = Vector3.zero;

        void List_to_Dictionary()
        {
            DInputs = new Dictionary<string, InputRow>();
            foreach (var item in inputs)
                DInputs.Add(item.name, item);
        }
    }
}