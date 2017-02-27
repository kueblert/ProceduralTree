using UnityEngine;

[System.Serializable]
public class TreeIterationParam : MonoBehaviour
{
    // unique number of this iteration (auto-incrementing)
    public int iterationCounter = -1;


    // Branch rotation   
    public enum RotationType { TWIST } // , FREE }
    public RotationType BranchRotation;
    // FREE parameters

    /*public class TwistFunction       //TODO not serializable or such a thing
    {
        public AnimationCurve curve;
    }
    public TwistFunction twistFunction; 
    */
    // TWIST parameters
    public int twistHop = 10;
    public int alpha0 = 4;       // in degree
    public int alpha1 = -4;

    // Endpoint selection method to be used and its parameters
    public enum PlacementType { VOLUME, HOPS}
    public PlacementType endpointMethod = PlacementType.HOPS;
    public int nBranches = 3; // how many endpoints to select
    
    // HOPS parameters
    public int nHops = 5;
    // VOLUME parameters
    public GameObject volume = null;


    public TreeIterationParam()
    {
        iterationCounter = -1;


        endpointMethod = PlacementType.HOPS;
        nBranches = 3;
        nHops = 5;
        volume = null;

    }

    // Update is called once per frame
    void Update () {
	
	}
}
