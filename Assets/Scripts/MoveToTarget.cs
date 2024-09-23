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

        StartCoroutine(Wait(0.5f));

        yLevel = transform.localPosition.y;

        foreach (Transform obj in checkpoints){
            obj.gameObject.SetActive(true);
        }

        nextCheckpoint = checkpoints[0];
        checkpoint = 0;

        RandomizePosition(obstaclePositions1, 1);
        RandomizePosition(obstaclePositions2, 2);
        RandomizePosition(obstaclePositions3, 3);
    }

    IEnumerator Wait(float delay){
        yield return new WaitForSeconds(delay);
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
        
        AddReward(-0.0001f);

        //Adds reward the closer they get to checkpoint
        if (distanceToCheckpoint > Vector3.Distance(transform.localPosition, nextCheckpoint.localPosition)){
            AddReward(0.01f);
        }
        distanceToCheckpoint = Vector3.Distance(transform.localPosition, nextCheckpoint.localPosition);

        if (yLevel > transform.localPosition.y) AddReward(-0.5f); 
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
            AddReward(50f);
            nextCheckpoint = checkpoints[++checkpoint];
            Debug.Log("Checkpoint reached!");
        }
        if (other.gameObject.CompareTag("FinalReward"))
        {
            other.gameObject.SetActive(false);
            AddReward(150f);
            Debug.Log("Final checkpoint reached!");
        }
        if (other.gameObject.CompareTag("Wall") || other.gameObject.CompareTag("obstacle"))
        {
            AddReward(-10);
            characterAnimationManager.characterController.enabled = false;
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Wall2"))
        {
            AddReward(-20);
            characterAnimationManager.characterController.enabled = false;
            EndEpisode();
        }
    }
}
