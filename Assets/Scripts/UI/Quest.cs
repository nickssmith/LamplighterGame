using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class Quest : MonoBehaviour
{

    /*
Quest
  Name
  Giver
  Reward
  Owner/doer
  List of stages
  Task to do when done / task before quest was started



    current stage in list (list index)
    */

    void Start()
    {



    }

    void Update()
    {

    }


    public void SetNextTask(){
        // get next stage
        // set its task etc as current
        // set next task to QUEST
    }

    // todo read from current stage, then
    public void GetDialogOptionToChoose(){
        // return int of what option to choose in a dialog
    }

}



public class QuestStage : MonoBehaviour
{
    /*

Stage
  Name
  Desc
  Type (goto kill buy)
  Task to do (like type but exact task for character)
  Target transform
  List of dialog options to select 
  Target object name
    */
}