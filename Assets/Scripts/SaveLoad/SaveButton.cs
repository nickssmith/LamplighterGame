using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour {

    SaveManager Saver;
	void Start () {
		Button btn = this.gameObject.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
        Saver  = gameObject.GetComponentInParent<SaveManager>();
		
	}

	void TaskOnClick(){
        Saver.Save();



	}
}
