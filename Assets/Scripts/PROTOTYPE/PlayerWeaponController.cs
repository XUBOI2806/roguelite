using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    public float turnSpeedMultiplier;
    public GameObject projectilePrefab;

    public float fireRate = 200;
    public int bulletsPerShot = 1;
    public float spreadAngle;

    public int magazineSize = 8;
    public int currentMagazineAmount = 8;

    public List<PlayerProjectileEffectSO> effectTemplates = new List<PlayerProjectileEffectSO>();
    public List<PlayerProjectileEffectSO> effects = new List<PlayerProjectileEffectSO>();

    public Transform muzzle;
    public ParticleSystem muzzleFlash;

    private Vector3 lastMuzzlePosition;
    public Vector3 muzzleWorldVelocity;

    private float lastTimeShot;

    [SerializeField] private InputReader inputReader = default;


    private void OnEnable()
    {
        inputReader.OnAttack2Performed += TryAttack;
    }

    private void OnDisable()
    {
        inputReader.OnAttack2Performed -= TryAttack;
    }

    
    private void Awake()
    {
           
    }

    void Update()
    {
        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity = (muzzle.position - lastMuzzlePosition) / Time.deltaTime;
            lastMuzzlePosition = muzzle.position;
        }
    }

    public void TryAttack()
    {
        FireWeapon();
    }

    public bool FireWeapon()
    {
        if (currentMagazineAmount <= 0)
        {
            return false;
        }
        if (lastTimeShot + 60 / fireRate < Time.time)
        {
            currentMagazineAmount -= 1;

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(muzzle);
                GameObject newProjectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(shotDirection));
                Debug.Log(effects[currentMagazineAmount]);
                newProjectile.GetComponent<PlayerProjectile>().InitialiseProjectile(this, effects[currentMagazineAmount]);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.Clear();
                muzzleFlash.Play();
            }
            lastTimeShot = Time.time;
            return true;
        }
        return false;
    }

    public void AddToCurrentMagazine(int count)
    {
        if (currentMagazineAmount < magazineSize)
        {
            currentMagazineAmount += count;
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Transform origin)
    {
        Debug.Log(origin.gameObject.name);
        float spreadAngleRatio = spreadAngle / 180f;
        Vector3 spreadWorldDirection = Vector3.Slerp(origin.forward, Random.insideUnitSphere, spreadAngleRatio);
        return spreadWorldDirection;
    }
}