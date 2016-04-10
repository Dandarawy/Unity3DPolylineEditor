using UnityEngine;
using System.Collections;

public class PathMovement : MonoBehaviour {
    public float speed = 1;
    public Polyline path;
    int targetIndex = 1;
    Vector3 velocity;
    // Use this for initialization
	void Start () {
        transform.position = path.nodes[0];
        velocity = (path.nodes[targetIndex] - path.nodes[targetIndex - 1]).normalized * speed;
	}

    // Update is called once per frame
    bool pathFinished = false;
	void Update () {
        if (!pathFinished)
        {
            if ((transform.position - path.nodes[targetIndex]).magnitude < (velocity * Time.deltaTime).magnitude)
            {
                transform.position = path.nodes[targetIndex];
                targetIndex++;
                if (targetIndex == path.nodes.Count)
                {
                    pathFinished = true;
                    return;
                }
                velocity = (path.nodes[targetIndex] - path.nodes[targetIndex - 1]).normalized * speed;
            }
            else
                transform.position += velocity * Time.deltaTime;
        }
	}
}
