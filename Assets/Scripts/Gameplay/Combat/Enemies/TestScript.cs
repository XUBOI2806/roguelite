using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{

    public Transform spawnPoint; // The spawnPoint that the bullets came out.
    [SerializeField] private float timer = 5.0f; // [SerializeField] is for private vairables, but it can be edit in the unity inspector.
    private float bulletTime; // Loading time of the bullet.
    public GameObject enemyBullet; // To get the bullet object.
    public float enemySpeed; // The move speed of the enemy.
    public TransformAnchor playerTransformAnchor = default;  

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {   // Keep the enemy that faces the player all the time.
        transform.LookAt(playerTransformAnchor.Value);
        // Shoot the player.
        ShootAtPlayer();
    }
    void ShootAtPlayer()
    {
        // bulletTime minus 1/60 in each frame.
        bulletTime -= Time.deltaTime;
        // If bulletTime is bigger than 0, do nothing. This means the loading time is passed.
        if (bulletTime > 0) return;
        // If else do.
        // Set the reload for shot time back to.
        bulletTime = timer;

        //30% to do
        if (Random.value <= 0.3f)
        {
            //Shoot 3 bullets

            //Shoot normal bullet
            ShootBulletDirection(spawnPoint.transform.rotation);

            //Shoot bullet a bit to the left
            Quaternion leftBullet = Quaternion.Euler(0, -15, 0) * spawnPoint.transform.rotation;
            ShootBulletDirection(leftBullet);

            //Shoot bullet a bit to the right
            Quaternion RightBullet = Quaternion.Euler(0, 15, 0) * spawnPoint.transform.rotation;
            ShootBulletDirection(RightBullet);
        }
        else
        {
            //Shoot normal bullet
            ShootBulletDirection(spawnPoint.transform.rotation);
        }
    }

    void ShootBulletDirection(Quaternion bulletRotation)
    {
        // Create a new Object that on that position and rotation, with the enemyBullet.

        // We use "as GameObject" at last because Instantiate will return a "object" not GameObject, -
        // - we use to explicitly cast to GameObject. Instantiate(GameObject, Vector3, Quaternion).
        GameObject bulletObj = Instantiate(enemyBullet, spawnPoint.transform.position, bulletRotation) as GameObject;
        // Get the rigidbody of the object.
        Rigidbody bulletRig = bulletObj.GetComponent<Rigidbody>();
        // Shot the object.
        bulletRig.AddForce(-bulletRig.transform.forward * -enemySpeed);
        // Destroy the object after 5 second.
        Destroy(bulletObj, 5f);
    }
}
