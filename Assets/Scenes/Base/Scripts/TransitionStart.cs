using UnityEngine;

namespace EasyTransition
{
    public class TransitionStart : DemoLoadScene
    {
        private void OnEnable()
        {
            TransitionManager.Instance().Transition(transition, startDelay);
        }
    }
}