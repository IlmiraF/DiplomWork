using UnityEngine;

namespace RidingSystem.Controller
{
    public class AttackTriggerBehaviour : StateMachineBehaviour
    {
        public int AttackTrigger = 1;

        [MinMaxRange(0, 1)]
        public RangedFloat AttackActivation = new RangedFloat(0.3f, 0.6f);

        private bool isOn, isOff;
        private IMDamagerSet[] damagers;


        override public void OnStateEnter(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (damagers == null) damagers = anim.GetComponents<IMDamagerSet>();
            isOn = isOff = false;
        }

        override public void OnStateUpdate(Animator anim, AnimatorStateInfo state, int layer)
        {
            var time = state.normalizedTime % 1;

            if (!isOn && (time >= AttackActivation.minValue))
            {
                foreach (var d in damagers) d.ActivateDamager(AttackTrigger);
                isOn = true;
            }

            if (!isOff && (time >= AttackActivation.maxValue))
            {
                if (anim.IsInTransition(layer) && anim.GetNextAnimatorStateInfo(layer).fullPathHash == state.fullPathHash) return;
                foreach (var d in damagers) d.ActivateDamager(0);
                isOff = true;
            }
        }

        override public void OnStateExit(Animator anim, AnimatorStateInfo state, int layer)
        {
            if (anim.GetCurrentAnimatorStateInfo(layer).fullPathHash == state.fullPathHash) return;

            if (!isOff)
                foreach (var d in damagers) d.ActivateDamager(0);


            isOn = isOff = false;
        }
    }
}