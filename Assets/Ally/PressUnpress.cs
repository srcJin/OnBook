using UnityEngine;

public class PressUnpress : MonoBehaviour
{
    private Animator mAnimator;
    
    void Start(){
        mAnimator = GetComponent<Animator>();

    }

    void Update(){
        if(mAnimator != null){
            if(Input.GetKeyDown(KeyCode.Space)){
                mAnimator.SetTrigger("TriPress");
            }
            if(Input.GetKeyDown(Keycode.c)){
                mAnimator.SetTrigger("TriUnpress");
            }
        }
    }
}
