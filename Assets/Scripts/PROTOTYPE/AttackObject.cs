using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackObject : MonoBehaviour
{
    public int damage;

    public bool onlyHitEachObjOnce;

    //public GameObject[] objsHit;
    //public int objsHitIndex;

    public bool ignoreCrit;

    public Hittable hittable;
    public PlayerCharacter playerChar;
    public ObjectPooler objectPooler;

    public GameObject hitMarker;
    public GameObject critMarker;

    void Awake()
    {
        objectPooler = ObjectPooler.Instance;
        playerChar = GameObject.FindWithTag("Player").GetComponent<PlayerCharacter>();
    }

    public void Hit(GameObject otherG)
    {
        hittable = otherG.GetComponent<Hittable>();
        if (hittable != null)
        {
            if (otherG.CompareTag("Enemy"))
            {
                int crit = Random.Range(0, 100);
                if (crit <= playerChar.critChance && !ignoreCrit)
                {
                    GameObject hitMarkerObj = Instantiate(critMarker, otherG.transform.position, Quaternion.identity);
                    hitMarkerObj.GetComponent<HitMarker>().damage = playerChar.critMultiplier * damage;
                    hitMarkerObj.GetComponent<HitMarker>().OnObjectSpawn();
                    hittable.health -= playerChar.critMultiplier * damage;
                }
                else
                {
                    GameObject hitMarkerObj = Instantiate(hitMarker, otherG.transform.position, Quaternion.identity);
                    hitMarkerObj.GetComponent<HitMarker>().damage = damage;
                    hitMarkerObj.GetComponent<HitMarker>().OnObjectSpawn();
                    hittable.health -= damage;
                }
            }

            hittable.Hit();
        }
        hittable = null;

    }

}
