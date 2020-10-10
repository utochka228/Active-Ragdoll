using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public PhotonView PV;

    public Rigidbody[] partsOfBody;
    public Transform pelvis;

    Vector3 COM;
    public Transform COMpos;
    public float COMposOffset = 1f;

    private ConfigurableJoint leftLeg, leftKnee, rightLeg,  rightKnee;

    public Camera camera;

    public float moveSpeed = 100f, jumpForce = 20f, stepSmooth = 2f, turnSmoothTime = 0.2f, legsDelay = 1f;

    float turnSmoothVelocity, currentAngle, targetRotation;

    private bool leftLegMoving = true;
    private bool leftLegIsGrounded, rightLegIsGrounded, StandUping, canStandUp, falled, falling,
         jumping;
    public bool knockOuted;
    public LayerMask legMask;

    RigidbodyConstraints previousConst;

    Vector3 startVector;
    Vector3 pelvis2Vector;
    Vector3 standingRot;

    // Start is called before the first frame update
    void Start()
    {
        PV = transform.parent.parent.GetComponent<PhotonView>();
        transform.parent.parent.GetComponent<AvatarSetup>().myCamera.GetComponent<CameraFollow>().target = transform;

        camera = transform.parent.parent.GetComponent<AvatarSetup>().myCamera.GetComponent<Camera>();

        leftLeg = partsOfBody[1].GetComponent<ConfigurableJoint>();
        rightLeg = partsOfBody[3].GetComponent<ConfigurableJoint>();
        leftKnee = partsOfBody[2].GetComponent<ConfigurableJoint>();
        rightKnee = partsOfBody[4].GetComponent<ConfigurableJoint>();

        CalculateCOM();
        COMpos.position = COM;

        leftLegMoving = true;

        previousConst = partsOfBody[0].constraints;

        startVector = partsOfBody[0].transform.right;
        pelvis2Vector = partsOfBody[5].transform.right;

        standingRot = new Vector3(0f, transform.rotation.y, -90f);

    }


    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine) return;
        CalculateCOM();

        currentAngle = Vector3.Angle(startVector, partsOfBody[0].transform.right);
        if (currentAngle >= 80f)
        {
            falled = true;
            falling = false;
            StartCoroutine(StandUpDelay());
        }
        else
        {
            falled = false;
            canStandUp = false;
        }

        if(falled == false && knockOuted == false && StandUping == false)
        {
            Vector2 input = new Vector2(-Input.GetAxisRaw("Horizontal"), -Input.GetAxisRaw("Vertical"));
            Vector2 inputDir = input.normalized;

            if (inputDir != Vector2.zero)
            {
                targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + camera.transform.eulerAngles.y;
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, -90f);
            }
            //Falling condition
            float pelvis2Angle = Vector3.Angle(pelvis2Vector, partsOfBody[5].transform.right);
            if (/*Vector3.Distance(COM, COMpos.position) > COMposOffset &&*/ pelvis2Angle > 45f)
            {
                falling = true;
                partsOfBody[0].constraints = RigidbodyConstraints.None;
                Balancing();
            }
            else
            {
                falling = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            knockOuted = true;
        }

        if (Input.GetKeyDown(KeyCode.G))
            canManipulate = !canManipulate;

        if (knockOuted)
        {
            partsOfBody[0].constraints = RigidbodyConstraints.None;
        }

        if (falled && Input.anyKeyDown && canStandUp)
        {
            StandUping = true;
        }

        if(StandUping)
            StandUp();

        if(!jumping)
            LegsMovingCycle();

        if(canManipulate)
            SetPositionToArms();
    }
    public bool canManipulate = true;
    IEnumerator StandUpDelay()
    {
        yield return new WaitForSeconds(3f);
        canStandUp = true;
    }

    void FixedUpdate()
    {
        //Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        //partsOfBody[0].AddForce(movement * moveSpeed, ForceMode.VelocityChange);
        if (!PV.IsMine) return;
        CheckGroundAndJump();
        
    }
    float h = 0f;
    float v = 0f;
    public float mouseSensativity = 2f;
    public float rotDeltaDegree = 2f;

    public bool rotateLeftArm = true;
    public bool rotateRightArm = true;
    void SetPositionToArms()
    {
        //Enable Arm
        if (Input.GetKeyDown(KeyCode.Alpha1))
            rotateLeftArm = !rotateLeftArm;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            rotateRightArm = !rotateRightArm;

        v += -Input.GetAxis("Mouse Y") * mouseSensativity;
        v = Mathf.Clamp(v, -90f, 90f);

        if (targetRotation >= 135f && targetRotation <= 225f)
            h += -Input.GetAxis("Mouse X") * mouseSensativity;
        else
            h += Input.GetAxis("Mouse X") * mouseSensativity;
        h = Mathf.Clamp(h, 0f, 180f);

        //Left Arm Rot
        Vector3 leftRotateTO = Vector3.zero;
        if (rotateLeftArm)
        {
            leftRotateTO = new Vector3(0, h, v);
        }
        else
        {
            leftRotateTO = new Vector3(0, 0, 0.65f * 100f);
        }
        Quaternion leftArmRot = partsOfBody[7].GetComponent<ConfigurableJoint>().targetRotation;
        leftArmRot = Quaternion.RotateTowards(leftArmRot, Quaternion.Euler(leftRotateTO), rotDeltaDegree * Time.deltaTime);
        partsOfBody[7].GetComponent<ConfigurableJoint>().targetRotation = leftArmRot;

        //Right Arm Rot
        Vector3 rightRotateTO = Vector3.zero;
        if (rotateRightArm)
        {
            rightRotateTO = new Vector3(0, 180f - h, -v);
        }
        else
        {
            rightRotateTO = new Vector3(0, 0, -0.65f * 100f);
        }
        Quaternion rightArmRot = partsOfBody[9].GetComponent<ConfigurableJoint>().targetRotation;
        rightArmRot = Quaternion.RotateTowards(rightArmRot, Quaternion.Euler(rightRotateTO), rotDeltaDegree * Time.deltaTime);
        partsOfBody[9].GetComponent<ConfigurableJoint>().targetRotation = rightArmRot;
    }

    public void CalculateCOM()
    {
        Vector3 num = Vector3.zero;
        float denum = 0;
        for (int i = 0; i < partsOfBody.Length; i++)
        {
            num += partsOfBody[i].mass * partsOfBody[i].transform.position;
            denum += partsOfBody[i].mass;
        }
        COM = num / denum;
    }

    void CheckGroundAndJump()
    {
        //Left leg check
        Ray lRay = new Ray(partsOfBody[2].transform.GetChild(0).transform.position, Vector3.down);
        RaycastHit lHit;
        Debug.DrawRay(partsOfBody[2].transform.GetChild(0).transform.position, Vector3.down, Color.red, 0.1f);
        if (Physics.Raycast(lRay, out lHit, 0.1f, legMask))
        {
            leftLegIsGrounded = true;
            jumping = false;
        }
        else
        {
            leftLegIsGrounded = false;
        }

        //Right leg check
        Ray rRay = new Ray(partsOfBody[4].transform.GetChild(0).transform.position, Vector3.down);
        RaycastHit rHit;
        Debug.DrawRay(partsOfBody[4].transform.GetChild(0).transform.position, Vector3.down, Color.red, 0.1f);
        if (Physics.Raycast(rRay, out rHit, 0.1f, legMask))
        {
            rightLegIsGrounded = true;
            jumping = false;
        }
        else
        {
            rightLegIsGrounded = false;
        }



        //Jump
        float axisesValue = 0f;
        if (Input.GetAxis("Vertical") != 0)
        {
            axisesValue = Input.GetAxis("Vertical");
        }
        if (Input.GetAxis("Horizontal") != 0)
        {
            axisesValue = Input.GetAxis("Horizontal");
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(leftLegIsGrounded || rightLegIsGrounded)
            {
                partsOfBody[0].AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
                jumping = true;
            }

        }
    }

    float legsLerpTime = 0f;

    void LegsMovingCycle()
    {
        float axisInputValue = 0;

        if (Input.GetKeyDown(KeyCode.D))
            leftLegMoving = false;

        if (Input.GetAxis("Vertical") != 0)
        {
            axisInputValue = Input.GetAxis("Vertical");
            legsLerpTime += stepSmooth * Time.deltaTime;
        }
        if (Input.GetAxis("Horizontal") != 0)
        {
            axisInputValue = Input.GetAxis("Horizontal");
            legsLerpTime += stepSmooth * Time.deltaTime;
        }
        if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            legsLerpTime = 0f;

        legsLerpTime = Mathf.Clamp(legsLerpTime, 0f, 1f);
        axisInputValue = Mathf.Abs(axisInputValue) * 100f;

        if (leftLegMoving)
        {
            Quaternion rightLegValue = Quaternion.Lerp(rightLeg.targetRotation, Quaternion.Euler(0, 0, 0), legsLerpTime);
            rightLeg.targetRotation = rightLegValue;
            Quaternion rightKneeValue = Quaternion.Lerp(rightKnee.targetRotation, Quaternion.Euler(0, 0, 0), legsLerpTime);
            rightKnee.targetRotation = rightKneeValue;

            leftLeg.targetRotation = Quaternion.Lerp(leftLeg.targetRotation, Quaternion.Euler(0, 0, -axisInputValue), legsLerpTime);
            leftKnee.targetRotation = Quaternion.Lerp(leftKnee.targetRotation, Quaternion.Euler(0, 0, axisInputValue), legsLerpTime);
            if (Mathf.Abs(leftLeg.targetRotation.z) >= 0.65f && rightLeg.targetRotation.z == rightLegValue.z)
            {
                StartCoroutine(stepDelay(false));
            }
        }
        else
        {
            Quaternion leftLegValue = Quaternion.Lerp(leftLeg.targetRotation, Quaternion.Euler(0, 0, 0), legsLerpTime);
            leftLeg.targetRotation = leftLegValue;
            Quaternion leftKneeValue = Quaternion.Lerp(leftKnee.targetRotation, Quaternion.Euler(0, 0, 0), legsLerpTime);
            leftKnee.targetRotation = leftKneeValue;

            rightLeg.targetRotation = Quaternion.Lerp(rightLeg.targetRotation, Quaternion.Euler(0, 0, axisInputValue), legsLerpTime);
            rightKnee.targetRotation = Quaternion.Lerp(rightKnee.targetRotation, Quaternion.Euler(0, 0, -axisInputValue), legsLerpTime);
            if (Mathf.Abs(rightLeg.targetRotation.z) >= 0.65f && leftLeg.targetRotation.z == leftLegValue.z)
            {
                StartCoroutine(stepDelay(true));
            }
        }
    }

    IEnumerator stepDelay(bool value)
    {
        yield return new WaitForSeconds(legsDelay);
        legsLerpTime = 0f;
        leftLegMoving = value;
    }

    void Balancing()
    {
        //Balancing
    }
    float lerpTime = 0f;
    [Range(0f, 1f)]
    public float lerpSmooth = 0.1f;
    void StandUp()
    {
        //standing up!
        lerpTime += lerpSmooth * Time.deltaTime;
        lerpTime = Mathf.Clamp(lerpTime, 0f, 1f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(standingRot), lerpTime);

        if(currentAngle <= 5f)
        {
            lerpTime = 0f;
            partsOfBody[0].constraints = previousConst;
            knockOuted = false;
            StandUping = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(COM, 0.5f);
        Gizmos.DrawWireSphere(COMpos.position, COMposOffset);
    }
}
