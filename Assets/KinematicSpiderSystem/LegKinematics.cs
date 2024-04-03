using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegKinematics : MonoBehaviour
{
    public Transform body;
    public Transform target;

    [Range(1, 50)]
    public int cycles = 1;
    [Range(0, 10)]
    public float verticalStart = 1;

    public bool stepA = true;
    public bool stepB = true;
    public bool stepC = true;

    public float LegLength 
    { 
        get
        {
            float legLength = 0;
            for (int i = 0; i < legSegments.Count; i++)
            {
                legLength += legSegments[i].length;
            }
            return legLength;
        }  
    }

    [System.Serializable]
    public struct LegSegment 
    {
        public Transform transform;
        public float length;
    }

    public List<LegSegment> legSegments = new List<LegSegment>();

    private void Update()
    {
        float legLength = LegLength;//get the leg length for maximum foot position
        Vector3 targetDir = target.position - body.position;
        Vector3 targetPos = target.position;

        //clamp foot position
        if (targetDir.magnitude > legLength)
        {
            targetPos = body.position + targetDir.normalized * legLength;
        }
        Debug.DrawLine(body.position, targetPos);


        //reset leg to inline with new position
        if (stepA) 
        {
            //setup starting leg direction before IK is calculated to be up and towards the foot
            Vector3 legDir = targetPos - body.position;
            legDir.y = Mathf.Max(0, (legLength - (body.position - targetPos).magnitude) * verticalStart);
            legDir.Normalize();

            //move leg parts to new direction
            Vector3 lastEnd = body.position;
            for (int i = 0; i < legSegments.Count; i++)
            {
                legSegments[i].transform.position = lastEnd;
                legSegments[i].transform.rotation = Quaternion.LookRotation(legDir, Vector3.up);
                lastEnd = lastEnd + legDir * legSegments[i].length;
            }
        }       

        //Repeat steps b and c multiple times to get a more acurate final position
        for (int c = 0; c < cycles; c++)
        {
            //Shift leg parts towards target position
            if(stepB) 
            {
                Vector3 stepATarget = targetPos;
                for (int i = legSegments.Count - 1; i >= 0; i--)
                {
                    Vector3 oldPos = legSegments[i].transform.position;
                    Vector3 moveDir = (stepATarget - oldPos).normalized;

                    legSegments[i].transform.position = stepATarget - (moveDir * legSegments[i].length);
                    legSegments[i].transform.rotation = Quaternion.LookRotation(moveDir, stepATarget - body.position);
                    stepATarget = legSegments[i].transform.position;
                }
            }

            //Shift leg parts back to body
            if(stepC) 
            {
                Vector3 stepBTarget = body.position;
                for (int i = 0; i < legSegments.Count; i++)
                {
                    Vector3 oldPos = legSegments[i].transform.position;
                    Vector3 oldTarget = legSegments[i].transform.forward * legSegments[i].length + oldPos;
                    Vector3 angleDir = (stepBTarget - oldTarget).normalized;

                    legSegments[i].transform.position = stepBTarget;
                    legSegments[i].transform.rotation = Quaternion.LookRotation(-angleDir, stepBTarget - body.position + new Vector3(0,0.01f,0));
                    stepBTarget = stepBTarget - (angleDir * legSegments[i].length);
                }
            }
        }
    }
}
