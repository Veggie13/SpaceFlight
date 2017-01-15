using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {

    bool _rocketFiring = false;
    float _rocketRate;
    Vector3 _cameraDefaultPosition;
    float _cameraDefaultAngle;

    const float Thrust = 20f;
    const float Manoeuvering = 1f;
    const float G = 6.67408e-11f;
    const float M = 1e17f;

    GameObject Planet { get { return GameObject.Find("Planet"); } }
    GameObject Rocket { get { return GameObject.Find("Rocket"); } }
    GameObject RocketSound { get { return GameObject.Find("RocketSound"); } }
    GameObject JetSound { get { return GameObject.Find("JetSound"); } }
    GameObject Dust { get { return GameObject.Find("Dust"); } }
    GameObject Camera { get { return GameObject.Find("Camera"); } }

    // Use this for initialization
    void Start () {
        var rocketTrail = Rocket.GetComponent<ParticleSystem>();
        var em = rocketTrail.emission;
        _rocketRate = em.rateOverTimeMultiplier;
        em.rateOverTimeMultiplier = 0f;

        RocketSound.GetComponent<AudioSource>().enabled = false;

        var planet = Planet;
        var s = planet.transform.position - transform.position;
        float vOrbit = Mathf.Sqrt(G * M / s.magnitude);

        Vector3 velocity = vOrbit * Vector3.Cross(s, transform.up).normalized;
        var myRigidBody = GetComponent<Rigidbody>();
        myRigidBody.AddForce(velocity, ForceMode.VelocityChange);

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

        var planet = Planet;
        var s = planet.transform.position - transform.position;
        Vector3 g = (G * M / s.sqrMagnitude) * s.normalized;
        Vector3 acceleration = g;

        var rocketTrail = Rocket.GetComponent<ParticleSystem>();
        var em = rocketTrail.emission;
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

        Vector3 torque = new Vector3(z, x, -y) * Manoeuvering;
        JetSound.GetComponent<AudioSource>().enabled = (torque.magnitude > 1e-5);

        var myRigidBody = GetComponent<Rigidbody>();
        Vector3 relVelocity = transform.InverseTransformVector(myRigidBody.velocity);
        Vector3 relDirection = relVelocity.normalized;
        Dust.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, -relDirection);
        var dustEm = Dust.GetComponent<ParticleSystem>().emission;
        dustEm.rateOverTimeMultiplier = relVelocity.magnitude;

        myRigidBody.AddForce(acceleration, ForceMode.Acceleration);
        myRigidBody.AddRelativeTorque(torque, ForceMode.Acceleration);

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
