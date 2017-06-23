using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class Turret
{
	public Transform horizontalPivot, verticalPivot, gunBarrel, muzzlePoint;
	public float firingRate, muzzleVelocity, turnSpeed, curCooldown;
	public string bulletName;

    private float _curHealth;
	private GameObject _bullet;

	public GameObject Bullet
	{
		set{ _bullet = value;}
		get{ return _bullet;}
	}
}


public class TankHandler : NetworkBehaviour {

	public float movSpeed, rotSpeed, maxHealth;
	public Turret[] turrets = new Turret[]{ };
	public Camera _cam;

    private float _curHealth;


	// Use this for initialization
	void Start () {
		_cam = GetComponentInChildren<Camera> ();

		foreach (Turret turret in turrets) {
			turret.Bullet = (GameObject)Resources.Load (turret.bulletName);
		}
        if (isLocalPlayer)
        {
            _cam.transform.parent.parent = null;
        }
        else
        {
            Destroy(_cam.gameObject);
        }
        _curHealth = maxHealth;
    }
	
	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer) return;

		bool isFiring = false;

		if (Input.GetAxisRaw ("Fire1") == 1) {
			isFiring = true;
		}

		float x, y;
		x = Input.GetAxis ("Horizontal");
		y = Input.GetAxis ("Vertical");

		Vector3 moveDir = new Vector3 (0, 0, y*(movSpeed*Time.deltaTime));

		transform.Translate (moveDir, Space.Self);
		transform.Rotate (new Vector3 (0, x * (rotSpeed * Time.deltaTime), 0));

		Vector3 aimPos = Vector3.zero;
		Ray ray = _cam.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, Mathf.Infinity)) {
			aimPos = hit.point;
		} else {
			aimPos = ray.GetPoint (300);
		}

		foreach (Turret turret in turrets) {

			if (turret.curCooldown >= 0)
				turret.curCooldown -= Time.deltaTime;

			Quaternion wantedHRot = Quaternion.LookRotation ((aimPos - turret.horizontalPivot.position).normalized);
			Quaternion wantedVRot = Quaternion.LookRotation ((aimPos - turret.verticalPivot.position).normalized);

			turret.horizontalPivot.rotation = Quaternion.Euler (
				turret.horizontalPivot.eulerAngles.x, 
				Mathf.LerpAngle (turret.horizontalPivot.eulerAngles.y, wantedHRot.eulerAngles.y, turret.turnSpeed * Time.deltaTime),
				turret.horizontalPivot.eulerAngles.z);
			
			turret.verticalPivot.rotation = Quaternion.Euler (
				Mathf.LerpAngle (turret.verticalPivot.eulerAngles.x, wantedVRot.eulerAngles.x, turret.turnSpeed * Time.deltaTime),
				turret.verticalPivot.eulerAngles.y, 
				turret.verticalPivot.eulerAngles.z);

			if (isFiring && turret.curCooldown < 0) {
                CreateBullet(turret);
				turret.curCooldown = turret.firingRate;
			}
		}

	}

    public void AdjustCurHealth(float val)
    {
        _curHealth += val;

        if (_curHealth > maxHealth) _curHealth = maxHealth;

        if (_curHealth <= 0) NetworkServer.Destroy(gameObject);
    }
    void CreateBullet(Turret turret)
    {
        CmdSpawnBullet(turret.bulletName, turret.muzzlePoint.position, turret.muzzlePoint.rotation, turret.muzzlePoint.forward, turret.muzzleVelocity, netId);
    }
    [Command]
    void CmdSpawnBullet(string bulletName, Vector3 muzzlePos, Quaternion muzzleRot, Vector3 muzzleVec, float muzzleVel, NetworkInstanceId id)
    {
        GameObject obj = GameObject.Instantiate((GameObject)Resources.Load(bulletName), muzzlePos, muzzleRot);
     
        obj.GetComponent<Rigidbody>().AddForce(muzzleVec*muzzleVel, ForceMode.Impulse);
        obj.GetComponent<BulletHandler>().owner = id;
            NetworkServer.Spawn(obj);
    }
}
