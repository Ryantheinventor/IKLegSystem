using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    public Transform bodyRoot;

    public float targetBodyHeight = 3;

    public float bodyMoveSpeed = 1;

    public float stepDist;

    public List<Gate> gates;

    public List<LegConfig> gateA;
    public List<LegConfig> gateB;

    public LayerMask walkableLayers;

    public float stepTime = 1;
    private float curStepTime = 0;
    public AnimationCurve stepHeightCurve;
    public float turnSpeed = 90;
    public float standingStepDist = 0.001f;

    [System.Serializable]
    public struct Gate 
    {
        public List<LegConfig> legs;
    }

    [System.Serializable]
    public struct LegConfig 
    {
        public Transform footPosition;
        public Transform targetingTransform;
        public LegKinematics leg;
        internal Vector3 targetPos;
    }

    private void Awake()
    {
        //initalize the foot positions
        foreach (Gate gate in gates) 
        {
            for (int i = 0; i < gate.legs.Count; i++)
            {
                LegConfig config = gate.legs[i];
                config.targetPos = gate.legs[i].footPosition.position;
                gate.legs[i] = config;
            }
        }
    }

    private void Update()
    {
        //find move vector
        Vector3 targetBodyPos = CameraTargetSystem.targetPos + new Vector3(0, targetBodyHeight, 0);
        Vector3 moveVec = targetBodyPos - bodyRoot.position;
        Vector3 moveThisFrame = moveVec.normalized * bodyMoveSpeed * Time.deltaTime;
        if (moveVec.magnitude < bodyMoveSpeed * Time.deltaTime) 
        {
            moveThisFrame = moveVec;
        }

        //rotate the body
        Vector3 lookVec = moveVec;
        lookVec.y = 0;
        if (lookVec.magnitude > 0.2f) 
        {
            lookVec.Normalize();
            bodyRoot.rotation = Quaternion.RotateTowards(bodyRoot.rotation, Quaternion.LookRotation(lookVec, Vector3.up), turnSpeed * Time.deltaTime);
        }



        //move body
        bodyRoot.position += moveThisFrame;
        float minStepDist = moveVec.magnitude > 0.001f ? stepDist : standingStepDist;


        //find current ideal target position for legs

        List<float> gateDists = new List<float>();
        for (int g = 0; g < gates.Count; g++)
        {
            float gateDist = 0;
            for (int i = 0; i < gates[g].legs.Count; i++)
            {
                LegConfig config = gates[g].legs[i];

                if (Physics.Raycast(config.targetingTransform.position, -config.targetingTransform.up, out RaycastHit hit, config.leg.LegLength * 2, walkableLayers))
                {
                    config.targetPos = hit.point;
                    Debug.DrawLine(hit.point, hit.point + Vector3.up);
                }

                gateDist = Mathf.Max(gateDist, Vector3.Distance(config.footPosition.position, config.targetPos));
                Debug.DrawLine(config.targetPos, config.footPosition.position, gateDist > minStepDist ? Color.red : Color.white);

                gates[g].legs[i] = config;
            }
            gateDists.Add(gateDist);
        }


        

        curStepTime += Time.deltaTime;
        //only allow a step every stepTime seconds
        if (curStepTime > stepTime) 
        {

            //find the gate set with the largest distance that needs to be traveled
            float farthestDist = 0;
            int index = 0;
            for (int i = 0; i < gates.Count; i++)
            {
                if (gateDists[i] > farthestDist) 
                {
                    index = i;
                    farthestDist = gateDists[i];
                }
            }

            //only if the gate needs to move more than the min step dist do we start the step
            if (farthestDist > minStepDist) 
            {
                StartCoroutine(StepRoutine(gates[index], minStepDist / 2));
                curStepTime = 0;
            }
        }
    }

    private IEnumerator StepRoutine(Gate gate, float minStepDist) 
    {
        float curStepTime = 0;
        //get all our starting leg states
        List<Vector3> startPos = new List<Vector3>();
        List<bool> skipLeg = new List<bool>();
        for (int i = 0; i < gate.legs.Count; i++)
        {
            LegConfig config = gate.legs[i];
            startPos.Add(config.footPosition.position);
            skipLeg.Add(Vector3.Distance(config.footPosition.position, config.targetPos) < minStepDist);//skip the leg if it does not need to move the min dist
        }

        while (curStepTime < stepTime) 
        {
            curStepTime += Time.deltaTime;
            //move each leg to new target pos with a height offset from stepHeightCurve
            for (int i = 0; i < gate.legs.Count; i++)
            {
                if (skipLeg[i]) { continue; }
                LegConfig config = gate.legs[i];

                Vector3 lerpPos = Vector3.Lerp(startPos[i], config.targetPos, curStepTime / stepTime);
                lerpPos += new Vector3(0, stepHeightCurve.Evaluate(curStepTime / stepTime), 0);

                config.footPosition.position = lerpPos;
            }
            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var gate in gates) 
        {
            foreach (var leg in gate.legs) 
            {
                LegGizmo(leg);
            }
        }
    }

    private void LegGizmo(LegConfig config) 
    {
        if (!(config.leg && config.targetingTransform && config.footPosition)) { return; }
        Gizmos.DrawLine(config.targetingTransform.position, config.targetingTransform.position + (-config.targetingTransform.up * config.leg.LegLength * 2));
    }
}
