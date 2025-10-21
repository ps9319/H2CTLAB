using UnityEngine;

namespace EasyTransition
{

	public class DemoLoadScene : MonoBehaviour
	{
		public TransitionSettings transition;
		public float startDelay;


		public void LoadScene(string _sceneName)
		{
			TransitionManager.Instance().Transition(_sceneName, transition, startDelay);
		}

		public void LoadScene2()
		{
			TransitionManager.Instance().Transition(transition, startDelay);
		}

		public float getStartDelay(){
			return startDelay;
		}

		public float setStartDelay(float _startDelay){
			startDelay = _startDelay;
			return startDelay;
		}

	}
}


