﻿using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace TKPackages.TKPhysics.Runtime
{

    [DisallowMultipleComponent]
    public class ADBRuntimeController : MonoBehaviour, IADBPhysicMonoComponent
    {
        public MonoBehaviour Target => this;

        public const int MAXCOLLIDERCOUNT = 1024;

        public bool isDebug = false;

        public float bufferTime=1f;
        public int iteration=4;
        public float windForceScale=0f;
        public bool isDrawGizmo = false;
        public bool isOptimize = false;
       

        public ColliderCollisionType colliderCollisionType = ColliderCollisionType.Constraint;

        public float timeScale = 1;
        public bool isRunAsync = false;
        public bool isParallel=false;

        public List<ADBColliderReader> overlapsColliderList;
        public Bounds OverlapBox;


        public UpdateMode updateMode=UpdateMode.FixedUpdate;
        public ADBChainProcessor[] allChain;
        private ADBPhysicsKernel ADBkernel;
        private bool isInitialize = false;

        private float deltaTime=0;
        private float startVelocityDamp = 0;
        private float scale;
        private bool isResetPoint;
        private Vector3 addForce;
        private Collider[] colliders;
        private List<ColliderRead> tempColliderReads;


        public float colliderSize=1;

     

        private void Start()
        {
            if (!isDebug)
            {
                isDrawGizmo = false;
            }
            if (!ParentCheck())
            {
                Destroy(this);
                return;
            }
            Initialize();

            if (allChain == null && allChain.Length == 0)
            {
                Debug.Log(transform.name+" No Chain Data Found");
                return;
            }

            ADBkernel = new ADBPhysicsKernel();
            if (Application.isPlaying&& allChain.Length!=0)
            {
                for (int i = 0; i < allChain.Length; i++)
                {
                    allChain[i].SetData( ADBkernel);
                }
                ADBkernel.SetNativeArray();
                isInitialize = true;
                bufferTime = bufferTime < 0.017f ? 0.017f : bufferTime;
                isResetPoint = true;
            }
        }

        private void Initialize()
        {
            var tempScale = transform.localScale;
            transform.localScale = transform.localScale / (float3)transform.lossyScale;
            ListCheck();
            InitializeChain();

            transform.localScale = tempScale;
        }
        public void RefreshSize()
        {
            for (int i = 0; i < overlapsColliderList?.Count; i++)
            {
                overlapsColliderList[i].Resize( colliderSize);
            }
        }

        private void Update()
        {
            if (updateMode==UpdateMode.Update)
            {
                Run(Time.deltaTime* timeScale);
            }
        }
        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                Run(Time.fixedDeltaTime* timeScale);
            }

        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
            {
                Run(Time.unscaledDeltaTime* timeScale);
            }
        }
        private void OnDisable()
        {
            if (ADBkernel != null)
            {
                RestoreRuntimePoint();
            }
        }
        private void OnDestroy()
        {
            if (ADBkernel != null)
            {
                RestoreRuntimePoint();
                ADBkernel.Dispose();
            }

        }

        private void Run(float inputDeltaTime)
        {
            if (!isInitialize) return;

            if (isResetPoint)
            {
                isResetPoint = false;
                RestoreRuntimePoint();
                addForce+= UnityEngine.Random.insideUnitSphere * 1e-6f;
                startVelocityDamp = 0;
                return;
            }
            deltaTime =Mathf.Min(0.02f, Mathf.Lerp(deltaTime, inputDeltaTime, 1 / (bufferTime * 60)));
            startVelocityDamp =math.saturate (startVelocityDamp+ inputDeltaTime / bufferTime);


            scale =math.cmax((float3) transform.lossyScale);
            scale = math.max(scale, math.EPSILON);
            addForce += ADBWindZone.getaddForceForce(transform.position) * windForceScale * deltaTime;
            UpdateOverlapsCollider();
            UpdateDataPakage();

        }
        private void UpdateDataPakage()
        {
            bool isSuccessfulRun = ADBkernel.Schedule(deltaTime,
                                                              scale,
                                                              ref iteration,
                                                              addForce,
                                                              colliderCollisionType,
                                                              isOptimize,
                                                              isRunAsync,
                                                              isParallel,
                                                              startVelocityDamp
                                                              );

            if (isSuccessfulRun)
            {
                addForce = Vector3.zero;
            }
        }

        public void UpdateOverlapsCollider()
        {
            ClacBounds();
            if (colliders==null||colliders.Length != MAXCOLLIDERCOUNT)
            {
                colliders = new Collider[MAXCOLLIDERCOUNT];
            }

            if (colliderCollisionType==ColliderCollisionType.Null)
            { return; }

            Vector3 center =  OverlapBox.center;
            Vector3 halfExtent = OverlapBox.extents* scale;
            int count = Physics.OverlapBoxNonAlloc(
                               center,
                                halfExtent,
                                colliders, Quaternion.identity, int.MaxValue, QueryTriggerInteraction.Collide
                                );
            if (count==0)
            {
                return;
            }
            overlapsColliderList.Clear();
            tempColliderReads.Clear();
            for (int i = 0; i < count; i++)
            {
                if (ADBColliderReader.ColliderTokenDic.TryGetValue(colliders[i].GetInstanceID(),out ADBColliderReader colliderToken))
                {
                    tempColliderReads.Add( colliderToken.runtimeCollider.colliderRead);
                    overlapsColliderList.Add(colliderToken);
                }
            }

            if (isInitialize)
            {
                ADBkernel.SetRuntimeCollider(tempColliderReads.ToArray());
            }
            
        }
        public void ResetData()
        {
            RestoreRuntimePoint();
            Start();

        }
        public int GetPointCount()
        {
            int count = 0;
            for (int i = 0; i < allChain?.Length; i++)
            {
                count += allChain[i].allPointList.Count;
            }
            return count;
        }

        public void SetPhysicData(ADBChainProcessor[] jointAndPointControlls)
        {
            this.allChain = jointAndPointControlls;
        }
        public void RestoreRuntimePoint()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("Use it On Runtime!");
                return;
            }
            scale = math.cmax((float3)transform.lossyScale);
            scale = math.max(scale, math.EPSILON);

            ADBkernel.restorePoint(scale);
        }
        public bool ParentCheck()
        {
            var parentRuntimeController = transform.parent?.GetComponentInParent<ADBRuntimeController>();
            if (parentRuntimeController != null)
            {
                Debug.LogError(transform.name + " find the parent has  ADB Runtime Controller in" + parentRuntimeController.transform.name + "check it ");

                return false;
            }
            return true;
        }
        public bool PointCheck()
        {
            return allChain?.Length > 0;
        }
        public void ListCheck()
        {

            if (overlapsColliderList == null)
            {
                overlapsColliderList = new List<ADBColliderReader>();
            }
            if (tempColliderReads==null)
            {
                tempColliderReads = new List<ColliderRead>(); 
            }
            allChain = gameObject.GetComponentsInChildren<ADBChainProcessor>();
            overlapsColliderList.Clear();
            overlapsColliderList.AddRange(gameObject.GetComponentsInChildren<ADBColliderReader>());
        }
        public void InitializeChain()
        {
            for (int i = 0; i < allChain?.Length; i++)
            {
                allChain[i].Initialize();
            }

            if (allChain == null)
            {
                Debug.Log( "no point found , check the white key word");
            }
        }
        private void ClacBounds()
        {
            OverlapBox = allChain[0].GetCurrentRangeBounds();
            for (int i = 1; i < allChain.Length; i++)
            {
                OverlapBox.Encapsulate(allChain[i].GetCurrentRangeBounds());
            }

        }
        public void AddForce(Vector3 force)
        {
            addForce += force;
        }
        public bool GetConstraintByKey(string key, ConstraintType constraintType, out ADBRuntimeConstraint[] returnConstraint)
        {
            List<ADBRuntimeConstraint> constraints = new List<ADBRuntimeConstraint>();
            bool isFind = false;
            for (int i = 0; i < allChain.Length; i++)
            {
                if (allChain[i].keyWord == key)
                {
                    constraints.AddRange( allChain[i].GetConstraint(constraintType));
                    isFind = true;
                }
            }
            returnConstraint = constraints.ToArray();
            return isFind;
        }

        private void OnDrawGizmos()
        {
            if (!isDrawGizmo && isActiveAndEnabled) return;

            for (int i = 0; i < allChain?.Length; i++)
            {
                allChain[i].DrawGizmos(colliderCollisionType);
            }

            Gizmos.color = Color.white;
            Vector3 center = OverlapBox.center ;
            Vector3 halfExtent = OverlapBox.extents * scale;
            Gizmos.DrawWireCube(center, halfExtent*2);
            if (Application.isPlaying)
            {
                ADBkernel.DrawGizmos();
            }
        }
    }
    public enum ColliderCollisionType
    {
        Both = 1,
        Constraint = 2,
        Point = 3,
        Null = 4
    }

    public enum UpdateMode
    {
        Update = 1,
        FixedUpdate = 2,
        LateUpdate = 3,
    }
}