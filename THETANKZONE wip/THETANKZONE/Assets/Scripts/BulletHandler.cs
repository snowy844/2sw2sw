using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BulletHandler : NetworkBehaviour {

    [SyncVar]
    public NetworkInstanceId owner;

    public GameObject explosion;
    public float deadTime, damageRadius, damageVal;
    public bool explodeOnImpact, explodeOnTimeout;

    private float _curTime;
	// Use this for initialization
	void Start () {
        Collider myCol = GetComponent<Collider>();
        GameObject obj = ClientScene.FindLocalObject(owner);
        foreach (Collider col in obj.GetComponentsInChildren<Collider>()){
            Physics.IgnoreCollision(col, myCol);
        }
	}
    void DamageRadius(Vector3 pos)
    {
        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (Vector3.Distance(pos, obj.transform.position)< damageRadius)
            {
                obj.GetComponent<TankHandler>().AdjustCurHealth(-damageVal);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {

        _curTime += Time.deltaTime;

        if (_curTime > deadTime) {
          if (explodeOnTimeout)
            {
                CmdExplode(transform.position, Quaternion.identity.eulerAngles);
                DamageRadius(transform.position);
            }
            NetworkServer.Destroy(gameObject);
        }
	}

    [ServerCallback]
    void OnCollisionEnter(Collision col)
    {
        if (explodeOnImpact)
        {
            CmdExplode(col.contacts[0].point, col.contacts[0].normal);
            DamageRadius(col.contacts [0].point);
            NetworkServer.Destroy(gameObject);
        }
    }

    void CmdExplode(Vector3 pos, Vector3 rot)
    {
        GameObject exp = GameObject.Instantiate(explosion, pos, Quaternion.Euler(rot));
        NetworkServer.Spawn(exp);
    }
}
