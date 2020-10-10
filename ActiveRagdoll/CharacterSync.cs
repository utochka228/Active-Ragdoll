using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CharacterSync : MonoBehaviour, IPunObservable
{
    public PhotonView PV;
    public Rigidbody[] rb;

    public HingeJoint[] joints = new HingeJoint[2];
    public ConfigurableJoint[] conJoints = new ConfigurableJoint[9];
    Quaternion[] targetRotations = new Quaternion[9]; //Other part of body
    float[] targetPositions = new float[2] { 0f, 0f};//For jivot i chest

    Vector3 latestPos;
    Quaternion latestRot;

    private Vector3[] velocity;
    private Vector3[] angularVelocity;

    bool valuesReceived = false;
    // Start is called before the first frame update
    void Start()
    {
        PV = transform.parent.parent.GetComponent<PhotonView>();
        PV.ObservedComponents.Clear();
        PV.ObservedComponents.Add(this);
        velocity = new Vector3[rb.Length];
        angularVelocity = new Vector3[rb.Length];
        for (int i = 0; i < rb.Length; i++)
        {
            velocity[i] = Vector3.zero;
            angularVelocity[i] = Vector3.zero;
        }
        for (int i = 0; i < 9; i++)
        {
            targetRotations[i] = new Quaternion(0, 0, 0, 1);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine && valuesReceived)
        {
            //Update Object position and Rigidbody parameters
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * 5);
            for (int i = 0; i < rb.Length; i++)
            {
                rb[i].velocity = velocity[i];
                rb[i].angularVelocity = angularVelocity[i];

                if (i < joints.Length)
                {
                    JointSpring spring = joints[i].spring;
                    spring.targetPosition = targetPositions[i];
                    joints[i].spring = spring;
                }

                if (i < conJoints.Length)
                {
                    Quaternion rot = conJoints[i].targetRotation;
                    rot = targetRotations[i];
                    conJoints[i].targetRotation = rot; 
                }
            }
            valuesReceived = false;
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            for (int i = 0; i < rb.Length; i++)
            {
                stream.SendNext(rb[i].velocity);
                stream.SendNext(rb[i].angularVelocity);

                if(i < joints.Length)
                    stream.SendNext(joints[i].spring.targetPosition);

                if (i < conJoints.Length)
                    stream.SendNext(conJoints[i].targetRotation);
            }
        }
        else
        {
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();
            for (int i = 0; i < rb.Length; i++)
            {
                velocity[i] = (Vector3)stream.ReceiveNext();
                angularVelocity[i] = (Vector3)stream.ReceiveNext();

                if (i < joints.Length)
                    targetPositions[i] = (float)stream.ReceiveNext();

                if (i < conJoints.Length)
                    targetRotations[i] = (Quaternion)stream.ReceiveNext();

            }
            valuesReceived = true;
        }
    }
}
