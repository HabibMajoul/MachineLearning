using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class MoveToTarget : Agent
{
    public CharacterAnimationManager characterAnimationManager;
    public float moveX;
    public float moveZ;
    [SerializeField] private List<Transform> checkpoints;
    private Transform nextCheckpoint;
    private int checkpoint;
    private float distanceToCheckpoint;
    private float yLevel;
    [SerializeField] private int ObstaclesPerFloor;
    [SerializeField] private List<Transform> obstacles = new List<Transform>();
    [SerializeField] private List<Transform> obstaclePositions1 = new List<Transform>();
    [SerializeField] private List<Transform> obstaclePositions2 = new List<Transform>();
    [SerializeField] private List<Transform> obstaclePositions3 = new List<Transform>();
    private float distanceRewardMultiplier = 1.0f;

    public void RandomizePosition(List<Transform> list, int floor){
        
        List<int> indexes = new List<int>();
        for (int i = 0; i < ObstaclesPerFloor; i++){
            int index;

            do {
                index = Random.Range(0, list.Count);
            } while (indexes.Contains(index));

            indexes.Add(index);

            obstacles[i + ObstaclesPerFloor*(floor-1)].position = list[index].position;
        }
    }

    public override void OnEpisodeBegin()
    {
        characterAnimationManager.OnResetEpisode();
        distanceRewardMultiplier = 1.0f;

        yLevel = -0.1854539f;

        foreach (Transform obj in checkpoints){
            obj.gameObject.SetActive(true);
        }

        nextCheckpoint = checkpoints[0];
        checkpoint = 0;

        RandomizePosition(obstaclePositions1, 1);
        RandomizePosition(obstaclePositions2, 2);
        RandomizePosition(obstaclePositions3, 3);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(distanceToCheckpoint);

        // sensor.AddObservation(nextCheckpoint.localPosition - transform.localPosition);
        
        //Recheck logic
        foreach (Transform checkpoint in checkpoints){
            sensor.AddObservation(checkpoint.localPosition);
        }

        foreach (Transform obstacle in obstacles){
            sensor.AddObservation(obstacle.transform.localPosition);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        moveX = actions.ContinuousActions[0];
        moveZ = actions.ContinuousActions[1];
        
        AddReward(-0.00000125f * distanceRewardMultiplier);

        //Adds reward the closer they get to checkpoint
        if (distanceToCheckpoint > Vector3.Distance(transform.localPosition, nextCheckpoint.localPosition)){
            AddReward(0.000005f * distanceRewardMultiplier);
        }
        else
        {
            AddReward(-0.00000125f * distanceRewardMultiplier);
        }
        distanceToCheckpoint = Vector3.Distance(transform.localPosition, nextCheckpoint.localPosition);

        if (yLevel > transform.localPosition.y)
        {
            AddReward(-0.2f);
        }
        if (yLevel < transform.localPosition.y)
        {
            AddReward(0.09f);
        }
        yLevel = transform.localPosition.y;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Reward"))
        {
            other.gameObject.SetActive(false);
            AddReward(1f + (10f * distanceRewardMultiplier));
            nextCheckpoint = checkpoints[++checkpoint];
            distanceRewardMultiplier *= 2;
            Debug.Log("Checkpoint " + (checkpoint - 1) + " reached!");
        }
        if (other.gameObject.CompareTag("FinalReward"))
        {
            other.gameObject.SetActive(false);
            AddReward(50f * distanceRewardMultiplier);
            Debug.Log("Final checkpoint reached!");
        }
        if (other.gameObject.CompareTag("Wall"))
        {
            AddReward(-20f);
            characterAnimationManager.characterController.enabled = false;
            EndEpisode();
        }
        if (other.gameObject.CompareTag("obstacle"))
        {
            AddReward(-10f);
            characterAnimationManager.characterController.enabled = false;
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Wall2"))
        {
            AddReward(-70);
            characterAnimationManager.characterController.enabled = false;
            EndEpisode();
        }
    }
}
