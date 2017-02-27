using UnityEngine;
using UnityEngine.Assertions;

public class GuidingVectorTree : MonoBehaviour
{

    public GameObject stem;

    // sampling
    public Vector3 size = new Vector3(10, 10, 10);
    [Range(0.0f, 1.0f)]
    public float minDist = 0.5f;
    [Range(1, 20000)]
    public int nSamples = 1000;
    public YaoGraph.ConnectionType connectivity;

    // Meshing
    public int ElementsPerRing = 9;
    // Growing
    public int nIterations = 3;



    void OnEnable()
    {
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            // Initialization
            GridJitterSampling sampling = new GridJitterSampling(getStemTop(), size, minDist, nSamples);
            YaoGraph yao = new YaoGraph(sampling.data, connectivity);


            // Perform selection iterations
            TreeIterationParam[] iterations = gameObject.GetComponents<TreeIterationParam>();
            Assert.IsTrue(iterations.Length == nIterations);
            foreach (TreeIterationParam iteration in iterations)
            {
                yao.prepareNextIteration();

                // Dijkstra
                Dijkstra paths = new Dijkstra(yao, getStemTop());

                // Endpoint placement
                EndPointSelection.select(yao, iteration);
            }

            // Visualize

            sampling.visualize();
            //sampling.data.visualize();

            //yao.visualize();
            //paths.visualize();

            //EndPointSelection.visualize(yao);

            /*
            Meshifier meshGenerator = new Meshifier(yao, ElementsPerRing);
            // add Mesh Filter and Mesh Renderer
            MeshFilter mf = gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();

            mf.mesh = meshGenerator.generatedMesh;
            */

        }
    }

    private Vector3 getStemTop()
    {
        return stem.transform.position + new Vector3(0, stem.GetComponent<CapsuleCollider>().height / 2, 0);
    }
}
