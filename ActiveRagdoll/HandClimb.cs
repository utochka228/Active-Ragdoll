using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandClimb : MonoBehaviour
{
    public int mouseKey = 0;

    private bool inZone, climbed, grab;
    public bool isLeftArm;

    private GameObject myClimbedPoint;

    public RagdollController ragController;

    public float offset;

    void OnTriggerStay(Collider other)
    {
        if(other.tag == "Climb")
        {
            inZone = true;
            if (grab)
            {
                climbed = !climbed;

                if(climbed)
                {
                    myClimbedPoint = new GameObject();
                    Vector3 pos = transform.position + (transform.right * offset);
                    myClimbedPoint.transform.position = pos;

                    Rigidbody rb = myClimbedPoint.AddComponent<Rigidbody>();
                    rb.isKinematic = true;

                    HingeJoint hj = gameObject.AddComponent<HingeJoint>();
                    hj.connectedBody = rb;
                    hj.useLimits = true;

                    ragController.knockOuted = true;

                    //if (isLeftArm)
                    //    ragController.rotateLeftArm = false;
                    //else
                    //    ragController.rotateRightArm = false;
                }
                else
                {
                    Destroy(myClimbedPoint);
                    HingeJoint hj = GetComponent<HingeJoint>();
                    Destroy(hj);

                    //if (isLeftArm)
                    //    ragController.rotateLeftArm = true;
                    //else
                    //    ragController.rotateRightArm = true;
                }
                
                Debug.Log("CLIMB: " + climbed);
                grab = false;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Climb")
            inZone = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(mouseKey) && inZone)
        {
            grab = true;
        }
    }
}
