using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class HiddenMarkovModel : MonoBehaviour
{
    
    public List<Vector2Int> states; // A list of possible positions (x, y) on the grid.
    public List<Vector2Int> observations; // A list of possible observations.

    // Define the models
    public Dictionary<Vector2Int, Dictionary<Vector2Int, float>> transitionModel; // P(next_state | current_state)
    public Dictionary<Vector2Int, Dictionary<Vector2Int, float>> emissionModel; // P(observation | state)
    public Dictionary<Vector2Int, float> initialStateDistribution; // Initial belief

    // For visualization
    Dictionary<Vector2Int, float> smoothedBeliefs = new Dictionary<Vector2Int, float>();

    // Define the observation range
    public int observationRange = 10;
    public Vector2Int pacManPos = new Vector2Int();
    public Vector2Int ghostPos = new Vector2Int();

    GhostMovement ghostSript;
    PacManScript pacManScript;

    float time;
    public float updateSpeed = 1.0f;



    void Start()
    {

        ghostSript = GameObject.FindWithTag("Ghost").GetComponent<GhostMovement>();
        pacManScript = GameObject.FindWithTag("PacMan").GetComponent<PacManScript>();
        // Initialize the states, observations, models       
        InitializeStates();
        InitializeObservations();
        InitializeTransitionModel();
        InitializeEmissionModel();
        InitializeInitialStateDistribution();
      
    }

    void Update()
    {
        time+= Time.deltaTime;
        if (time > updateSpeed)
        {
            time = 0;
            PredictNextState(); 
        }
        UpdateObservations();
        updatePos();
        UpdateEmissionModel(pacManPos);

         foreach (var state in states)
        {
            float previousBelief = smoothedBeliefs.ContainsKey(state) ? smoothedBeliefs[state] : initialStateDistribution[state];
            smoothedBeliefs[state] = 0.9f * previousBelief + 0.1f * initialStateDistribution[state];
        }

    }
    void InitializeStates()
    {
        states = new List<Vector2Int>();

        // Loop over the game grid and add valid positions to states
        for (int y = 0; y < PlayfieldGenerator.height; y++)
        {
            for (int x = 0; x < PlayfieldGenerator.width; x++)
            {
                int cell = PlayfieldGenerator.Map[y, x];

                if (!(cell == 1))
                {                    
                    states.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    void InitializeObservations()
    {
        observations = new List<Vector2Int>();

        foreach (Vector2Int pos in states)
        {
            int distance = Mathf.Abs(pacManPos.x - pos.x) + Mathf.Abs(pacManPos.y - pos.y); // Manhattan distance

            if (distance <= observationRange)
            {
                observations.Add(pos);

            }
        }
    }

    void InitializeTransitionModel()
    {
        transitionModel = new Dictionary<Vector2Int, Dictionary<Vector2Int, float>>();

        foreach (Vector2Int currentState in states)
        {
            List<Vector2Int> neighbors = GetAccessibleNeighbors(currentState);

            Dictionary<Vector2Int, float> neighborProbabilities = new Dictionary<Vector2Int, float>();
            float probability = 1f / neighbors.Count;

            foreach (var neighbor in neighbors)
            {
                neighborProbabilities[neighbor] = probability;
            }

            transitionModel[currentState] = neighborProbabilities;
        }
    }

    List<Vector2Int> GetAccessibleNeighbors(Vector2Int state)
    {
        // Initialize the list to hold the accessible neighbors.
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // Define possible moves. For example, for a 4-connected grid:
        Vector2Int[] possibleMoves =
        {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(1, 0),  // Right
        new Vector2Int(0, -1), // Down
        new Vector2Int(-1, 0)  // Left
    };

        // Check each possible move.
        foreach (var move in possibleMoves)
        {
            Vector2Int neighbor = state + move;

            // If the neighbor is in the list of viable states, add it to the neighbors.
            if (states.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    void InitializeEmissionModel()
    {
        emissionModel = new Dictionary<Vector2Int, Dictionary<Vector2Int, float>>();

        foreach (var state in states)
        {
            var emissionProbabilities = new Dictionary<Vector2Int, float>();

            var possibleObservations = observations
                .Where(obs => IsWithinObservationRange(state, obs, pacManPos))
                .ToList();

            float probability = (possibleObservations.Count > 0) ? 1f / possibleObservations.Count : 0f;

            foreach (var observation in observations) // Note: Loop over all observations, not just possibleObservations
            {
                emissionProbabilities[observation] = possibleObservations.Contains(observation) ? probability : 0f;
            }

            emissionModel[state] = emissionProbabilities;
        }
    }

    bool IsWithinObservationRange(Vector2Int state, Vector2Int observation, Vector2Int pacManPos)
    {
        int distanceToState = Mathf.Abs(pacManPos.x - state.x) + Mathf.Abs(pacManPos.y - state.y);
        int distanceToObservation = Mathf.Abs(pacManPos.x - observation.x) + Mathf.Abs(pacManPos.y - observation.y);
   
        return distanceToObservation <= observationRange && distanceToObservation <= distanceToState;
    }

    void InitializeInitialStateDistribution()
    {
        initialStateDistribution = new Dictionary<Vector2Int, float>();

        float probability = 1f / states.Count;

        foreach (var state in states)
        {
            initialStateDistribution[state] = probability;
        }
    }

    void UpdateObservations()
    {
        pacManPos = pacManScript.GetPacManPosition(); // Update Pac-Man’s current position if it can change during the game
        UpdateObservationList(); // Update the observations list before checking where the ghost is

        Vector2Int observedPosition = new Vector2Int(-1, -1); // Representing no observation

        foreach (Vector2Int pos in observations)
        {
            if (IsGhostAtPosition(pos))
            {
               
                observedPosition = pos;
                break;
            }
        }

        UpdateBeliefState(observedPosition);
        
    }

    void UpdateObservationList()
    {
        observations.Clear(); // Clear the previous observations
        foreach (Vector2Int pos in states)
        {
            int distance = Mathf.Abs(pacManPos.x - pos.x) + Mathf.Abs(pacManPos.y - pos.y); // Manhattan distance

            if (distance <= observationRange)
            {
                observations.Add(pos);
                
            }
        }
    }


    void updatePos() 
    {
        pacManPos = pacManScript.pacManPosition;
        ghostPos = ghostSript.ghostPosition;
    }


    void UpdateBeliefState(Vector2Int observedPosition)
    {
        Dictionary<Vector2Int, float> newBeliefState = new Dictionary<Vector2Int, float>();

        foreach (var state in states)
        {
           float emissionProbability;
           if (!emissionModel[state].TryGetValue(observedPosition, out emissionProbability))
            {

                emissionProbability = 0f; // default value if observedPosition is not found in emissionModel[state]
            }

            float probability = initialStateDistribution[state] * emissionProbability;
            newBeliefState[state] = probability;

        }


        // Normalize the new belief state.
        float total = newBeliefState.Values.Sum();
        if (total == 0f) return; // Prevent division by zero

        foreach (var state in states)
        {

            newBeliefState[state] /= total;
    
        }

        // Update the initial state distribution with the new belief state.
        initialStateDistribution = new Dictionary<Vector2Int, float>(newBeliefState);
    }

    void PredictNextState()
    {
        Dictionary<Vector2Int, float> predictedState = new Dictionary<Vector2Int, float>();

        foreach (var state in states)
        {
            float totalProbability = 0;

            foreach (var previousState in states)
            {
                // Check if the 'previousState' and 'state' are in their respective dictionaries
                if (transitionModel.TryGetValue(previousState, out var transitions)
                   && transitions.TryGetValue(state, out var transitionProbability)
                   && initialStateDistribution.TryGetValue(previousState, out var initialProbability))
                {
                    totalProbability += initialProbability * transitionProbability;
                }
            }

            //Debug.Log("Total probability for state " + state + " is " + totalProbability);
            predictedState[state] = totalProbability;
        }

        float sum = predictedState.Values.Sum();
        foreach (var state in predictedState.Keys.ToList())
        {
            predictedState[state] /= sum;
        }

        // Update the initial state distribution with the predicted state.
        initialStateDistribution = new Dictionary<Vector2Int, float>(predictedState);
    }

    private bool IsGhostAtPosition(Vector2Int pos)
    {
        return ghostSript.ghostPosition == pos;
    }

    void UpdateEmissionModel(Vector2Int pacManPos)
    {
        emissionModel = new Dictionary<Vector2Int, Dictionary<Vector2Int, float>>();

        foreach (var state in states)
        {
            var emissionProbabilities = new Dictionary<Vector2Int, float>();

            // Check if the ghost is visible to Pac-Man.
            bool ghostIsVisible = IsGhostVisibleToPacMan();


            foreach (var observation in observations)
            {
                if (ghostIsVisible && state == ghostPos)
                {
                    emissionProbabilities[observation] = (observation == ghostPos) ? 1f : 0f;
                }
                else
                {
                    // If ghost is not visible, assign equal probability to all states
                    emissionProbabilities[observation] = 1f / observations.Count;
                }
            }


            emissionModel[state] = emissionProbabilities;

           
        }
    }

    bool IsGhostVisibleToPacMan()
    {

        foreach (Vector2Int pos in observations)
        {
            if (IsGhostAtPosition(pos))
            {

                return true;
              
            }
        }

        return false;
        
    }
    void OnDrawGizmos()
    {
        if (smoothedBeliefs != null && smoothedBeliefs.Any())
        {
            float maxProbability = smoothedBeliefs.Values.Max();
            float minProbability = smoothedBeliefs.Values.Min();

            foreach (var state in states)
            {
                float probability = smoothedBeliefs[state];

                float range = maxProbability - minProbability;
                float adjustedProb = (probability - minProbability) / range;

                // Define your color thresholds here
                if (probability == maxProbability)
                {
                    Gizmos.color = Color.magenta;
                }
                else if (adjustedProb >= 0.75f)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.50f);
                }
                else if (adjustedProb >= 0.25f)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.50f);
                }
                else
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
                }

                // Draw a cube at the state position with the determined color
                // Adjust the Vector3 argument to set the position and the second argument to set the size of the cube
                Gizmos.DrawCube(new Vector3(state.x + 0.5f, -state.y - 0.5f, 0), new Vector3(0.5f, 0.5f, 0.5f));
            }
        }
    }







}
