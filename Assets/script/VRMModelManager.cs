using UnityEngine;
using VRM;

public class VRMModelManager : MonoBehaviour
{
    public Animator animator;
    public VRMBlendShapeProxy vrmBlendShapeProxy;
    public GameObject mainModel;
    public Transform neck;
    public Transform spine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator.StopPlayback();
    }

    // Update is called once per frame
    void Update()
    {
        animator.StopPlayback();
    }

}
