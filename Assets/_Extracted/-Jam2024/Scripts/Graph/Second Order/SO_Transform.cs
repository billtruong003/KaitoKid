using System;
using UnityEngine;
using Sora_Ults;
using Unity.Collections;
using Sirenix.OdinInspector;

public class SO_Transform : MonoBehaviour
{
    public SelectedAlgorithm ASelector;


    [FoldoutGroup("Position")] 
    public bool PositionToggle = true;
    [FoldoutGroup("Position")]
    [SerializeField][Range(0,10)] public float PosFrequency = 1f;
    [FoldoutGroup("Position")]
    [SerializeField][Range(0,1)] public float PosDamping = 1f;
    [FoldoutGroup("Position")]
    [SerializeField] public float PosResponsiveness = 0.2f;
    [FoldoutGroup("Position")]
    [SerializeField] public float PosDeltaTimeScale = 17f;
    
    [Space(5)]
    [FoldoutGroup("Rotation")]
    public bool RotationToggle = true;
    [FoldoutGroup("Rotation")]
    [SerializeField][Range(0,10)] public float RotFrequency = 1f;
    [FoldoutGroup("Rotation")]
    [SerializeField][Range(0,1)] public float RotDamping = 1f;
    [FoldoutGroup("Rotation")]
    [SerializeField] public float RotResponsiveness = 0.2f;
    [FoldoutGroup("Rotation")]
    [SerializeField] public float RotDeltaTimeScale = 17f;
    
    [Space(5)]
    [FoldoutGroup("Scale")]
    public bool ScaleToggle = true;
    [FoldoutGroup("Scale")]
    [SerializeField][Range(0,10)] public float ScaleFrequency = 1f;
    [FoldoutGroup("Scale")]
    [SerializeField][Range(0,1)] public float ScaleDamping = 1f;
    [FoldoutGroup("Scale")]
    [SerializeField] public float ScaleResponsiveness = 0.2f;
    [FoldoutGroup("Scale")]
    [SerializeField] public float ScaleDeltaTimeScale = 17f;
    
    [Space(10)]
    [SerializeField] private Transform target;
    [Space(10)]
    //public GraphVisualize GV;
    private SO_Position_Handler positionHandler = new SO_Position_Handler();
    private SO_Quaternion_Handler rotationHandler = new SO_Quaternion_Handler();
    private SO_Scale_Handler scaleHandler = new SO_Scale_Handler();
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private void Awake()
    {
        //GV = this.gameObject.GetComponent<GraphVisualize>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        positionHandler.Initialize(PosFrequency, PosDamping, PosResponsiveness, PosDeltaTimeScale, initialPosition, ASelector,target.transform.position);
        rotationHandler.Initialize(RotFrequency, RotDamping, RotResponsiveness, RotDeltaTimeScale, initialRotation, ASelector, target.transform.rotation);
        scaleHandler.Initialize(ScaleFrequency, ScaleDamping, ScaleResponsiveness, ScaleDeltaTimeScale, initialScale, ASelector,target.transform.localScale);
    }

    private void OnValidate()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        
        positionHandler.Initialize(PosFrequency, PosDamping, PosResponsiveness, PosDeltaTimeScale, initialPosition, ASelector, target.transform.position);
        rotationHandler.Initialize(RotFrequency, RotDamping, RotResponsiveness, RotDeltaTimeScale, initialRotation, ASelector, target.transform.rotation);
        scaleHandler.Initialize(ScaleFrequency, ScaleDamping, ScaleResponsiveness, ScaleDeltaTimeScale, initialScale, ASelector,target.transform.localScale);
        //GV.UpdateOnGUI_Pos(PosFrequency, PosDamping, PosResponsiveness, PosDeltaTimeScale * 0.015f, ASelector, PositionToggle);
        //GV.UpdateOnGUI_Rot(RotFrequency, RotDamping, RotResponsiveness, RotDeltaTimeScale  * 0.015f, ASelector, RotationToggle);
        //GV.UpdateOnGUI_Scale(ScaleFrequency, ScaleDamping, ScaleResponsiveness, ScaleDeltaTimeScale * 0.015f, ASelector, ScaleToggle);

    }

    private void LateUpdate()
    {
        if(PositionToggle) transform.position = positionHandler.UpdatePosition(target.position);
        if(RotationToggle) transform.rotation = rotationHandler.UpdateRotation(target.rotation);
        if(ScaleToggle) transform.localScale = scaleHandler.UpdateScale(target.localScale);
    }
    
    
}