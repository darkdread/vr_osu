using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public struct Node {
    public Vector3 position;
}

[ExecuteInEditMode]
public class NodeCreator : MonoBehaviour
{
	public float xGrid = 1f;
	public float yGrid = 1f;
	public float zGrid = 1f;

    public ComputeShader computeShader;

    public Vector3 center = new Vector3(0f, 0f, 0f);
    public int iterations = 10;
    public LayerMask obstacleMask;

    [HideInInspector]
    public bool debugOutline = false;

    Node[] nodes;

    public void GenerateWithComputeShaderSingleThreaded(){
        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node[] data = new Node[iterations * iterations * iterations];
        ComputeBuffer nodesBuffer = new ComputeBuffer(data.Length, 3 * 4);
        nodesBuffer.SetData(data);

        int kernelId = computeShader.FindKernel("ayylmao");

        computeShader.SetInt("iterations", iterations);
        computeShader.SetFloats("gridSize", new float[3]{xGrid, yGrid, zGrid});
        computeShader.SetBuffer(kernelId, "dataBuffer", nodesBuffer);
        computeShader.Dispatch(kernelId, iterations, iterations, iterations);

        Node[] output = new Node[data.Length];
        nodesBuffer.GetData(output);

        // foreach(Node n in output){
        //     print(n.position);
        // }

        nodes = output;

        nodesBuffer.Release();

        sw.Stop();
        print("Single Thread: " + sw.ElapsedMilliseconds);
    }

    public void GenerateWithComputeShaderMultiThreaded(){
        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node[] data = new Node[iterations * iterations * iterations];
        ComputeBuffer nodesBuffer = new ComputeBuffer(data.Length, 3 * 4);
        nodesBuffer.SetData(data);

        int kernelId = computeShader.FindKernel("ayylmao2");

        computeShader.SetInt("iterations", iterations);
        computeShader.SetBuffer(kernelId, "dataBuffer", nodesBuffer);
        computeShader.Dispatch(kernelId, iterations/4, iterations/4, iterations/4);

        Node[] output = new Node[data.Length];
        nodesBuffer.GetData(output);

        // foreach(Node n in output){
        //     print(n.position);
        // }

        nodes = output;

        nodesBuffer.Release();
        sw.Stop();
        print("Multi Thread: " + sw.ElapsedMilliseconds);
    }

    public void Generate(){

        // nodes.Clear();

        // if (iterations % 2 != 0){
        //     iterations += 1;
        // }

		for (int x = -iterations / 2 + 1; x < iterations/2; x++) {
			for (int y = -iterations / 2 + 1; y < iterations/2; y++) {
				for (int z = -iterations / 2 + 1; z < iterations/2; z++) {
					Vector3 pos = new Vector3(x * xGrid, y * yGrid, z * zGrid) + center;

                    RaycastHit hit;
					bool hitCollider = Physics.Raycast(pos, Vector3.up, out hit, 1000f);
                    // UnityEngine.Debug.DrawRay(pos, Vector3.up * 1000, Color.black, 1f);

                    if (hitCollider){
                        int objectLayer = hit.collider.gameObject.layer;
                        if (objectLayer == LayerMask.NameToLayer("Ground") || objectLayer == LayerMask.NameToLayer("Wall")){
                            continue;
                        }
                    }

                    Node newNode = new Node(){
                        position = pos
                    };

                    // nodes.Add(newNode);
				}
			}
		}
    }

    public void DebugOutline(){
		debugOutline = !debugOutline;
    }

	public void OnDrawGizmos() {
        if (!debugOutline){
            return;
        }

        Vector3 offset = new Vector3(xGrid * iterations/2, yGrid * iterations/2, zGrid * iterations/2);
        Gizmos.DrawWireCube(center + offset, new Vector3(xGrid * iterations, yGrid * iterations, zGrid * iterations));

        foreach(Node n in nodes){
            // Gizmos.DrawSphere(n.position, 0.2f);
            Gizmos.DrawLine(n.position, n.position + Vector3.up);
        }
        
		// for (int x = 0; x < 2; x++) {
		// 	for (int y = 0; y < 2; y++) {
		// 		for (int z = 0; z < 2; z++) {
		// 			int signX = x == 0 ? -1 : 1;
		// 			int signY = y == 0 ? -1 : 1;
		// 			int signZ = z == 0 ? -1 : 1;

		// 			Vector3 start = new Vector3(signX * iterations, signY * iterations, signZ * iterations) + center;
		// 			Vector3 end = center;
		// 			Gizmos.DrawLine(start, end);

        //             print(string.Format("x{0}, y{1}, z{2}", x, y, z));
		// 		}
		// 	}
		// }

	}
}
