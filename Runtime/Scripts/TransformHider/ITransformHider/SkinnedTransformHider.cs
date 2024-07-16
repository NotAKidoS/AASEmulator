using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class SkinnedTransformHider : ITransformHider
{
    private static readonly int s_Pos = Shader.PropertyToID("pos");
    private static readonly int s_BufferLayout = Shader.PropertyToID("bufferLayout");
    private static readonly int s_WeightedCount = Shader.PropertyToID("weightedCount");
    private static readonly int s_WeightedVertices = Shader.PropertyToID("weightedVertices");
    private static readonly int s_WeightedWeights = Shader.PropertyToID("weightedWeights");
    private static readonly int s_OriginalPositions = Shader.PropertyToID("originalPositions");
    private static readonly int s_VertexBuffer = Shader.PropertyToID("VertexBuffer");
    
    // mesh & bone
    private readonly SkinnedMeshRenderer _mainMesh;
    private readonly Transform _rootBone;
    
    // main hider stuff
    private GraphicsBuffer _graphicsBuffer;
    private readonly int _bufferLayout;
    
    // subtasks
    private readonly List<SubTask> _subTasks = new();
    internal SubTask AddSubTask(FPRExclusionWrapper exclusion)
    {
        SubTask subTask = new(this, exclusion);
        _subTasks.Add(subTask);
        return subTask;
    }
    
    #region ITransformHider Methods
    
    public bool IsActive { get; set; } = true; // default hide, but FPRExclusion can override
    
    // anything player can touch is suspect to death
    public bool IsValid => _mainMesh != null && _rootBone != null;

    private bool _isHidden;
    public bool IsHidden => _isHidden || !_mainMesh.enabled || !_mainMesh.gameObject.activeInHierarchy;
    
    public SkinnedTransformHider(SkinnedMeshRenderer renderer)
    {
        _mainMesh = renderer;
        _rootBone = _mainMesh.rootBone;
        _rootBone ??= _mainMesh.transform; // fallback to transform if no root bone
        
        Mesh mesh = _mainMesh.sharedMesh;
        if (mesh.HasVertexAttribute(VertexAttribute.Position)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Normal)) _bufferLayout += 3;
        if (mesh.HasVertexAttribute(VertexAttribute.Tangent)) _bufferLayout += 4;
        // ComputeShader is doing bitshift so we dont need to multiply by 4
        
        _mainMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        _mainMesh.forceMatrixRecalculationPerRender = false;
    }

    public void HideTransform()
    {
        _graphicsBuffer = _mainMesh.GetVertexBuffer();
        if (_graphicsBuffer == null) return; // can occur in editor
        
        _isHidden = true;
        
        foreach (SubTask subTask in _subTasks)
            if (subTask.IsActive && subTask.IsValid)
                subTask.Dispatch(0);

        //_graphicsBuffer.Release();
    }

    public void ShowTransform()
    {
        _isHidden = false;
        
        //_graphicsBuffer = _mainMesh.GetVertexBuffer();
        if (_graphicsBuffer == null) return; // can occur in editor
        
        foreach (SubTask subTask in _subTasks)
            if (subTask.IsActive && subTask.IsValid)
                subTask.Dispatch(1);

        _graphicsBuffer.Release();
    }

    public void Dispose()
    {
        foreach (SubTask subTask in _subTasks)
            subTask.Dispose();
        
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
    }
    
    #endregion ITransformHider Methods

    #region Sub Task Class

    internal class SubTask : IFPRExclusionTask
    {
        public bool IsActive { get; set; } = true;
        public bool IsValid => _computeBuffer != null; // TODO: cleanup dead tasks
        public bool ShrinkToZero { get; set; } = true;
        
        private readonly SkinnedTransformHider _parent;
        private readonly Transform _shrinkBone;
        private readonly int _vertexCount;
        private readonly ComputeBuffer _computeBuffer;
        private readonly ComputeBuffer _weightBuffer;
        private readonly ComputeBuffer _originalPosBuffer;
        private readonly int _threadGroups;
        
        public SubTask(SkinnedTransformHider parent, FPRExclusionWrapper exclusion)
        {
            _parent = parent;
            _shrinkBone = exclusion.target;
        
            var exclusionVerts = exclusion.affectedVertexIndices;
            var exclusionWeights = exclusion.affectedVertexWeights;
            
            _vertexCount = exclusionVerts.Count;
            
            _computeBuffer = new ComputeBuffer(_vertexCount, sizeof(int));
            _computeBuffer.SetData(exclusionVerts.ToArray());
            
            _weightBuffer = new ComputeBuffer(_vertexCount, sizeof(float));
            _weightBuffer.SetData(exclusionWeights.ToArray());
            
            _originalPosBuffer = new ComputeBuffer(_vertexCount, sizeof(float) * 3);
            
            const float xThreadGroups = 64f;
            _threadGroups = Mathf.CeilToInt(_vertexCount / xThreadGroups);
        }

        public void Dispatch(int kernel = 0)
        {
            Vector3 pos = ShrinkToZero 
                ? _parent._rootBone.transform.InverseTransformPoint(_shrinkBone.position) * _parent._rootBone.lossyScale.y
                : Vector3.positiveInfinity;

            try
            {
                TransformHiderUtils.shader.SetVector(s_Pos, pos);
                TransformHiderUtils.shader.SetInt(s_WeightedCount, _vertexCount);
                TransformHiderUtils.shader.SetInt(s_BufferLayout, _parent._bufferLayout);
                TransformHiderUtils.shader.SetBuffer(kernel, s_WeightedVertices, _computeBuffer);
                TransformHiderUtils.shader.SetBuffer(kernel, s_WeightedWeights, _weightBuffer);
                TransformHiderUtils.shader.SetBuffer(kernel, s_OriginalPositions, _originalPosBuffer);
                TransformHiderUtils.shader.SetBuffer(kernel, s_VertexBuffer, _parent._graphicsBuffer);
                TransformHiderUtils.shader.Dispatch(kernel, _threadGroups, 1, 1);
            }
            catch (Exception e)
            {
                Debug.LogError($"Dispatch Error: {e.Message}");
                Debug.LogError($"Parent: {_parent._mainMesh.name}", _parent._mainMesh);
            }
        }
        
        public void Dispose()
        {
            _computeBuffer?.Dispose();
            _weightBuffer?.Dispose();
            _originalPosBuffer?.Dispose();
        }

        #region Private Methods
        
        public static void FindExclusionVertList(SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusionWrapper> exclusions)
        {
            // Start the stopwatch
            //Stopwatch stopwatch = new();
            //stopwatch.Start();

            var boneWeights = renderer.sharedMesh.boneWeights;
            var bones = renderer.bones;
            int boneCount = bones.Length;
            bool[] boneHasExclusion = new bool[boneCount];

            // Populate the weights array
            for (int i = 0; i < boneCount; i++)
            {
                Transform bone = bones[i];
                if (bone == null) continue;
                if (exclusions.ContainsKey(bone))
                    boneHasExclusion[i] = true;
            }

            // nan'd bones have no consideration for weight
            const float minWeightThreshold = float.Epsilon;

            // Check bone weights and add vertex to exclusion list if needed
            for (int i = 0; i < boneWeights.Length; i++)
            {
                BoneWeight weight = boneWeights[i];
                Transform bone = null;
                float boneWeight = 0f;

                if (boneHasExclusion[weight.boneIndex3] && weight.weight3 > minWeightThreshold)
                {
                    boneWeight += weight.weight3;
                    bone = bones[weight.boneIndex3];
                }
                if (boneHasExclusion[weight.boneIndex2] && weight.weight2 > minWeightThreshold)
                {
                    boneWeight += weight.weight2;
                    bone = bones[weight.boneIndex2];
                }
                if (boneHasExclusion[weight.boneIndex1] && weight.weight1 > minWeightThreshold)
                {
                    boneWeight += weight.weight1;
                    bone = bones[weight.boneIndex1];
                }
                if (boneHasExclusion[weight.boneIndex0] && weight.weight0 > minWeightThreshold)
                {
                    boneWeight += weight.weight0;
                    bone = bones[weight.boneIndex0];
                }

                if (bone == null || !exclusions.TryGetValue(bone, out FPRExclusionWrapper exclusion)) 
                    continue;
                
                exclusion.affectedVertexIndices.Add(i);
                exclusion.affectedVertexWeights.Add(Mathf.Clamp01(boneWeight));
            }

            // Stop the stopwatch
            //stopwatch.Stop();

            // Log the execution time
            //Debug.Log($"FindExclusionVertList execution time: {stopwatch.ElapsedMilliseconds} ms");
        }
        
        #endregion Private Methods
    }
    
    #endregion Sub Task Class
}