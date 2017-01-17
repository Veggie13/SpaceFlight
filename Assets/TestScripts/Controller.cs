using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {

    class MassiveObject
    {
        public GameObject Object;
        public float Mass;

        public void ApplyGravity(GameObject other)
        {
            var g = GetGravityAt(other.transform.position);
            other.GetComponent<Rigidbody>().AddForce(g, ForceMode.Acceleration);
        }

        public Vector3 GetGravityAt(Vector3 pos)
        {
            var s = Object.transform.position - pos;
            return (G * Mass / s.sqrMagnitude) * s.normalized;
        }
    }

    bool _rocketFiring = false;
    float _rocketRate;
    Vector3 _cameraDefaultPosition;
    float _cameraDefaultAngle;

    const float Thrust = 100f;
    const float Brake = 20f;
    const float Manoeuvering = 1f;
    const float G = 6.67408e-11f;
    const float TrajectoryLookAhead = 20f;
    const float TrajectoryDelta = 0.1f;
    const int TrajectoryCount = (int)(TrajectoryLookAhead / TrajectoryDelta);

    MassiveObject Planet;
    MassiveObject Moon;
    GameObject Rocket { get { return GameObject.Find("Rocket"); } }
    GameObject RocketSound { get { return GameObject.Find("RocketSound"); } }
    GameObject JetSound { get { return GameObject.Find("JetSound"); } }
    GameObject Dust { get { return GameObject.Find("Dust"); } }
    GameObject Camera { get { return GameObject.Find("Camera"); } }
    GameObject Trajectory { get { return GameObject.Find("Trajectory"); } }

    // Use this for initialization
    void Start () {
        Planet = new MassiveObject()
        {
            Object = GameObject.Find("Planet"),
            Mass = 1e17f
        };
        Moon = new MassiveObject()
        {
            Object = GameObject.Find("Moon"),
            Mass = 1e15f
        };

        var s = Planet.Object.transform.position - transform.position;
        float vOrbit = Mathf.Sqrt(G * Planet.Mass / s.magnitude);

        Vector3 velocity = vOrbit * Vector3.Cross(s, transform.up).normalized;
        GetComponent<Rigidbody>().AddForce(velocity, ForceMode.VelocityChange);

        s = Planet.Object.transform.position - Moon.Object.transform.position;
        vOrbit = Mathf.Sqrt(G * Planet.Mass / s.magnitude);

        velocity = vOrbit * Vector3.Cross(s, Moon.Object.transform.up).normalized;
        Moon.Object.GetComponent<Rigidbody>().AddForce(velocity, ForceMode.VelocityChange);

        Trajectory.GetComponent<LineRenderer>().startWidth = 0.01f;

        var rocketTrail = Rocket.GetComponent<ParticleSystem>();
        var em = rocketTrail.emission;
        _rocketRate = em.rateOverTimeMultiplier;
        em.rateOverTimeMultiplier = 0f;

        RocketSound.GetComponent<AudioSource>().enabled = false;

        _cameraDefaultPosition = Camera.transform.localPosition;
        _cameraDefaultAngle = Vector3.Angle(Vector3.forward, -_cameraDefaultPosition);
    }

    private void OnCollisionEnter(Collision collision)
    {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }

    // Update is called once per frame
    void Update () {
        var x = Input.GetAxis("Shoulder");
        var y = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");
        var cx = Input.GetAxis("RHorizontal");
        var cy = Input.GetAxis("RVertical");
        bool rocketFiring = Input.GetButton("Fire1");
        bool braking = Input.GetButton("Fire2");

        Planet.ApplyGravity(Moon.Object);
        Planet.ApplyGravity(gameObject);
        Moon.ApplyGravity(gameObject);

        var rocketTrail = Rocket.GetComponent<ParticleSystem>();
        var em = rocketTrail.emission;
        Vector3 acceleration = new Vector3();
        if (rocketFiring)
        {
            acceleration += transform.TransformVector(0f, 0f, Thrust);
            if (!_rocketFiring)
            {
                em.rateOverTimeMultiplier = _rocketRate;
                RocketSound.GetComponent<AudioSource>().enabled = true;
            }
        }
        else if (_rocketFiring)
        {
            em.rateOverTimeMultiplier = 0f;
            RocketSound.GetComponent<AudioSource>().enabled = false;
        }
        _rocketFiring = rocketFiring;

        if (braking)
        {
            acceleration += transform.TransformVector(0f, 0f, -Brake);
        }

        Vector3 torque = new Vector3(z, x, -y) * Manoeuvering;
        JetSound.GetComponent<AudioSource>().enabled = braking || (torque.magnitude > 1e-5);

        var myRigidBody = GetComponent<Rigidbody>();
        Vector3 relVelocity = transform.InverseTransformVector(myRigidBody.velocity);
        Vector3 relDirection = relVelocity.normalized;
        Dust.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, -relDirection);
        var dustEm = Dust.GetComponent<ParticleSystem>().emission;
        dustEm.rateOverTimeMultiplier = relVelocity.magnitude;

        myRigidBody.AddForce(acceleration, ForceMode.Acceleration);
        myRigidBody.AddRelativeTorque(torque, ForceMode.Acceleration);

        Vector3 curPos = myRigidBody.transform.position;
        Vector3 curVel = myRigidBody.velocity;
        var trajectoryRenderer = Trajectory.GetComponent<LineRenderer>();
        trajectoryRenderer.numPositions = TrajectoryCount;
        for (int i = 0; i < TrajectoryCount; i++)
        {
            trajectoryRenderer.SetPosition(i, curPos);
            curPos += curVel * TrajectoryDelta;
            curVel += (Planet.GetGravityAt(curPos) + Moon.GetGravityAt(curPos)) * TrajectoryDelta;
        }

        float cy2 = (1 - cy * cy) * _cameraDefaultAngle / 90f + cy;
        var cameraDirX = Vector3.SlerpUnclamped(Vector3.back, Vector3.right, cx);
        var cameraDir = (cy2 > 0) ?
            Vector3.Slerp(cameraDirX, Vector3.up, cy2) :
            Vector3.Slerp(cameraDirX, Vector3.down, -cy2);
        Camera.transform.localPosition = _cameraDefaultPosition.magnitude * cameraDir;
        var cameraFaceX = -cameraDirX;
        var cameraFace = Vector3.Slerp(cameraFaceX, -cameraDir, Mathf.Sqrt(Mathf.Abs(cy)));
        Camera.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, cameraFace);
    }
}
