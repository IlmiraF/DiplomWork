using RidingSystem.Scriptables;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RidingSystem.Controller
{
    [System.Serializable]
    public class MSpeedSet : IComparable, IComparer
    {
        public string name;
        public List<StateID> states;
        public List<StanceID> stances;
        public IntReference StartVerticalIndex;
        public IntReference TopIndex;
        public FloatReference BackSpeedMult = new FloatReference(0.5f);

        public FloatReference PitchLerpOn = new FloatReference(10f);

        public FloatReference PitchLerpOff = new FloatReference(10f);

        public FloatReference BankLerp = new FloatReference(10f);
        public List<MSpeed> Speeds;

        public bool HasStances => stances != null && stances.Count > 0;

        public int CurrentIndex { get; set; }

        public MSpeedSet()
        {
            name = "Set Name";
            states = new List<StateID>();
            StartVerticalIndex = new IntReference(1);
            TopIndex = new IntReference(2);
            Speeds = new List<MSpeed>(1) { new MSpeed("SpeedName", 1, 4, 4) };
        }

        public MSpeed this[int index]
        {
            get => Speeds[index];
            set => Speeds[index] = value;
        }

        public MSpeed this[string name] => Speeds.Find(x => x.name == name);



        public bool HasStance(int stance)
        {
            if (!HasStances) return true;
            else return stances.Find(s => s.ID == stance);
        }

        public int Compare(object x, object y)
        {
            bool XHas = (x as MSpeedSet).HasStances;
            bool YHas = (y as MSpeedSet).HasStances;

            if (XHas && YHas)
                return 0;
            else if (XHas && !YHas)
                return 1;
            else return -1;
        }

        public int CompareTo(object obj)
        {
            bool XHas = (obj as MSpeedSet).HasStances;
            bool YHas = HasStances;

            if (XHas && YHas)
                return 0;
            else if (XHas && !YHas)
                return 1;
            else return -1;
        }
    }
    [System.Serializable]
    public struct MSpeed
    {
        public static readonly MSpeed Default = new MSpeed("Default", 1, 4, 4);

        public string name;

        public FloatReference Vertical;

        public FloatReference position;

        public FloatReference lerpPosition;

        public FloatReference lerpPosAnim;

        public FloatReference rotation;

        public FloatReference lerpRotation;

        public FloatReference lerpRotAnim;

        public FloatReference animator;

        public FloatReference lerpAnimator;

        public FloatReference strafeSpeed;

        public FloatReference lerpStrafe;

        public string Name { get => name; set => name = value; }

        public MSpeed(MSpeed newSpeed)
        {
            name = newSpeed.name;

            position = newSpeed.position;
            lerpPosition = newSpeed.lerpPosition;
            lerpPosAnim = newSpeed.lerpPosAnim;

            rotation = newSpeed.rotation;
            lerpRotation = newSpeed.lerpRotation;
            lerpRotAnim = newSpeed.lerpRotAnim;

            animator = newSpeed.animator;
            lerpAnimator = newSpeed.lerpAnimator;
            Vertical = newSpeed.Vertical;
            strafeSpeed = newSpeed.strafeSpeed;
            strafeSpeed = newSpeed.strafeSpeed;
            lerpStrafe = newSpeed.lerpStrafe;
        }


        public MSpeed(string name, float lerpPos, float lerpanim)
        {
            this.name = name;
            Vertical = 1;

            position = 0;
            lerpPosition = lerpPos;
            lerpPosAnim = 4;

            rotation = 0;
            strafeSpeed = 0;
            lerpRotation = 4;
            lerpRotAnim = 4;
            lerpStrafe = 4;

            animator = 1;
            lerpAnimator = lerpanim;
        }

        public MSpeed(string name, float vertical, float lerpPos, float lerpanim)
        {
            this.name = name;
            Vertical = vertical;

            position = 0;
            lerpPosition = lerpPos;
            lerpPosAnim = 4;

            rotation = 0;
            strafeSpeed = 0;
            lerpRotation = 4;
            lerpRotAnim = 4;
            lerpStrafe = 4;


            animator = 1;
            lerpAnimator = lerpanim;
        }


        public MSpeed(string name)
        {
            this.name = name;
            Vertical = 1;

            position = 0;
            lerpPosition = 4;
            lerpPosAnim = 4;


            rotation = 0;
            strafeSpeed = 0;

            lerpRotation = 4;
            lerpRotAnim = 4;
            lerpStrafe = 4;


            animator = 1;
            lerpAnimator = 4;
        }
    }
}