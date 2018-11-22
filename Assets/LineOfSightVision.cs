using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class LineOfSightVision : MonoBehaviour
{
    
    // Amount of directions within FOV to check 
    public int Resolution = 1024;
    public bool DrawDebugLines = false;
    public float Range = 25;
    public float FieldOfView = 90;
    public float DegreeOffset = 90;
    public float MaxAngle = 60;
    private MeshFilter LOSMeshFilter;

    public Material material;
    
    void Start() {        
        LOSMeshFilter = GetComponent<MeshFilter>();
        GetComponent<MeshRenderer>().material = material ? material : new Material(Shader.Find("Standard"));        
    }

    void Update() {

        List<Vector3>[] pointLines = new List<Vector3>[Resolution + 1];

        for(int i = 0; i <= Resolution; i++) {
            float degrees = (((float)i / Resolution) * FieldOfView);
            degrees -= FieldOfView / 2;
            degrees -= transform.parent.rotation.eulerAngles.y;
            degrees += DegreeOffset;

            pointLines[i] = GetPointLine(transform.position, RadiansToVector3(degrees * Mathf.Deg2Rad), Range);
        }

            if (DrawDebugLines) {
            for (int i = 0; i < pointLines.Length; i++) {
                Debug.DrawLine(transform.position, pointLines[i][0], Color.red, 0);
                for(int j = 0; j < pointLines[i].Count - 1; j++) {
                    Debug.DrawLine(pointLines[i][j], pointLines[i][j + 1], Color.red, 0);
                }                
            }
        }
        LOSMeshFilter.mesh = CreateMeshFromPointLines(pointLines);     
    }

    List<Vector3> GetPointLine(Vector3 origin, Vector3 direction, float maxDist) {
        List<Vector3> pointLine = new List<Vector3>();

        float travelledDist = 0;
        
        // Loop until cant travel more or angle is too large
        while(travelledDist < maxDist && Vector3.Angle(direction, new Vector3(direction.x, 0, direction.z)) < MaxAngle) {
            RaycastHit hit;
            if(Physics.Raycast(origin, direction, out hit, maxDist-travelledDist)) { // If a surface was hit
                pointLine.Add(hit.point);
                travelledDist += hit.distance;
                origin = hit.point;
                Vector3 newDir = DoubleCross(direction, hit.normal);    
                if(Vector3.Angle(newDir, direction) > MaxAngle) {
                    break;
                } else {
                    direction = newDir;
                }
            } else {
                pointLine.Add(origin + (direction.normalized) * (maxDist - travelledDist));
                break;
            }
        }

        return pointLine;
    }

    Vector3 DoubleCross(Vector3 u, Vector3 n) {
        // v = Cross u x n
        Vector3 v = Vector3.Cross(u, n);
        // d = Cross n x v
        Vector3 d = Vector3.Cross(n, v);
        return d;
    }

    Vector3[] SimpleGetPoints() {
        Vector3[] points = new Vector3[Resolution + 2];
        transform.rotation = Quaternion.identity;
        for(int i = 1; i <= Resolution + 1; i++) {

            RaycastHit hitInfo;
            float degrees = (((float)(i - 1) / Resolution) * FieldOfView);
            degrees -= FieldOfView / 2;
            degrees -= transform.parent.rotation.eulerAngles.y;
            degrees += DegreeOffset;


            if(Physics.Raycast(new Ray(transform.position, RadiansToVector3(degrees * Mathf.Deg2Rad)), out hitInfo, Range)) {
                points[i] = hitInfo.point - transform.position;
            } else {
                points[i] = RadiansToVector3(degrees * Mathf.Deg2Rad).normalized * Range;
            }
        }
        return points;
    }

    /// <summary>
    /// Gives a point within unit sphere with angle rads
    /// </summary>
    Vector3 RadiansToVector3(float rads) {
        return new Vector3(Mathf.Cos(rads), 0, Mathf.Sin(rads));
    }

    Mesh CreateMeshFromPointLines(List<Vector3>[] pointLines) {
        Mesh mesh = new Mesh();
        mesh.name = "LOSMesh";

        return mesh;
    }

    Mesh CreateMeshFromPoints(Vector3[] points) {
        Mesh m = new Mesh();
        m.name = "LOSMesh";

        points[0] = Vector3.zero;
        m.vertices = points;

        int[] trianglesArray = new int[(m.vertices.Length-1) * 3];

        int count = 1;
        for (int i = 0; i < trianglesArray.Length-3; i+=3) {
            trianglesArray[i] = count;
            trianglesArray[i + 1] = 0;
            trianglesArray[i + 2] = count + 1;
            count++;
        }
        //trianglesArray[trianglesArray.Length-3] = m.vertices.Length-1;
        //trianglesArray[trianglesArray.Length-2] = 0;
        //trianglesArray[trianglesArray.Length-1] = 1;
        
        m.triangles = trianglesArray;       

        m.RecalculateNormals();

        return m;
    }
}
